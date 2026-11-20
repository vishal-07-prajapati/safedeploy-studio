using SafeDeployStudio.Domain.Common;
using SafeDeployStudio.Domain.Enums;

namespace SafeDeployStudio.Domain.Entities;

public class DatabaseConnection : Entity
{
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public string Name { get; set; } = string.Empty;
    public DatabaseProviderType ProviderType { get; set; } = DatabaseProviderType.SqlServer;
    public string Server { get; set; } = "localhost";
    public int Port { get; set; } = 1433;
    public string DatabaseName { get; set; } = string.Empty;
    public AuthenticationType AuthType { get; set; } = AuthenticationType.WindowsIntegrated;
    public string? Username { get; set; }
    public string? EncryptedCredentialSecret { get; set; }
    public bool UseTlsSsl { get; set; } = true;
    public bool TrustServerCertificate { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
    public string? AdditionalParamsJson { get; set; }
    public bool IsActive { get; set; } = true;

    public DatabaseConnection() { }

    public DatabaseConnection(Guid projectId, string name, string server, string databaseName, DatabaseProviderType providerType = DatabaseProviderType.SqlServer)
    {
        ProjectId = projectId;
        Name = name;
        Server = server;
        DatabaseName = databaseName;
        ProviderType = providerType;
    }
}
