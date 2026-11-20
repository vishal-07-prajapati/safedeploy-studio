# SafeDeploy Studio — Getting Started Guide

## Prerequisites

1. **Operating System**: Windows 10 / Windows 11 / Windows Server 2019+ (64-bit).
2. **.NET SDK**: .NET 10.0 SDK or .NET 10.0 Desktop Runtime.
3. **Database**: Microsoft SQL Server 2016+, Azure SQL Database, or SQL Server Express / LocalDB.
4. **WebView2**: Microsoft Edge WebView2 Evergreen Runtime (pre-installed on Windows 11).

---

## 1. Quick Start (Web Application)

To run the full Blazor Server workbench:

```bash
cd D:\Project\safedeploy-studio
dotnet run --project src/SafeDeployStudio.Web/SafeDeployStudio.Web.csproj
```

Navigate to: `https://localhost:7199` (or the URL displayed in the console).

The application will automatically initialize the local SQLite database at `%LocalAppData%/SafeDeployStudio/safedeploy.db` and seed standard initial organizations, projects, and environments.

---

## 2. Quick Start (WPF Windows Desktop Client)

To run the native Windows application:

```bash
dotnet run --project src/SafeDeployStudio.Desktop/SafeDeployStudio.Desktop.csproj
```

Features in Desktop Shell:
- Native Windows Menu Bar (`File`, `Edit`, `View`, `Tools`, `Help`).
- Direct access to local Backup Vault explorer via `Tools -> Open Backup Vault Explorer`.
- Embedded WebView2 client pointing to local or hosted SafeDeploy server.
- Native status bar with provider and ready status indicators.

---

## 3. Running the REST API & SignalR Service

To run the headless backend service with Swagger documentation:

```bash
dotnet run --project src/SafeDeployStudio.Api/SafeDeployStudio.Api.csproj
```

Swagger UI will be available at: `https://localhost:7198/swagger`.

---

## 4. Running the Automated Test Suite

```bash
dotnet test SafeDeployStudio.slnx
```

This executes all unit and integration tests across:
- Guardrail AST visitor checks
- SQL Server provider batch splitting
- DiffPlex diff generator
- SARGability anti-pattern detector
- Repository data operations
