using Microsoft.Data.SqlClient;
using SafeDeployStudio.Domain.Entities;
using SafeDeployStudio.Domain.Enums;

namespace SafeDeployStudio.DatabaseProviders.SqlServer;

public static class SqlServerConnectionStringBuilder
{
    public static string Build(DatabaseConnection connection, string? overrideDatabase = null, string? decryptedPassword = null)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = connection.Port == 1433 || connection.Port <= 0
                ? connection.Server
                : $"{connection.Server},{connection.Port}",
            InitialCatalog = string.IsNullOrWhiteSpace(overrideDatabase)
                ? (string.IsNullOrWhiteSpace(connection.DatabaseName) ? "master" : connection.DatabaseName)
                : overrideDatabase,
            ConnectTimeout = connection.TimeoutSeconds > 0 ? connection.TimeoutSeconds : 30,
            TrustServerCertificate = connection.TrustServerCertificate,
            Encrypt = connection.UseTlsSsl ? SqlConnectionEncryptOption.Mandatory : SqlConnectionEncryptOption.Optional,
            ApplicationName = "SafeDeployStudio"
        };

        switch (connection.AuthType)
        {
            case AuthenticationType.WindowsIntegrated:
                builder.IntegratedSecurity = true;
                break;

            case AuthenticationType.SqlAuthentication:
                builder.IntegratedSecurity = false;
                builder.UserID = connection.Username ?? string.Empty;
                builder.Password = decryptedPassword ?? string.Empty;
                break;

            case AuthenticationType.AzureActiveDirectory:
                builder.Authentication = SqlAuthenticationMethod.ActiveDirectoryDefault;
                break;
        }

        return builder.ConnectionString;
    }
}
