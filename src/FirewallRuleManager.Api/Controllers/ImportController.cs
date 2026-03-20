using FirewallRuleManager.Api.Data;
using FirewallRuleManager.Api.Services;
using FirewallRuleManager.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace FirewallRuleManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImportController : ControllerBase
{
    private readonly ExcelImportService _importService;
    private readonly FirewallRuleRepository _repository;
    private readonly GitService _gitService;

    public ImportController(ExcelImportService importService, FirewallRuleRepository repository, GitService gitService)
    {
        _importService = importService;
        _repository = repository;
        _gitService = gitService;
    }

    [HttpPost("excel")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB limit
    public async Task<ActionResult<ImportResult>> ImportExcel(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".xlsx" && extension != ".xls")
            return BadRequest("Only Excel files (.xlsx, .xls) are supported.");

        using var stream = file.OpenReadStream();
        var result = _importService.Import(stream, out var validRules);

        if (validRules.Count > 0)
        {
            _repository.AddRange(validRules);

            await _gitService.CommitAndPushAsync(
                Path.Combine(_repository.GetDataDirectory(), "firewall-rules.json"),
                $"Import {validRules.Count} firewall rules from Excel");
        }

        return Ok(result);
    }
}
