using FirewallRuleManager.Api.Data;
using FirewallRuleManager.Api.Services;
using FirewallRuleManager.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FirewallRuleManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FirewallRulesController : ControllerBase
{
    private readonly FirewallRuleRepository _repository;
    private readonly GitService _gitService;

    public FirewallRulesController(FirewallRuleRepository repository, GitService gitService)
    {
        _repository = repository;
        _gitService = gitService;
    }

    [HttpGet]
    public ActionResult<IEnumerable<FirewallRule>> GetAll()
    {
        return Ok(_repository.GetAll());
    }

    [HttpGet("{id:guid}")]
    public ActionResult<FirewallRule> GetById(Guid id)
    {
        var rule = _repository.GetById(id);
        if (rule == null) return NotFound();
        return Ok(rule);
    }

    [HttpPost]
    public async Task<ActionResult<FirewallRule>> Create([FromBody] FirewallRule rule)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var created = _repository.Add(rule);

        await _gitService.CommitAndPushAsync(
            Path.Combine(_repository.GetDataDirectory(), "firewall-rules.json"),
            $"Add firewall rule: {created.FromHostname} -> {created.ToHostname}:{created.PortNumber}");

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FirewallRule>> Update(Guid id, [FromBody] FirewallRule rule)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var updated = _repository.Update(id, rule);
        if (updated == null) return NotFound();

        await _gitService.CommitAndPushAsync(
            Path.Combine(_repository.GetDataDirectory(), "firewall-rules.json"),
            $"Update firewall rule: {updated.FromHostname} -> {updated.ToHostname}:{updated.PortNumber}");

        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var rule = _repository.GetById(id);
        if (rule == null) return NotFound();

        _repository.Delete(id);

        await _gitService.CommitAndPushAsync(
            Path.Combine(_repository.GetDataDirectory(), "firewall-rules.json"),
            $"Delete firewall rule: {rule.FromHostname} -> {rule.ToHostname}:{rule.PortNumber}");

        return NoContent();
    }
}
