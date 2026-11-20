# SafeDeploy Studio — Architecture & Technical Specifications

## 1. Executive Architectural Blueprint

SafeDeploy Studio is built as a **Modular Monolith** adhering strictly to **Clean Architecture** and **Domain-Driven Design (DDD)** principles. It completely decouples database change management, diff calculation, safety verification, and execution from any specific proprietary project or database vendor.

```
+-----------------------------------------------------------------------------------+
|                            Presentation Layer                                     |
|     SafeDeployStudio.Desktop (WPF + WV2)    |    SafeDeployStudio.Web (Blazor)    |
|                         SafeDeployStudio.Api (REST / SignalR)                     |
+-----------------------------------------------------------------------------------+
                                       |
                                       v
+-----------------------------------------------------------------------------------+
|                            Application Layer                                      |
|                       SafeDeployStudio.Application                                |
|        (Use Cases, DTOs, Workflow Orchestration, Procedure Pipelines)            |
+-----------------------------------------------------------------------------------+
           |                     |                      |                    |
           v                     v                      v                    v
+---------------------+ +--------------------+ +------------------+ +---------------+
| Guardrails Engine   | | Deployment Engine  | | Backup Vault     | | AI Advisor    |
| (ScriptDom AST      | | (DiffPlex, Normal- | | (Pre-Change      | | (SARGability  |
|  Visitor Rules)     | |  ization, Dry-Run) | |  Snapshots, SHA) | |  Analyzer)    |
+---------------------+ +--------------------+ +------------------+ +---------------+
           \                     |                      /                    /
            \                    v                     /                    /
+-----------------------------------------------------------------------------------+
|                        Database Provider Abstractions                             |
|                 SafeDeployStudio.DatabaseProviders.Abstractions                   |
|           (IDatabaseProvider, IDatabaseProviderFactory, IScriptBatchSplitter)     |
+-----------------------------------------------------------------------------------+
                                       |
                                       v
+-----------------------------------------------------------------------------------+
|                     Database Provider Implementations                             |
|                  SafeDeployStudio.DatabaseProviders.SqlServer                     |
|           (Microsoft.Data.SqlClient, Dapper, TSql160Parser ScriptDom)             |
+-----------------------------------------------------------------------------------+
                                       |
                                       v
+-----------------------------------------------------------------------------------+
|                               Domain Core                                         |
|                          SafeDeployStudio.Domain                                  |
|         (Aggregates: Organization, Project, Environment, DeploymentRecord)        |
+-----------------------------------------------------------------------------------+
                                       ^
                                       |
+-----------------------------------------------------------------------------------+
|                            Infrastructure Layer                                   |
|                        SafeDeployStudio.Infrastructure                            |
|             (EF Core SQLite / SQL Server Repositories, DatabaseSeeder)            |
+-----------------------------------------------------------------------------------+
```

---

## 2. Project Breakdown & Responsibilities

