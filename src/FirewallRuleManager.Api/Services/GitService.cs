using FirewallRuleManager.Shared.DTOs;
using LibGit2Sharp;
using Octokit;

namespace FirewallRuleManager.Api.Services;

public class GitService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GitService> _logger;

    public GitService(IConfiguration configuration, ILogger<GitService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public GitRepositoryConfig GetConfig()
    {
        var path = _configuration["Git:RepositoryPath"];
        var token = _configuration["Git:GitHubToken"];
        var owner = _configuration["Git:GitHubOwner"];
        var repoName = _configuration["Git:GitHubRepoName"];

        return new GitRepositoryConfig
        {
            RepositoryPath = path,
            GitHubToken = token,    // not serialized by API
            HasToken = !string.IsNullOrEmpty(token),
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
                throw new InvalidOperationException($"Failed to create GitHub repository: {ex.Message}", ex);
            }
        }

        // Initialize local repository
        var localPath = string.IsNullOrEmpty(request.LocalPath)
            ? Path.Combine(AppContext.BaseDirectory, "data", "git")
            : request.LocalPath;

        if (!Directory.Exists(localPath))
            Directory.CreateDirectory(localPath);

        if (!LibGit2Sharp.Repository.IsValid(localPath))
        {
            LibGit2Sharp.Repository.Init(localPath);
            _logger.LogInformation("Initialized local git repository at {Path}", localPath);
        }

        // Update configuration
        _configuration["Git:RepositoryPath"] = localPath;
        _configuration["Git:GitHubToken"] = request.GitHubToken;
        _configuration["Git:GitHubOwner"] = request.GitHubOwner;
        _configuration["Git:GitHubRepoName"] = request.RepositoryName;

        // Persist config to appsettings
        await PersistConfigAsync(localPath, request.GitHubToken, request.GitHubOwner, request.RepositoryName);

        return new GitRepositoryConfig
        {
            RepositoryPath = localPath,
            GitHubToken = request.GitHubToken,  // not serialized by API
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
            var token = _configuration["Git:GitHubToken"];
            if (!string.IsNullOrEmpty(token))
            {
                await PushToGitHubAsync(repo, token);
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

    private async Task PersistConfigAsync(string repoPath, string? token, string? owner, string? repoName)
    {
        var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(appSettingsPath))
            return;

        try
        {
            var json = await File.ReadAllTextAsync(appSettingsPath);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement.Clone();

            // We'll use a simple approach - write to a separate config file
            var gitConfigPath = Path.Combine(AppContext.BaseDirectory, "git-config.json");
            var gitConfig = new
            {
                Git = new
                {
                    RepositoryPath = repoPath,
                    GitHubToken = token,
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
}
