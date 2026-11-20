# LEGACY APPLICATION ANALYSIS: CE21 SafeDB DeployBot
**System Inspection, Architecture Audit & Reverse-Engineering Report**
*Reference Functional Baseline for SafeDeploy Studio .NET 10 Migration*

---

## 1. Executive Summary & Repository Map

The legacy application (`CE21 SafeDB DeployBot`) is a Python 3.14-based database deployment agent designed to automate the comparison, safety audit, transactional dry-run, backup, deployment, and rollback of Microsoft SQL Server database objects (specifically Stored Procedures, Functions, and Views).

```
D:\Project\CE21 DB Changes Backup\SafeDB_Agent\
│
├── config.py                 # Configuration loader, ODBC conn string builder, route enforcement
├── connections.json          # Local persistence for connection endpoints, AI keys, user identity
├── guardrails.py             # Security rule engine: AST/regex scan, table DDL blocking, ASCII safety
├── harness.py                # Isolated DEV sandbox dry-run execution inside rolled-back transaction
├── backup_manager.py         # Pre-deployment snapshot engine, audit header injector, SQLite ledger
├── diff_engine.py            # Normalized diff engine: whitespace collapsing, token diff, compact hunks
├── optimizer.py              # SARGability analyzer, deterministic rule engine, multi-provider AI advisor
├── compatibility.py          # Stored procedure parameter signature parser (backward compatibility)
│
├── qt_app.py                 # Native PyQt6 desktop studio (VS 2022 / DataGrip inspired architecture)
├── theme_manager.py          # Centralized dynamic design tokens & QSS styles (Dark & Light themes)
├── desktop_app.py            # PyWebView + Edge Chromium webview desktop bridge
├── server.py                 # FastAPI REST API (Intranet/IIS team server)
│
├── assets/branding/          # Custom SVG, PNG (1024x1024), and Windows ICO branding
├── screenshots/              # Visual verification test captures (8 high-res evidence files)
├── ui/index.html             # Legacy webworkbench HTML/CSS/JS frontend
└── widgets/                  # PyQt6 modular UI components:
    ├── icons.py              # Pure vector SVG renderer (QSvgRenderer)
    ├── nav_rail.py           # Collapsible left navigation rail (expanded / compact)
    ├── code_editor.py        # Syntax-highlighted SQL editor with gutter line numbers & diff bands
    ├── welcome_widget.py     # Developer overview dashboard with genuine topology & SQLite history
    ├── output_panel.py       # Collapsible bottom execution output & multi-tab audit logs
    ├── connections_page.py   # Decoupled server connection management page
    ├── ai_page.py            # AI advisor configuration & interactive test sandbox
    ├── guardrails_page.py    # Enterprise security policy reference & validator simulator
    └── settings_page.py      # Operator identity, theme mode, and system workbench preferences
```

---

## 2. Application Entry Points & Execution Flow

The legacy system has three distinct execution entry points:

1. **Native Windows Desktop Application (`qt_app.py`):**
   - Direct execution via `python qt_app.py` or `Launch_SafeDB_Studio.bat`.
   - Initializes `QApplication`, configures Windows `AppUserModelID` (`CE21.SafeDB.DeployBot.Studio`), and creates `SafeDBStudioMainWindow`.
   - Communicates directly in-process with `SafeDBApi` (`desktop_app.py`).

2. **Webview Desktop Application (`desktop_app.py`):**
   - Launched via `Launch_SafeDB_Webview_Studio.bat`.
   - Uses `pywebview` with Edge Chromium (`Microsoft.Web.WebView2`) hosting `ui/index.html`.
   - Exposes Python `SafeDBApi` as a JavaScript window object (`window.pywebview.api`).

3. **FastAPI Web & Intranet Team Server (`server.py`):**
   - Runs `uvicorn` listening on port `8080`.
   - Exposes REST endpoints (`/api/fetch_and_compare`, `/api/deploy_object`, `/api/rollback_deployment`, `/api/get_history`) mirroring `SafeDBApi` 1-to-1.
   - Serves `ui/index.html` as a single-page intranet application.

---

## 3. UI Screens, Widgets & User Experience

The application layout was recently redesigned from a cramped single-screen dashboard into an IDE-style workspace:

