using FirewallRuleManager.Api.Data;
using FirewallRuleManager.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Load optional git-config.json if it exists
var gitConfigPath = Path.Combine(AppContext.BaseDirectory, "git-config.json");
if (File.Exists(gitConfigPath))
{
    builder.Configuration.AddJsonFile(gitConfigPath, optional: true, reloadOnChange: true);
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<FirewallRuleRepository>();
builder.Services.AddSingleton<GitService>();
builder.Services.AddSingleton<ExcelImportService>();

// CORS for Blazor frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                builder.Configuration["AllowedOrigins"] ?? "https://localhost:7123",
                "http://localhost:5003",
                "https://localhost:7123")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
