namespace FirewallRuleManager.Shared.DTOs;

public class GitRepositoryConfig
{
    public string? RepositoryPath { get; set; }
    /// <summary>Token is never serialized in API responses; use HasToken to check if configured.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? GitHubToken { get; set; }
    public bool HasToken { get; set; }
    public string? GitHubOwner { get; set; }
    public string? GitHubRepoName { get; set; }
    public bool IsConfigured { get; set; }
}

public class CreateRepositoryRequest
{
    public string RepositoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPrivate { get; set; } = true;
    public string LocalPath { get; set; } = string.Empty;
    public string GitHubToken { get; set; } = string.Empty;
    public string GitHubOwner { get; set; } = string.Empty;
}

public class ImportResult
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<ImportRowError> Errors { get; set; } = new();
}

public class ImportRowError
{
    public int RowNumber { get; set; }
    public List<string> Messages { get; set; } = new();
}