- **Top Application Header (`TopHeaderWidget`):**
  - Brand vector logo, version indicator (`v3.5.0`), hardcoded route pill (`LOCAL ➔ DEV`).
  - Active database connection health pill (`● CE21_0001 ➔ DEV Multi-Tenant`).
  - AI engine status pill (`● Qwen 3.8 / Groq`).
  - Guardrails enforcement pill (`● Guardrails Enforced`).
  - Operator identity attribution (`Operator: vishal.prajapati`).
  - Instant theme toggle button (Dark / Light) and Settings shortcut.

- **Left Navigation Rail (`LeftNavRail`):**
  - Collapsible navigation (210px expanded with labels; 54px compact icon-only).
  - Pure vector SVG icons: `Deploy Studio`, `Audit & Rollback`, `Backup Vault`, `Database Connections`, `AI Optimization Engine`, `Guardrails Policy`, and `Settings`.
  - Blue left-accent border indicator on active item (`3px solid #3997F7`).

- **Deploy Studio Workspace (`Page 0`):**
  - **Left Scope Panel (340px):** Migration scope (Ticket ID / Bug Title), Stored Procedure Name, Source Database selector (editable combo), Target DEV Scope selector (multi-tenant / root / custom), Quick-Pick stored procedure buttons with tooltips, `Fetch & Compare (F5)` button, Pre-flight guarantees summary card.
  - **Main Upper Stack:**
    - *View A (Welcome Dashboard):* Displays genuine connection topology, real SQLite recent operations ledger, quick action shortcuts, and fast-start CTA.
    - *View B (Side-by-Side SQL Diff):* Candidate SQL (Local) vs Active Baseline SQL (DEV) with gutter line numbers, active line highlighting, diff color bands, AI SARGability advisor box with 1-click adoption, and full-width green `Deploy to DEV Multi-Tenant` action bar.
  - **Bottom Output Console (`OutputLogsPanel`):** Collapsible panel with 3 log tabs (`Deployment Activity`, `Security Guardrails Audit`, `DEV Sandbox Dry-Run Trace`).

- **Dedicated Management Screens (`Pages 1–6`):**
  - **Audit & Rollback (`Page 1`):** Real-time KPI summary cards (Total Deployments, Active Deployed, Rolled Back, Unique Objects), live full-text search, database filter, status filter, SQLite history data table, and 1-click rollback trigger.
  - **Backup Vault Explorer (`Page 2`):** Lists all archived `.sql` pre-change snapshot files on disk with direct Windows Explorer integration.
  - **Database Connections (`Page 3`):** Server connection profile editor. Decouples host/credentials from specific database selection.
  - **AI Optimization Engine (`Page 4`):** AI provider configuration, model name, endpoint, API key, and interactive SARGability sandbox.
  - **Guardrails Policy (`Page 5`):** Comprehensive architectural security policies and interactive AST validation simulator.
  - **Settings & Preferences (`Page 6`):** Operator attribution username, appearance theme selection, and system metadata.

---

## 4. Database Connection Logic (`config.py` & `desktop_app.py`)

- **Driver:** Microsoft ODBC Driver 18 for SQL Server (`{ODBC Driver 18 for SQL Server}`).
- **Authentication Modes:**
  1. *Windows Authentication:* `Trusted_Connection=yes`.
  2. *SQL Server Authentication:* `UID={username};PWD={password}`.
  3. *Azure Active Directory (Service Principal / Password):* Configurable via connection string.
- **Security Parameters:** `TrustServerCertificate=yes` (configurable).
- **Environment Isolation:** Hardcoded check in `validate_environment()`:
  - Allowed Target: `DEV` only.
  - Forbidden Targets: `TEST`, `STAGE`, `STAGING`, `QA`, `PROD`, `PRODUCTION`. Any attempt to connect or target these throws `PermissionError`.
- **Decoupled Database Selection:**
  - Server connections (`connections.json`) store host and credentials with `database: ""`.
  - Specific databases (`CE21_0001`, `CE21_0002`, `CE21Root`) are chosen dynamically per migration in the Deploy Studio UI.

---

## 5. SQL Comparison Implementation (`diff_engine.py`)

- **Normalization:** `normalize_line()` collapses consecutive whitespace, tabs, and spaces.
- **Word/Token Level Diffing:** Uses Python's `difflib.SequenceMatcher` to identify character/token replacements within lines, preserving leading indentation.
- **Dual Display Modes:**
  1. *Differences Only (Compact Hunks):* Extracts changed rows with a configurable context margin (default: 3 lines before and after).
  2. *Full Procedure Text:* Displays entire definitions aligned side-by-side.
