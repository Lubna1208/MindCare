# MindCare local setup

This project targets .NET 10, SQL Server Express, Entity Framework Core, and ASP.NET Core Identity.
The application code and its existing migrations are already in place. The steps below create or update the local database using those migrations; they do not drop a database or delete application data.

## Prerequisites

1. Install the .NET 10 SDK.
2. Install SQL Server Express (or use an existing SQL Server instance) and ensure the `SQL Server (SQLEXPRESS)` service is running.
3. Ensure the Windows account running the commands can create `MindCareDb` on that SQL instance. For an existing database, it needs permission to apply schema migrations.

## Configure the connection

The development connection is in `MindCare/appsettings.json` and defaults to local SQL Express:

```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=MindCareDb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true"
```

For a different instance, do not commit a personal connection string. In PowerShell for the current session, set an override before running the commands:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=YOUR_SERVER;Database=MindCareDb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true"
```

## Restore, migrate, and run

From the repository root:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project MindCare\MindCare.csproj --startup-project MindCare\MindCare.csproj
dotnet run --project MindCare\MindCare.csproj
```

`database update` applies the committed migrations in order. On the first successful application startup, Identity roles and the configured administrator account are seeded automatically.

## Initial administrator

The initial administrator values are currently configured in `MindCare/appsettings.json`:

- Email: `admin@mindcare.local`
- Password: `Admin@12345`

Use them only for the first local login, then change the password. Before sharing, deployment, or committing a real environment configuration, override `SeedAdmin__Password` through a secret store or environment variable and use a strong unique password. Do not put real credentials in source control.

## If migration reports `Cannot generate SSPI context`

For a local, non-domain Windows account, do not register or change Kerberos SPNs. Use a TCP loopback connection to bypass named-instance discovery and Kerberos negotiation.

1. Open **SQL Server Configuration Manager** as Administrator.
2. Go to **SQL Server Network Configuration** > **Protocols for SQLEXPRESS** and enable **TCP/IP**.
3. Open **TCP/IP** > **Properties** > **IP Addresses**. Under **IPAll**, clear `TCP Dynamic Ports` and set `TCP Port` to `1433` (only if that port is unused).
4. Restart **SQL Server (SQLEXPRESS)**. SQL Browser is not needed when the connection explicitly supplies the port.
5. In a new PowerShell window, verify that the instance is listening:

```powershell
Test-NetConnection 127.0.0.1 -Port 1433
```

6. Set a process-only connection-string override and migrate. This changes neither source code nor the tracked configuration file:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=tcp:127.0.0.1,1433;Database=MindCareDb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true"
dotnet tool run dotnet-ef database update --project MindCare\MindCare.csproj --startup-project MindCare\MindCare.csproj
```

If SQL Server then says `Failed to open the explicitly specified database 'MindCareDb'`, the Windows login has reached SQL Server and the database simply does not exist yet (or is inaccessible). Connect to `master` in SSMS as a SQL Server administrator and run:

```sql
IF DB_ID(N'MindCareDb') IS NULL
    CREATE DATABASE [MindCareDb];
GO
```

Then rerun the EF migration command. The SQL statement is conditional: it creates the database only when absent and never deletes or overwrites one.

## Verification

After `database update` succeeds, SQL Server should contain a `MindCareDb` database with the EF migration history table (`__EFMigrationsHistory`). After the first application startup, it will also have the Identity roles `Admin`, `Counsellor`, and `User`.
