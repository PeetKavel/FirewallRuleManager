using FirewallRuleManager.Api.Services;
using FirewallRuleManager.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace FirewallRuleManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GitRepositoryController : ControllerBase
{
    private readonly GitService _gitService;

    public GitRepositoryController(GitService gitService)
    {
        _gitService = gitService;
    }

    [HttpGet("status")]
    public ActionResult<GitRepositoryConfig> GetStatus()
    {
        return Ok(_gitService.GetConfig());
    }

    [HttpPost("create")]
    public async Task<ActionResult<GitRepositoryConfig>> Create([FromBody] CreateRepositoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RepositoryName))
            return BadRequest("Repository name is required.");

        try
        {
            var config = await _gitService.CreateRepositoryAsync(request);
            return Ok(config);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