| Project | Namespace | Target | Responsibilities |
|---|---|---|---|
| **SafeDeployStudio.Domain** | `SafeDeployStudio.Domain` | `net10.0` | Core entities, value objects, domain enums, repository interfaces. Zero external dependencies. |
| **SafeDeployStudio.DatabaseProviders.Abstractions** | `SafeDeployStudio.DatabaseProviders.Abstractions` | `net10.0` | Provider contracts (`IDatabaseProvider`), metadata models, batch splitting abstractions. |
| **SafeDeployStudio.DatabaseProviders.SqlServer** | `SafeDeployStudio.DatabaseProviders.SqlServer` | `net10.0` | Microsoft SQL Server provider using `Microsoft.Data.SqlClient`, `Dapper`, and `ScriptDom`. |
| **SafeDeployStudio.Guardrails** | `SafeDeployStudio.Guardrails` | `net10.0` | Static code analysis via ScriptDom AST visitors: physical table DDL blocks, ASCII compliance, unqualified UPDATE/DELETE checks. |
| **SafeDeployStudio.Deployment** | `SafeDeployStudio.Deployment` | `net10.0` | Line-by-line SQL diff engine via `DiffPlex`, whitespace normalization, transactional dry-run orchestration. |
| **SafeDeployStudio.Backup** | `SafeDeployStudio.Backup` | `net10.0` | Automated pre-change snapshot generation (`DROP IF EXISTS` + original DDL), SHA-256 vault hashing, 1-click rollback execution. |
| **SafeDeployStudio.AI** | `SafeDeployStudio.AI` | `net10.0` | SARGability query analyzer, index anti-pattern detection (`DATEPART`, `ISNULL` traps), multi-provider LLM client. |
| **SafeDeployStudio.Application** | `SafeDeployStudio.Application` | `net10.0` | CQRS use cases, DTOs, cross-cutting workflow orchestration services. |
| **SafeDeployStudio.Infrastructure** | `SafeDeployStudio.Infrastructure` | `net10.0` | EF Core SQLite/SQL Server repositories, database initialization, automated seeding. |
| **SafeDeployStudio.Api** | `SafeDeployStudio.Api` | `net10.0` | REST API, OpenAPI/Swagger docs, SignalR deployment log hub. |
| **SafeDeployStudio.Web** | `SafeDeployStudio.Web` | `net10.0` | Blazor Server web application with 5-item developer navigation and 6-step guided deployment stepper. |
| **SafeDeployStudio.Desktop** | `SafeDeployStudio.Desktop` | `net10.0-windows` | Native WPF Windows application hosting Microsoft WebView2, native menu bar, and status bar. |
| **SafeDeployStudio.UnitTests** | `SafeDeployStudio.UnitTests` | `net10.0` | xUnit, Moq, FluentAssertions test suite for guardrails, AST parser, and diff engine. |
| **SafeDeployStudio.IntegrationTests** | `SafeDeployStudio.IntegrationTests` | `net10.0` | DatabaseSeeder and in-memory EF Core repository integration tests. |

---

## 3. Multi-Tenant / Multi-Project Hierarchy

```
Organization (e.g., Contoso Enterprise)
 └── Project (e.g., Core Billing DB, Platform Engine)
      ├── Guardrail Policy (Rules, Regexes, DDL Blocks)
      ├── Database Connections (Encrypted Credentials, Ports, Servers)
      ├── Environments (Dev, Staging, Production)
      ├── Deployment Routes (Dev -> Staging, Staging -> Production [Approval Required])
      ├── Deployment Records (Audit Trail, Executed Logs, Durations)
      └── Backup Vault Snapshots (SHA-256 Checksums, Timestamped .sql files)
```

---

## 4. Pre-Flight Verification & Transactional Execution Lifecycle

1. **Static Analysis & Guardrails Gate**:
   - The proposed SQL script is parsed by `Microsoft.SqlServer.TransactSql.ScriptDom.TSql160Parser`.
   - The AST visitor inspects all statements:
     - Any `DropTableStatement`, `TruncateTableStatement`, or `AlterTableStatement` targeting non-temporary tables (`!StartsWith('#')`) triggers a `CRITICAL` or `ERROR` blocking violation.
     - Any administrative commands (`xp_cmdshell`, `sp_configure`, `SHUTDOWN`) are blocked.
     - ASCII check flags any smart quotes (`“`, `”`) or Unicode characters that could corrupt SQL parsing.
     - Any `UpdateStatement` or `DeleteStatement` lacking a `WhereClause` is blocked.
2. **Transactional Sandbox Dry-Run**:
   - A connection is opened to the target database.
   - The script is wrapped in an explicit transaction:
     ```sql
     SET XACT_ABORT ON;
     BEGIN TRANSACTION;
     -- Execute migration batches
     ROLLBACK TRANSACTION;
     ```
   - All server messages (`PRINT`, `RAISERROR`) and syntax errors are captured.
   - The transaction is **always rolled back** during the dry-run, ensuring zero side effects.
3. **Vault Snapshot Capture**:
   - Before executing live changes, the current target database definition is fetched via `OBJECT_DEFINITION(OBJECT_ID(...))`.
   - A self-contained, idempotent rollback script is generated (`DROP IF EXISTS` + previous script definition).
   - Saved to disk under `%LocalAppData%/SafeDeployStudio/Vault/{project}/{environment}/{timestamp}.sql`.
   - The SHA-256 hash is recorded in `BackupSnapshot`.
4. **Live Migration Execution**:
   - Script is split into discrete batches by `IScriptBatchSplitter` (token-aware ScriptDom).
   - Executed within an atomic transaction.
   - Real-time logs are streamed to the frontend via SignalR / Action callback.
   - On exception, automatic rollback is triggered and recorded.