- **Data Matrix:** Returns synchronized rows:
  - `dev_lines` vs `cand_lines`
  - Status flags: `equal`, `del`, `add`, `modified`, `empty`
  - Line numbers for both left and right gutters.

---

## 6. SQL Deployment Implementation (`backup_manager.py`)

Deployment follows an atomic, multi-step pipeline:

```
[1. User clicks Deploy]
       │
       ▼
[2. Environment Verification: validate_environment(ALLOWED_TARGET_ENV)]
       │
       ▼
[3. Fetch Current Target Object Definition via sys.sql_modules]
       │
       ▼
[4. Create Pre-Change Snapshot File (.sql) in Backup Vault]
       │
       ▼
[5. Inject Audit Traceability Header into Candidate Script]
       │
       ▼
[6. Execute Script on Target Database via pyodbc (splitting on GO batches)]
       │
       ▼
[7. Insert Audit Record into SQLite history_index.sqlite (Status: DEPLOYED)]
       │
       ▼
[8. Append plain-text entry to deployment_history.log]
```

- **Multi-Tenant Deployment Loop:** For tenant-scoped procedures, the engine iterates over target databases (e.g. `CE21_0001` and `CE21_0002`), creating a dedicated pre-change backup for each database prior to executing the DDL.
- **Traceability Header Injection:** Prepends audit metadata (Operator, Ticket, Target DB, Timestamp, Pre-Change Backup Path) directly into the deployed script on SQL Server.

---

## 7. Guardrail Implementation (`guardrails.py`)

The safety engine validates SQL scripts before dry-run or deployment:

1. **Forbidden Administrative Operations:**
   - Permanent blocks on: `ALTER SCHEMA`, `DROP SCHEMA`, `CREATE SCHEMA`, `DROP DATABASE`, `ALTER DATABASE`, `EXEC sp_rename`, `xp_cmdshell`.
2. **Physical Table DDL Blocking:**
   - Blocks: `CREATE TABLE`, `ALTER TABLE`, `DROP TABLE`, `TRUNCATE TABLE`, `SELECT ... INTO ...` targeting physical schema tables.
   - **Allowed Exception:** Temporary tables (`#temp`, `##temp`, `tempdb..#temp`) and table variables (`@table`) are permitted for DDL and DML.
3. **Allowed Object Scope:**
   - Only `STORED PROCEDURE`, `FUNCTION`, and `VIEW` are allowed.
   - Normalized into `CREATE OR ALTER` or `DROP AND CREATE`.
4. **Encoding & ASCII Integrity:**
   - Scans for non-ASCII characters (> 127) to prevent collation mismatches and encoding corruption.
5. **Legacy Domain Check:**
   - Flags missing `@TenantId` parameter for tenant-scoped stored procedures.

---

## 8. Transaction & Dry-Run Functionality (`harness.py`)

- **Method:** `DryRunHarness.test_in_sandbox(sql_script, dev_conn_str)`
- **Mechanism:**
  - Connects to the target DEV Azure SQL instance with `autocommit=False`.
  - Splits script into batches using regex `(?im)^\s*GO\s*$`.
  - Executes every batch sequentially within an open SQL transaction.
  - Calls `conn.rollback()` immediately regardless of outcome.
- **Safety Guarantee:** Tests SQL Server compilation, object references, missing columns, and schema validity against live schema objects without altering the database state.

---

## 9. AI Integration (`optimizer.py`)

- **Hybrid Architecture:**
  1. *Deterministic Local Rule Engine:* Runs offline instantly at zero token cost. Detects `ISNULL` traps, `DATEFROMPARTS` opportunities, `STRING_AGG` replacements, `STRING_SPLIT` upgrades, and cursor anti-patterns.
  2. *Generative AI Advisor:* Calls external LLM providers (Groq, OpenAI, Ollama, Gemini, Claude) via REST API.
- **Delta-Targeting:** Restricts AI analysis strictly to lines added or modified in the candidate script.
- **Anti-Hallucination Recovery (`recover_and_sanitize_suggestion`):**
  - Rejects placeholder responses ("your code here", "todo").
  - Auto-recovers verbatim `before_code` from candidate SQL.
  - Synthesizes exact, verified SARGable T-SQL replacements.
