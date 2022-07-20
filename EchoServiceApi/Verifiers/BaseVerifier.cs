using NetLah.Extensions.Configuration;

namespace EchoServiceApi.Verifiers;

public abstract class BaseVerifier
{
    private IConfiguration? _configuration;
    private TokenCredentialFactory? _tokenCredentialFactory;

    protected BaseVerifier(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public IConfiguration Configuration => _configuration ??= ServiceProvider.GetRequiredService<IConfiguration>();

    public TokenCredentialFactory TokenFactory => _tokenCredentialFactory ??= ServiceProvider.GetRequiredService<TokenCredentialFactory>();

    public IServiceProvider ServiceProvider { get; }

    protected ProviderConnectionString GetConnection(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        IConnectionStringManager connectionStringManager = new ConnectionStringManager(Configuration);
        var connectionObj = connectionStringManager[name] ?? throw new Exception($"Connection string '{name}' not found");
        return connectionObj;
    }
}
