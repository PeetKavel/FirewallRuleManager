using FirewallRuleManager.Shared.Models;
using System.Net.Http.Json;

namespace FirewallRuleManager.Web.Services;

public class FirewallRuleService
{
    private readonly HttpClient _httpClient;

    public FirewallRuleService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<FirewallRule>> GetAllAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<FirewallRule>>("api/firewallrules") ?? new();
    }

    public async Task<FirewallRule?> GetByIdAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<FirewallRule>($"api/firewallrules/{id}");
    }

    public async Task<FirewallRule?> CreateAsync(FirewallRule rule)
    {
        var response = await _httpClient.PostAsJsonAsync("api/firewallrules", rule);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<FirewallRule>();
        return null;
    }

    public async Task<FirewallRule?> UpdateAsync(Guid id, FirewallRule rule)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/firewallrules/{id}", rule);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<FirewallRule>();
        return null;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/firewallrules/{id}");
        return response.IsSuccessStatusCode;
    }
}
