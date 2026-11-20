# SafeDeploy Studio — Guardrails & Safety Engine Guide

## Overview

Database procedural changes often contain accidental or dangerous statements that can lead to data loss, table locks, or schema corruption. SafeDeploy Studio prevents this with an AST-based static analysis engine powered by `Microsoft.SqlServer.TransactSql.ScriptDom`.

---

## Built-In Safety Rules

### 1. Physical Table DDL Blocker (`BlockPhysicalTableDdlRule`)
- **Severity**: `CRITICAL` / `ERROR`
- **Mechanism**: Traverses the T-SQL Abstract Syntax Tree for:
  - `DropTableStatement`
  - `TruncateTableStatement`
  - `AlterTableStatement`
- **Behavior**:
  - If the identifier starts with `#` or `##` (e.g. `#TempUsers`), the statement is **permitted** because temporary tables are standard in procedural ETL.
  - If the identifier is a physical table (e.g. `DROP TABLE dbo.Orders`), the deployment is **blocked immediately**.

### 2. Forbidden Administrative Keywords (`ForbiddenAdminKeywordsRule`)
- **Severity**: `CRITICAL`
- **Keywords**: `xp_cmdshell`, `sp_configure`, `SHUTDOWN`, `DROP DATABASE`, `RECONFIGURE`.
- **Behavior**: Scans non-commented code lines and flags any unauthorized system commands.

### 3. ASCII & Encoding Gate (`AsciiComplianceRule`)
- **Severity**: `WARNING`
- **Mechanism**: Inspects character values (`c > 127`).
- **Target Items**:
  - Smart double quotes (`“`, `”`)
  - Smart single quotes (`‘`, `’`)
  - Em-dashes and En-dashes (`—`, `–`)
  - Non-breaking spaces (`\u00A0`)
- **Remediation**: Reports exact line and column numbers and guides replacing them with standard 7-bit ASCII equivalents.

### 4. Unqualified UPDATE / DELETE Trap (`UnqualifiedUpdateDeleteRule`)
- **Severity**: `ERROR`
- **Mechanism**: Evaluates `UpdateStatement.UpdateSpecification.WhereClause` and `DeleteStatement.DeleteSpecification.WhereClause`.
- **Behavior**: If missing, blocks execution to prevent unintended whole-table updates or deletions.

### 5. Deterministic SARGability Analyzer (`SargabilityAnalyzer`)
- **Rules**:
  - `SARG001`: Non-SARGable date function on predicate column (`YEAR(Col) = ...`).
  - `SARG002`: Non-SARGable `ISNULL(Col, '') = ...` on predicate.
  - `SARG003`: Leading wildcard search (`LIKE '%abc'`).
  - `PERF001`: `SELECT *` inside stored procedures.
  - `PERF002`: T-SQL `CURSOR` declaration.
  - `PERF003`: Missing `SET NOCOUNT ON;`.
