using NetLah.Extensions.Configuration;

namespace EchoServiceApi.Verifiers;

public abstract class BaseVerifier
{
    private IConfiguration? _configuration;
    private TokenCredentialFactory? _tokenCredentialFactory;
    private ILogger? _logger;
    private DiagnosticInfo? _diagnosticInfo;

    protected BaseVerifier(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public IConfiguration Configuration => _configuration ??= ServiceProvider.GetRequiredService<IConfiguration>();

    public TokenCredentialFactory TokenFactory => _tokenCredentialFactory ??= ServiceProvider.GetRequiredService<TokenCredentialFactory>();

    public IServiceProvider ServiceProvider { get; }

    public ILogger Logger => _logger ??= ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());

    public DiagnosticInfo DiagnosticInfo => _diagnosticInfo ??= ServiceProvider.GetRequiredService<DiagnosticInfo>();

    protected ProviderConnectionString GetConnection(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        IConnectionStringManager connectionStringManager = new ConnectionStringManager(Configuration);
        var connectionObj = connectionStringManager[name] ?? throw new Exception($"Connection string '{name}' not found");
        return connectionObj;
    }

    protected IDisposable LoggerBeginScopeDiagnostic() => Logger.BeginScope(DiagnosticInfo.LoggingScopeState) ?? throw new Exception("Logger.BeginScope null");
}
