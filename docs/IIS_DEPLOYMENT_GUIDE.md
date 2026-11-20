# SafeDeploy Studio — Windows Server IIS Deployment Guide

## Overview

SafeDeploy Studio is designed to run reliably under **Windows Server IIS** using the **ASP.NET Core Hosting Bundle (AspNetCoreModuleV2)**.

---

## 1. Prerequisites on Windows Server

1. **Install IIS**: Ensure the Web Server (IIS) role is installed with HTTP Redirection and WebSocket Protocol enabled.
2. **Install ASP.NET Core Hosting Bundle**: Install the **.NET 10.0 Hosting Bundle** (or latest matching runtime).
3. **Verify Module**: Ensure `AspNetCoreModuleV2` appears under IIS Modules.

---

## 2. Publish the Application

From your build workstation or CI/CD runner:

```bash
dotnet publish src/SafeDeployStudio.Web/SafeDeployStudio.Web.csproj --configuration Release --output D:\Websites\SafeDeployStudio
```

---

## 3. IIS Configuration

1. **Application Pool**:
   - Create a new Application Pool: `SafeDeployStudioPool`.
   - .NET CLR Version: **No Managed Code**.
   - Managed Pipeline Mode: **Integrated**.
   - Identity: `ApplicationPoolIdentity` (or dedicated service account).
2. **Web Site**:
   - Create a new website or application pointing to `D:\Websites\SafeDeployStudio`.
   - Bind to desired hostname and HTTPS port with a valid SSL/TLS certificate.
3. **File Permissions**:
   - Grant `IIS AppPool\SafeDeployStudioPool` **Read & Execute** permissions to the site directory.
   - Grant `IIS AppPool\SafeDeployStudioPool` **Modify** permissions to `%LocalAppData%\SafeDeployStudio` for SQLite database and Backup Vault files.

---

## 4. Production web.config Template

An `AspNetCoreModuleV2` out-of-process configuration is generated upon publishing:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet"
                  arguments=".\SafeDeployStudio.Web.dll"
                  stdoutLogEnabled="true"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="inprocess" />
    </system.webServer>
  </location>
</configuration>
```
