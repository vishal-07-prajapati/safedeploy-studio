using SafeDeployStudio.Domain.Enums;

namespace SafeDeployStudio.DatabaseProviders.Abstractions;

public interface IDatabaseProviderFactory
{
    IDatabaseProvider GetProvider(DatabaseProviderType providerType);
}

public class DatabaseProviderFactory : IDatabaseProviderFactory
{
    private readonly IEnumerable<IDatabaseProvider> _providers;

    public DatabaseProviderFactory(IEnumerable<IDatabaseProvider> providers)
    {
        _providers = providers;
    }

    public IDatabaseProvider GetProvider(DatabaseProviderType providerType)
    {
        var provider = _providers.FirstOrDefault(p => p.ProviderType == providerType);
        if (provider == null)
        {
            throw new NotSupportedException($"Database provider '{providerType}' is not registered or supported in this runtime.");
        }
        return provider;
    }
}
