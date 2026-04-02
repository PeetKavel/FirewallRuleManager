using System.ComponentModel.DataAnnotations;

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

public partial class CreateRepositoryRequest : IValidatableObject
{
    [Required(ErrorMessage = "Repository name is required.")]
    [MaxLength(100, ErrorMessage = "Repository name cannot exceed 100 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Repository name can only contain alphanumeric characters, hyphens, dots, and underscores.")]
    public string RepositoryName { get; set; } = string.Empty;

    [MaxLength(255, ErrorMessage = "Description cannot exceed 255 characters.")]
    public string? Description { get; set; }

    public bool IsPrivate { get; set; } = true;

    [MaxLength(260, ErrorMessage = "Local path cannot exceed 260 characters.")]
    public string LocalPath { get; set; } = string.Empty;

    public string GitHubToken { get; set; } = string.Empty;

    [MaxLength(39, ErrorMessage = "GitHub owner cannot exceed 39 characters.")]
    [RegularExpression(@"^$|^[a-zA-Z0-9]([a-zA-Z0-9-]*[a-zA-Z0-9])?$", ErrorMessage = "GitHub owner contains invalid characters.")]
    public string GitHubOwner { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(LocalPath))
        {
            var pathError = ValidateLocalPath(LocalPath);
            if (pathError != null)
                yield return pathError;
        }
    }

    private static ValidationResult? ValidateLocalPath(string localPath)
    {
        // Block path traversal: reject paths containing ".."
        if (localPath.Contains(".."))
            return new ValidationResult("Local path must not contain path traversal sequences (..).", new[] { nameof(LocalPath) });

        try
        {
            var fullPath = Path.GetFullPath(localPath);

            // Block absolute paths pointing to sensitive system directories
            if (ContainsSensitivePath(fullPath))
                return new ValidationResult("Local path points to a restricted system directory.", new[] { nameof(LocalPath) });
        }
        catch (Exception)
        {
            return new ValidationResult("Local path is not a valid file system path.", new[] { nameof(LocalPath) });
        }

        return null;
    }

    private static bool ContainsSensitivePath(string fullPath)
    {
        var normalized = fullPath.Replace('\\', '/').TrimEnd('/').ToLowerInvariant();

        // Block UNC paths
        if (normalized.StartsWith("//"))
            return true;

        // Block well-known sensitive Linux directories
        string[] sensitivePosixRoots = { "/etc", "/var", "/usr", "/bin", "/sbin", "/boot", "/proc", "/sys", "/dev" };
        foreach (var root in sensitivePosixRoots)
        {
            if (normalized.Equals(root) || normalized.StartsWith(root + "/"))
                return true;
        }

        // Block well-known sensitive Windows directories using environment where available
        var windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var systemDir = Environment.GetFolderPath(Environment.SpecialFolder.System);

        foreach (var dir in new[] { windowsDir, programFiles, programFilesX86, systemDir })
        {
            if (string.IsNullOrEmpty(dir)) continue;
            var normalizedDir = dir.Replace('\\', '/').TrimEnd('/').ToLowerInvariant();
            if (normalized.Equals(normalizedDir) || normalized.StartsWith(normalizedDir + "/"))
                return true;
        }

        return false;
    }
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
