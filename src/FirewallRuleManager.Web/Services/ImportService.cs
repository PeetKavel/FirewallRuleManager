using FirewallRuleManager.Shared.DTOs;
using System.Net.Http.Json;

namespace FirewallRuleManager.Web.Services;

public class ImportService
{
    private readonly HttpClient _httpClient;

    public ImportService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(ImportResult? Result, string? Error)> ImportExcelAsync(Stream fileStream, string fileName)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content.Add(streamContent, "file", fileName);

        var response = await _httpClient.PostAsync("api/import/excel", content);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ImportResult>();
            return (result, null);
        }
        var error = await response.Content.ReadAsStringAsync();
        return (null, error);
    }
}
