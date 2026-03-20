using System.Text.Json;
using FirewallRuleManager.Shared.Models;

namespace FirewallRuleManager.Api.Data;

public class FirewallRuleRepository
{
    private readonly string _dataDirectory;
    private readonly string _dataFile;
    private List<FirewallRule> _rules = new();

    public FirewallRuleRepository(IConfiguration configuration)
    {
        var configuredDir = configuration["DataDirectory"];
        _dataDirectory = string.IsNullOrWhiteSpace(configuredDir)
            ? Path.Combine(AppContext.BaseDirectory, "data")
            : configuredDir;
        _dataFile = Path.Combine(_dataDirectory, "firewall-rules.json");
        LoadRules();
    }

    private void LoadRules()
    {
        if (!Directory.Exists(_dataDirectory))
            Directory.CreateDirectory(_dataDirectory);

        if (File.Exists(_dataFile))
        {
            var json = File.ReadAllText(_dataFile);
            _rules = JsonSerializer.Deserialize<List<FirewallRule>>(json) ?? new List<FirewallRule>();
        }
    }

    private void SaveRules()
    {
        var json = JsonSerializer.Serialize(_rules, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_dataFile, json);
    }

    public IReadOnlyList<FirewallRule> GetAll() => _rules.AsReadOnly();

    public FirewallRule? GetById(Guid id) => _rules.FirstOrDefault(r => r.Id == id);

    public FirewallRule Add(FirewallRule rule)
    {
        rule.Id = Guid.NewGuid();
        _rules.Add(rule);
        SaveRules();
        return rule;
    }

    public FirewallRule? Update(Guid id, FirewallRule updated)
    {
        var existing = _rules.FirstOrDefault(r => r.Id == id);
        if (existing == null) return null;

        existing.FromHostname = updated.FromHostname;
        existing.ToHostname = updated.ToHostname;
        existing.PortNumber = updated.PortNumber;
        existing.Description = updated.Description;
        existing.Protocol = updated.Protocol;
        existing.RegistrationDate = updated.RegistrationDate;
        SaveRules();
        return existing;
    }

    public bool Delete(Guid id)
    {
        var rule = _rules.FirstOrDefault(r => r.Id == id);
        if (rule == null) return false;
        _rules.Remove(rule);
        SaveRules();
        return true;
    }

    public void AddRange(IEnumerable<FirewallRule> rules)
    {
        foreach (var rule in rules)
        {
            rule.Id = Guid.NewGuid();
            _rules.Add(rule);
        }
        SaveRules();
    }

    public string GetDataDirectory() => _dataDirectory;
}
