using FirewallRuleManager.Shared.DTOs;
using System.Net.Http.Json;

namespace FirewallRuleManager.Web.Services;

public class GitRepositoryService
{
    private readonly HttpClient _httpClient;

    public GitRepositoryService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<GitRepositoryConfig?> GetStatusAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<GitRepositoryConfig>("api/gitrepository/status");
        }
        catch
        {
            return null;
        }
    }

    public async Task<(GitRepositoryConfig? Config, string? Error)> CreateRepositoryAsync(CreateRepositoryRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/gitrepository/create", request);
        if (response.IsSuccessStatusCode)
        {
            var config = await response.Content.ReadFromJsonAsync<GitRepositoryConfig>();
            return (config, null);
        }
        var error = await response.Content.ReadAsStringAsync();
        return (null, error);
    }
}
