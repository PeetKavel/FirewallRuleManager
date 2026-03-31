using FirewallRuleManager.Api.Services;
using FirewallRuleManager.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace FirewallRuleManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GitRepositoryController : ControllerBase
{
    private readonly GitService _gitService;
    private readonly ILogger<GitRepositoryController> _logger;

    public GitRepositoryController(GitService gitService, ILogger<GitRepositoryController> logger)
    {
        _gitService = gitService;
        _logger = logger;
    }

    [HttpGet("status")]
    public ActionResult<GitRepositoryConfig> GetStatus()
    {
        return Ok(_gitService.GetConfig());
    }

    [HttpPost("create")]
    public async Task<ActionResult<GitRepositoryConfig>> Create([FromBody] CreateRepositoryRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrWhiteSpace(request.RepositoryName))
            return BadRequest("Repository name is required.");

        try
        {
            var config = await _gitService.CreateRepositoryAsync(request);
            return Ok(config);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to create repository");
            return BadRequest(ex.Message);
        }
    }
}
