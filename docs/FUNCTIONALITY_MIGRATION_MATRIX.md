# FUNCTIONALITY MIGRATION MATRIX: Python to .NET 10
**Legacy CE21 SafeDB DeployBot ➔ SafeDeploy Studio Enterprise Platform**

Every existing functional capability from the legacy Python application is mapped below to its equivalent .NET 10 architecture component.

---

## 1. Core Architecture & Infrastructure

| Legacy Python Component | Legacy Implementation | Proposed .NET 10 Component | Target Project Layer | Architecture Enhancements |
|---|---|---|---|---|
| **Solution Structure** | Ad-hoc Python scripts in single folder | Visual Studio Solution (`SafeDeployStudio.sln`) with Clean Architecture | All | Modular monolith, clean separation of concerns, strict dependency rules |
| **Runtime Environment** | Python 3.14 + PyQt6 + pyodbc | .NET 10 LTS (C# 14) + ASP.NET Core | `SafeDeployStudio.Api` / `SafeDeployStudio.Web` | Production runtime, high-throughput, cross-platform server, native JIT |
| **Data Storage (Audit & State)** | Single SQLite file (`history_index.sqlite`) | Entity Framework Core 10 + SQL Server / SQLite Provider | `SafeDeployStudio.Infrastructure` | Multi-tenant organization & project isolation, migrations, structured auditing |
| **Object Relational Mapping** | Raw SQL queries via `sqlite3` and `pyodbc` | Dapper for high-speed metadata & EF Core for application state | `SafeDeployStudio.Infrastructure` | Parameterized commands, strongly typed models, resilient retry policies |
| **Logging** | Python `logging` + plain-text append file | Serilog with structured JSON sinks (Console, File, SQL Server) | `SafeDeployStudio.Infrastructure` | Structured contextual logging (`CorrelationId`, `ProjectId`, `UserId`, `EnvironmentId`) |
| **Configuration** | Local `connections.json` | `Microsoft.Extensions.Configuration` + Encrypted Database Vault | `SafeDeployStudio.Infrastructure` | Environment-specific config (`appsettings.json`), Azure Key Vault / DPAPI credential protection |

---

## 2. Multi-Project & Multi-Environment Management

| Capability | Legacy Python Status | Proposed .NET 10 Design | Target Project Layer |
|---|---|---|---|
| **Organization Hierarchy** | Not supported (single hardcoded environment) | `Organization` entity with multiple `Projects` | `SafeDeployStudio.Domain` |
| **Project Creation** | Hardcoded to CE21 | Dynamic project creation via UI & API with custom policies | `SafeDeployStudio.Application` |
| **Environment Management** | Hardcoded `LOCAL` and `DEV` | Dynamic `Environment` entity (Dev, QA, Stage, Prod, Custom) | `SafeDeployStudio.Domain` |
| **Deployment Routes** | Single hardcoded route (`Local ➔ DEV`) | Configurable route graph (`Dev ➔ QA ➔ Staging ➔ Prod`) with approval gates | `SafeDeployStudio.Domain` |
| **Data Isolation** | None (single local user) | Tenant/Project scoped DbContext filters & role-based claims | `SafeDeployStudio.Infrastructure` |

---

## 3. Database Provider Abstraction

| Capability | Legacy Python Status | Proposed .NET 10 Design | Target Project Layer |
|---|---|---|---|
| **Provider Abstraction** | None (T-SQL specific via pyodbc) | `IDatabaseProvider`, `IDatabaseConnectionService`, `ISchemaMetadataService`, `IScriptExecutionService` | `SafeDeployStudio.DatabaseProviders.Abstractions` |
| **Microsoft SQL Server Provider** | `pyodbc` with ODBC Driver 18 | `Microsoft.Data.SqlClient` + Dapper execution engine | `SafeDeployStudio.DatabaseProviders.SqlServer` |
| **PostgreSQL Provider** | Not supported | Extensible interface prepared for `Npgsql` | `SafeDeployStudio.DatabaseProviders.PostgreSql` |
| **Database Discovery** | `SELECT name FROM sys.databases` | `ISchemaMetadataService.GetDatabasesAsync()` | `SafeDeployStudio.DatabaseProviders.SqlServer` |
| **Object Definition Extraction** | `sys.sql_modules` / `OBJECT_DEFINITION()` | `ISchemaMetadataService.GetObjectDefinitionAsync(objectName, type)` | `SafeDeployStudio.DatabaseProviders.SqlServer` |

---

## 4. SQL Comparison Engine

| Capability | Legacy Python Status | Proposed .NET 10 Design | Target Project Layer |
|---|---|---|---|
| **Whitespace Normalization** | `normalize_line()` (splits & rejoins) | `ISqlDiffEngine.NormalizeWhitespace()` | `SafeDeployStudio.Deployment` |
| **Token-Level Character Diff** | Python `difflib.SequenceMatcher` | DiffPlex library + Custom AST token highlighter | `SafeDeployStudio.Deployment` |
| **Differences-Only Mode** | Compact hunks with configurable 3-line context | Structured hunk chunking with context margin | `SafeDeployStudio.Deployment` |
| **Full Definition Alignment** | Aligned matrix with line numbering | Full-text side-by-side model with synchronized line mapping | `SafeDeployStudio.Deployment` |
| **Diff Statistics** | Added, removed, modified line counts | Strongly typed `DiffStatistics` model (`AddedCount`, `DeletedCount`, `ModifiedCount`) | `SafeDeployStudio.Deployment` |

---

## 5. Security & Guardrail Engine

| Capability | Legacy Python Status | Proposed .NET 10 Design | Target Project Layer |
|---|---|---|---|
| **Forbidden Keyword Detection** | Regex scanning (`DROP DATABASE`, `ALTER SCHEMA`, `xp_cmdshell`) | Rule-based AST scanner & regex token evaluation pipeline | `SafeDeployStudio.Guardrails` |
| **Physical Table DDL Blocking** | Regex matching physical table DDL; allows `#temp` | Pluggable rule: `BlockPhysicalTableDdlRule` with temp table whitelist | `SafeDeployStudio.Guardrails` |
| **Allowed Object Types** | SP, Function, View regex enforcement | Configurable policy: `AllowedObjectTypesRule` (SP, Function, View, Table, Index) | `SafeDeployStudio.Guardrails` |
| **Collation & ASCII Safety** | Non-ASCII character count check | `AsciiComplianceRule` with configurable tolerance | `SafeDeployStudio.Guardrails` |
| **Custom Project Rules** | Hardcoded `@TenantId` check | Pluggable `ProjectRuleDefinition` (e.g. required parameters, naming conventions) | `SafeDeployStudio.Guardrails` |
| **Environment-Specific Policies** | Fixed DEV policy | Rule profiles per environment (Permissive Dev vs Strict Prod with approvals) | `SafeDeployStudio.Guardrails` |

---

## 6. Execution, Dry-Run & Deployment Engine

| Capability | Legacy Python Status | Proposed .NET 10 Design | Target Project Layer |
|---|---|---|---|
| **Transactional Dry-Run** | `BEGIN TRAN ... EXEC(@sql) ... ROLLBACK TRAN` | `IDeploymentExecutionService.ExecuteDryRunAsync()` using `SqlTransaction.Rollback()` | `SafeDeployStudio.Deployment` |
| **GO Batch Splitting** | Regex `(?im)^\s*GO\s*$` | Microsoft.SqlServer.TransactSql.ScriptDom or robust regex batch splitter | `SafeDeployStudio.Deployment` |
| **Pre-Change Snapshot Backup** | Saves `.sql` to local directory | `IBackupService.CreatePreDeploymentSnapshotAsync()` to centralized server vault | `SafeDeployStudio.Backup` |
| **Traceability Header Injection** | String concatenation into script before DDL | Structured header builder using configurable template engine | `SafeDeployStudio.Deployment` |
| **Multi-Target Deployment** | Python `for db in target_dbs:` loop | Parallel or sequential deployment runner with individual transaction boundaries | `SafeDeployStudio.Deployment` |
| **Deployment Verification** | Checks SQL execution exit code | Post-deployment verification querying target database to confirm new object state | `SafeDeployStudio.Deployment` |

---

## 7. Backup & Rollback System

| Capability | Legacy Python Status | Proposed .NET 10 Design | Target Project Layer |
|---|---|---|---|
| **Snapshot Generation** | Generates `DROP` + `CREATE` script from live DB | Provider-specific snapshot generation (`DROP IF EXISTS` + definition) | `SafeDeployStudio.Backup` |
| **Centralized Storage** | Fixed local path `D:\Project\...\Dev` | Configurable physical server path: `SafeDeployData/{OrgId}/{ProjId}/Backups/` | `SafeDeployStudio.Backup` |
| **Safe File Naming** | `DB_changes_YYYY_Mon_DD_HH_MM_SS_AMPM_{db}.sql` | Sanitized, timestamped, hash-verified filenames with metadata manifest | `SafeDeployStudio.Backup` |
| **1-Click Rollback** | Reads backup file and executes on DEV | `IRollbackService.ExecuteRollbackAsync(deploymentId)` with safety authorization | `SafeDeployStudio.Backup` |
| **Rollback Tracking** | Updates SQLite `status = 'ROLLED_BACK'` | Updates deployment audit record, logs rollback operator, timestamp, and verification | `SafeDeployStudio.Audit` |

---

## 8. AI Optimization Advisor

| Capability | Legacy Python Status | Proposed .NET 10 Design | Target Project Layer |
|---|---|---|---|
| **Deterministic Rule Engine** | Offline regex rules (`ISNULL` traps, `DATEFROMPARTS`, `STRING_AGG`) | C# static rules engine evaluating AST / T-SQL patterns | `SafeDeployStudio.AI` |
| **Delta-Targeted Analysis** | Analyzes only added/modified lines from diff | Evaluates diff hunk chunks to minimize tokens and focus recommendations | `SafeDeployStudio.AI` |
| **Multi-Provider AI Client** | Python `requests` calls to Ollama, Groq, OpenAI | `IAiAdvisorClient` with `HttpClientFactory` supporting OpenAI, Azure OpenAI, Groq, Ollama | `SafeDeployStudio.AI` |
| **Anti-Hallucination Recovery** | String & token matching in candidate SQL | AST substring validator ensuring replacement code exists verbatim in source | `SafeDeployStudio.AI` |
| **1-Click Adoption** | Replaces string in candidate text | Interactive diff patcher applying suggestion and triggering re-validation | `SafeDeployStudio.AI` |
| **Server-Side Key Protection** | Stored in local JSON file | Secured server-side configuration; never transmitted to browser or desktop client | `SafeDeployStudio.Server` |

---

## 9. User Interface & Desktop Client

| Capability | Legacy Python Status | Proposed .NET 10 Design | Target Project Layer |
|---|---|---|---|
| **Desktop Shell** | Native PyQt6 window + PyWebView alternative | WPF Desktop Client hosting Microsoft WebView2 (`SafeDeployStudio.Desktop`) | `SafeDeployStudio.Desktop` |
| **Web Application** | HTML/JS index.html in FastAPI | Blazor Web App (Interactive Server / WebAssembly) | `SafeDeployStudio.Web` |
| **Theme System** | Python dictionary + QSS dynamic strings | Modern CSS Design System with CSS variables (`--bg-surface`, `--accent`, etc.) | `SafeDeployStudio.Web` |
| **Guided Workflow** | Deploy Studio tab with left scope sidebar | 6-Step Guided Stepper Workflow (Project ➔ Changes ➔ Review ➔ Validate ➔ Deploy ➔ Result) | `SafeDeployStudio.Web` |
| **Audit Ledger Screen** | QTableWidget with SQLite query | Blazor DataGrid with server-side paging, filtering, sorting, and CSV export | `SafeDeployStudio.Web` |
| **Desktop Auto-Updates** | None | Version checking API endpoint (`/api/version/check`) + update notification | `SafeDeployStudio.Desktop` |

---

## 10. DevOps, Hosting & GitHub Automation

| Capability | Legacy Python Status | Proposed .NET 10 Design | Target Project Layer |
|---|---|---|---|
| **Backend Hosting** | Python `uvicorn server:app` on port 8080 | ASP.NET Core on Windows Server IIS (`AspNetCoreModuleV2`) | `SafeDeployStudio.Server` |
| **Public Showcase Website** | None | Static/Modern Responsive Landing Page on GitHub Pages (`safedeploy-studio-site`) | Public Website Repository |
| **Continuous Integration** | None | GitHub Actions: build, restore, unit tests, integration tests, security audit | `.github/workflows/ci.yml` |
| **Release Automation** | None | GitHub Actions: automated build, MSI/Zip installer packaging, SHA-256 generation, GitHub Releases | `.github/workflows/release.yml` |
| **IIS Deployment Workflow** | Manual batch file | GitHub Actions: Web Deploy / SSH / PowerShell IIS automation with protected secrets | `.github/workflows/iis-deploy.yml` |

---

*This matrix establishes 100% functional parity and architectural advancement for SafeDeploy Studio.*
