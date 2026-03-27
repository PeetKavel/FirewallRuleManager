# FirewallRuleManager

A .NET 10 Blazor Server application for managing firewall rules with GitHub-backed persistence. Rules are stored in a GitHub repository as JSON, enabling version-controlled change tracking and audit history.

## Overview

FirewallRuleManager consists of three projects:

| Project | Description |
|---------|-------------|
| `FirewallRuleManager.Api` | ASP.NET Core Web API – CRUD endpoints for firewall rules, Git operations, and Excel import |
| `FirewallRuleManager.Web` | Blazor Server frontend – UI for creating, editing, and reviewing rules |
| `FirewallRuleManager.Shared` | Shared models and DTOs used by both API and Web |

### Firewall Rule Fields

Each rule contains:

- **From Hostname** – source hostname (required, max 255 characters)
- **To Hostname** – destination hostname (required, max 255 characters)
- **Port Number** – target port (required, 1–65535)
- **Protocol** – TCP or UDP (required)
- **Description** – optional notes (max 1000 characters)
- **Registration Date** – timestamp when the rule was created

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A GitHub repository and personal access token for rule persistence

### Configuration

Create a `git-config.json` file in the API project output directory (or configure via environment variables):

```json
{
  "GitRepository": {
    "Owner": "your-org",
    "Repo": "your-rules-repo",
    "Branch": "main",
    "Token": "ghp_..."
  }
}
```

### Running Locally

```bash
# Start the API
dotnet run --project src/FirewallRuleManager.Api

# Start the Web frontend
dotnet run --project src/FirewallRuleManager.Web
```

The Web app defaults to `https://localhost:7123` and connects to the API at `http://localhost:5003` / `https://localhost:7123`.

## Agentic Workflows

FirewallRuleManager uses [GitHub Copilot agentic workflows](https://docs.github.com/en/copilot) to automate documentation and repository maintenance tasks.

### Daily Documentation Updater

**File:** `.github/workflows/daily-doc-updater.md`

Runs every day at 06:00 UTC and on manual dispatch.

This workflow scans merged pull requests and commits from the last 24 hours, identifies new or changed features, and automatically creates a documentation pull request. It follows a structured process:

1. Search for PRs merged in the last 24 hours
2. Analyse additions, removals, and breaking changes
3. Review documentation guidelines from `.github/instructions/documentation.instructions.md`
4. Identify gaps in the `docs/src/content/docs/` directory
5. Update or create the appropriate documentation files
6. Open a draft PR with the changes for review

The generated PR is labelled `documentation` and `automation`, assigned to the `copilot` reviewer, and configured for auto-merge.

### Daily Repository Status

**File:** `.github/workflows/daily-repo-status.md`

Runs every day and on manual dispatch.

This workflow gathers recent repository activity and publishes a daily status report as a GitHub issue. The report includes:

- Recent issues, pull requests, and discussions
- Releases and notable code changes
- Progress highlights and community activity
- Actionable recommendations for maintainers

Issues are labelled `report` and `daily-status`. Older status issues are closed automatically when a new report is created.

## Contributing

Pull requests are welcome. The project uses GitHub Actions and agentic workflows to help maintain documentation quality and repository health automatically.