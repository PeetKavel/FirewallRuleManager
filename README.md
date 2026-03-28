# FirewallRuleManager

A .NET 10 Blazor Server web application for managing firewall rules with GitHub-backed persistence. Rules are stored as JSON in the repository and committed automatically on every create, update, or delete operation.

## Features

- View, create, update, and delete firewall rules via a Blazor Server UI
- ASP.NET Core REST API (`FirewallRuleManager.Api`) with OpenAPI support
- GitHub-backed persistence: changes are committed and pushed automatically via `GitService`
- Excel import support for bulk rule ingestion
- Shared models library (`FirewallRuleManager.Shared`)

## Projects

| Project | Description |
|---|---|
| `FirewallRuleManager.Api` | ASP.NET Core Web API — CRUD endpoints at `/api/firewallrules` |
| `FirewallRuleManager.Web` | Blazor Server frontend |
| `FirewallRuleManager.Shared` | Shared model types |

## Getting Started

1. Clone the repository
2. Configure `appsettings.json` in `FirewallRuleManager.Api` with your GitHub token and repository details
3. Run the API: `dotnet run --project src/FirewallRuleManager.Api`
4. Run the web app: `dotnet run --project src/FirewallRuleManager.Web`
5. Open `https://localhost:7123` in your browser

## Agentic Workflows

This repository uses [GitHub Agentic Workflows](https://github.com/github/gh-aw) (`gh-aw`) for automated maintenance tasks. The following workflows run on a schedule or can be triggered manually.

### Daily Repo Status

**File**: `.github/workflows/daily-repo-status.md`
**Schedule**: Daily

Creates a daily GitHub issue summarising recent repository activity: pull requests, issues, releases, and code changes. Provides project recommendations and actionable next steps for maintainers.

### Daily Documentation Updater

**File**: `.github/workflows/daily-doc-updater.md`
**Schedule**: Daily (06:00 UTC)

Scans merged pull requests and commits from the last 24 hours, identifies undocumented features or changes, and opens a pull request with documentation updates. Follows the Diátaxis documentation framework.

### Security Compliance Campaign

**File**: `.github/workflows/security-compliance.md`
**Trigger**: Manual (`workflow_dispatch`)

Inputs:
- `audit_date` — Compliance audit deadline (`YYYY-MM-DD`)
- `severity_threshold` — Minimum severity to process (`critical`, `high`, `medium`; default `high`)
- `max_issues` — Maximum vulnerabilities to process (default `500`)

Orchestrates a full security remediation campaign: scans GitHub Security Advisories across the organisation, creates tracking issues for each vulnerability, and generates a compliance report for the CISO.

### Static Analysis Report

**File**: `.github/workflows/static-analysis-report.md`
**Schedule**: Daily

Runs [zizmor](https://github.com/zizmorcore/zizmor), [poutine](https://github.com/boostsecurityio/poutine), and [actionlint](https://github.com/rhysd/actionlint) against all agentic workflow files. Clusters findings by tool and severity, stores historical scan data in cache memory, and posts a summary discussion in the **Security** category.

### Shared: Reporting Guidelines

**File**: `.github/workflows/shared/reporting.md`

Imported by other workflows. Defines consistent report formatting conventions: header hierarchy, progressive disclosure with `<details>` blocks, and Airbnb-inspired clarity principles.