using FirewallRuleManager.Shared.DTOs;
using LibGit2Sharp;
using Microsoft.AspNetCore.DataProtection;
using Octokit;

namespace FirewallRuleManager.Api.Services;

public class GitService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GitService> _logger;
    private readonly IDataProtector _protector;

    public GitService(IConfiguration configuration, ILogger<GitService> logger, IDataProtectionProvider dataProtectionProvider)
    {
        _configuration = configuration;
        _logger = logger;
        _protector = dataProtectionProvider.CreateProtector("GitService.Token");
    }

    public GitRepositoryConfig GetConfig()
    {
        var path = _configuration["Git:RepositoryPath"];
        var encryptedToken = _configuration["Git:GitHubToken"];
        var owner = _configuration["Git:GitHubOwner"];
        var repoName = _configuration["Git:GitHubRepoName"];

        bool hasToken = false;
        if (!string.IsNullOrEmpty(encryptedToken))
        {
            hasToken = TryUnprotectToken(encryptedToken, out _);
        }

        return new GitRepositoryConfig
        {
            RepositoryPath = path,
            HasToken = hasToken,
            GitHubOwner = owner,
            GitHubRepoName = repoName,
            IsConfigured = !string.IsNullOrEmpty(path) && Directory.Exists(path) && LibGit2Sharp.Repository.IsValid(path)
        };
    }

    public bool IsRepositoryConfigured()
    {
        var path = _configuration["Git:RepositoryPath"];
        return !string.IsNullOrEmpty(path) && Directory.Exists(path) && LibGit2Sharp.Repository.IsValid(path);
    }

    public async Task<GitRepositoryConfig> CreateRepositoryAsync(CreateRepositoryRequest request)
    {
        // Validate GitHub owner and repo name format to prevent URL injection
        if (!string.IsNullOrEmpty(request.GitHubOwner) && !System.Text.RegularExpressions.Regex.IsMatch(request.GitHubOwner, @"^[a-zA-Z0-9]([a-zA-Z0-9-]*[a-zA-Z0-9])?$"))
            throw new InvalidOperationException("GitHub owner contains invalid characters.");

        if (!System.Text.RegularExpressions.Regex.IsMatch(request.RepositoryName, @"^[a-zA-Z0-9._-]+$"))
            throw new InvalidOperationException("Repository name contains invalid characters.");

        // Create GitHub repository if token provided
        if (!string.IsNullOrEmpty(request.GitHubToken) && !string.IsNullOrEmpty(request.GitHubOwner))
        {
            try
            {
                var github = new GitHubClient(new ProductHeaderValue("FirewallRuleManager"));
                github.Credentials = new Octokit.Credentials(request.GitHubToken);

                var newRepo = new NewRepository(request.RepositoryName)
                {
                    Description = request.Description,
                    Private = request.IsPrivate,
                    AutoInit = true
                };

                await github.Repository.Create(newRepo);
                _logger.LogInformation("Created GitHub repository {Owner}/{Repo}", request.GitHubOwner, request.RepositoryName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create GitHub repository");
                throw new InvalidOperationException("Failed to create GitHub repository. Please verify your token and permissions.", ex);
            }
        }

        // Initialize local repository
        var localPath = string.IsNullOrEmpty(request.LocalPath)
            ? Path.Combine(AppContext.BaseDirectory, "data", "git")
            : Path.GetFullPath(request.LocalPath);

        if (!Directory.Exists(localPath))
            Directory.CreateDirectory(localPath);

        if (!LibGit2Sharp.Repository.IsValid(localPath))
        {
            LibGit2Sharp.Repository.Init(localPath);
            _logger.LogInformation("Initialized local git repository at {Path}", localPath);
        }

        // Encrypt the token before storing
        string? encryptedToken = null;
        if (!string.IsNullOrEmpty(request.GitHubToken))
        {
            encryptedToken = _protector.Protect(request.GitHubToken);
        }

        // Update configuration
        _configuration["Git:RepositoryPath"] = localPath;
        _configuration["Git:GitHubToken"] = encryptedToken;
        _configuration["Git:GitHubOwner"] = request.GitHubOwner;
        _configuration["Git:GitHubRepoName"] = request.RepositoryName;

        // Persist config to appsettings
        await PersistConfigAsync(localPath, encryptedToken, request.GitHubOwner, request.RepositoryName);

        return new GitRepositoryConfig
        {
            RepositoryPath = localPath,
            HasToken = !string.IsNullOrEmpty(request.GitHubToken),
            GitHubOwner = request.GitHubOwner,
            GitHubRepoName = request.RepositoryName,
            IsConfigured = true
        };
    }

    public async Task CommitAndPushAsync(string filePath, string commitMessage)
    {
        var repoPath = _configuration["Git:RepositoryPath"];
        if (string.IsNullOrEmpty(repoPath) || !LibGit2Sharp.Repository.IsValid(repoPath))
        {
            _logger.LogWarning("Git repository not configured, skipping commit");
            return;
        }

        try
        {
            using var repo = new LibGit2Sharp.Repository(repoPath);

            // Stage the file
            Commands.Stage(repo, filePath);

            // Create commit
            var signature = new LibGit2Sharp.Signature(
                _configuration["Git:CommitAuthorName"] ?? "FirewallRuleManager",
                _configuration["Git:CommitAuthorEmail"] ?? "frm@example.com",
                DateTimeOffset.UtcNow);

            if (repo.Index.Count == 0)
            {
                _logger.LogInformation("No changes to commit");
                return;
            }

            repo.Commit(commitMessage, signature, signature);
            _logger.LogInformation("Committed changes: {Message}", commitMessage);

            // Push to remote if configured
            var encryptedToken = _configuration["Git:GitHubToken"];
            if (!string.IsNullOrEmpty(encryptedToken) && TryUnprotectToken(encryptedToken, out var token))
            {
                await PushToGitHubAsync(repo, token!);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit/push changes");
        }
    }

    private Task PushToGitHubAsync(LibGit2Sharp.Repository repo, string token)
    {
        try
        {
            var remote = repo.Network.Remotes["origin"];
            if (remote == null)
            {
                var owner = _configuration["Git:GitHubOwner"];
                var repoName = _configuration["Git:GitHubRepoName"];
                if (!string.IsNullOrEmpty(owner) && !string.IsNullOrEmpty(repoName))
                {
                    // Validate owner and repo name to prevent URL injection
                    if (!System.Text.RegularExpressions.Regex.IsMatch(owner, @"^[a-zA-Z0-9]([a-zA-Z0-9-]*[a-zA-Z0-9])?$") ||
                        !System.Text.RegularExpressions.Regex.IsMatch(repoName, @"^[a-zA-Z0-9._-]+$"))
                    {
                        _logger.LogWarning("Invalid GitHub owner or repository name format, skipping remote setup");
                        return Task.CompletedTask;
                    }

                    repo.Network.Remotes.Add("origin", $"https://github.com/{owner}/{repoName}.git");
                    remote = repo.Network.Remotes["origin"];
                }
            }

            if (remote != null)
            {
                var pushOptions = new PushOptions
                {
                    CredentialsProvider = (_, _, _) =>
                        new UsernamePasswordCredentials { Username = "token", Password = token }
                };

                var branch = repo.Head;
                repo.Network.Push(remote, branch.CanonicalName, pushOptions);
                _logger.LogInformation("Pushed to remote");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to push to remote");
        }

        return Task.CompletedTask;
    }

    private async Task PersistConfigAsync(string repoPath, string? encryptedToken, string? owner, string? repoName)
    {
        try
        {
            // Write to a separate config file (token is already encrypted)
            var gitConfigPath = Path.Combine(AppContext.BaseDirectory, "git-config.json");
            var gitConfig = new
            {
                Git = new
                {
                    RepositoryPath = repoPath,
                    GitHubToken = encryptedToken,
                    GitHubOwner = owner,
                    GitHubRepoName = repoName
                }
            };

            await File.WriteAllTextAsync(gitConfigPath,
                System.Text.Json.JsonSerializer.Serialize(gitConfig, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist git config");
        }
    }

    private bool TryUnprotectToken(string encryptedToken, out string? token)
    {
        try
        {
            token = _protector.Unprotect(encryptedToken);
            return !string.IsNullOrEmpty(token);
        }
        catch (Exception)
        {
            // Token may be stored in plaintext from before encryption was added,
            // or the key may have changed. Log and treat as unavailable.
            _logger.LogWarning("Failed to decrypt stored token. It may need to be reconfigured.");
            token = null;
            return false;
        }
    }
}