- **1-Click Adoption:** Replaces the target code in `cand_sql` in-memory and triggers automatic recomputation of diffs and guardrail checks.

---

## 10. Backup Implementation (`backup_manager.py`)

- **Storage Directory:** `D:\Project\CE21 DB Changes Backup\Dev\`
- **File Naming Pattern:** `DB_changes_YYYY_Mon_DD_HH_MM_SS_AMPM_{db_name}.sql`
- **Snapshot Contents:**
  - Header with object metadata, timestamp, ticket ID, and developer name.
  - Preamble: `IF EXISTS (SELECT * FROM sys.objects ...) DROP {type} [{name}] GO`.
  - Verbatim original SQL definition fetched from `sys.sql_modules` / `OBJECT_DEFINITION`.
  - If object did not previously exist: writes comment `-- (Object did not previously exist on DEV)`.

---

## 11. Rollback Implementation (`backup_manager.py`)

- **Method:** `BackupManager.rollback(deployment_id, dev_conn_str)`
- **Execution:**
  1. Queries `deployment_history` by `deployment_id`.
  2. Validates that current status is `DEPLOYED` (not already rolled back).
  3. Verifies physical existence of `backup_filepath` on disk.
  4. Reads pre-change backup script.
  5. Executes backup script on target database (reverting object to previous state).
  6. Updates SQLite record: `status = 'ROLLED_BACK'`, `rolled_back_at = {timestamp}`.
  7. Appends entry to `deployment_history.log`.

---

## 12. Audit Logging (`backup_manager.py`)

- **SQLite Database:** `D:\Project\CE21 DB Changes Backup\history_index.sqlite`
- **Schema:**
  ```sql
  CREATE TABLE deployment_history (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      object_name TEXT NOT NULL,
      object_type TEXT NOT NULL,
      database_name TEXT NOT NULL DEFAULT '',
      ticket_title TEXT NOT NULL,
      developer TEXT NOT NULL,
      target_env TEXT NOT NULL,
      backup_filepath TEXT NOT NULL,
      deployed_at TEXT NOT NULL,
      status TEXT NOT NULL, -- 'DEPLOYED', 'ROLLED_BACK'
      rolled_back_at TEXT
  );
  ```
- **Text Log File:** `D:\Project\CE21 DB Changes Backup\deployment_history.log` (append-only audit log).

---

## 13. Configuration Management (`config.py` & `connections.json`)

Stored locally in `connections.json`:
- `audit.developer_name`: Username attributed to deployments.
- `local`: Server, auth mode, username, password.
- `dev`: Server, auth mode, username, password.
- `ai`: Enabled flag, provider (`ollama`, `groq`, `openai`, `gemini`), model name, API key, endpoint URL.
- `ui`: Theme mode (`dark` / `light`).

---

## 14. Existing Tests & Verification Suite

- **Capture Suite (`verify_and_capture.py`):** Instantiates `SafeDBStudioMainWindow`, verifies all 7 pages, captures 8 full-resolution screenshots, checks layout bounds and theme switching.
- **Manual Verification:** Tested against live local and DEV Azure SQL instances.

---

## 15. Dependencies & External Services

- Python 3.14
- PyQt6 (GUI)
- pyodbc (SQL Server ODBC Driver 18)
- sqlite3 (Local audit ledger)
- requests (AI provider HTTP calls)
- uvicorn / fastapi (Web server)
- pywebview (Edge WebView2 wrapper)

---

## 16. Existing Hardcoded Restrictions & Assumptions

1. **CE21-Specific Logic:**
   - Database naming assumptions (`CE21_0001`, `CE21_0002`, `CE21Root`).
   - Tenant isolation check specifically looking for `@TenantId`.
   - File path hardcoded to `D:\Project\CE21 DB Changes Backup`.
2. **Single-Route Restriction:**
   - Hardcoded to `Local ➔ DEV`.
   - In a multi-project enterprise tool, deployment routes must be configurable per project and environment (e.g. `Dev ➔ QA`, `QA ➔ Staging`, `Staging ➔ Prod` with approval gates).
3. **Single Database Provider:**
   - Logic is tied directly to SQL Server T-SQL syntax and `pyodbc`.

---

*This audit forms the baseline for the Functionality Migration Matrix.*
