using NetLah.Extensions.Configuration;

namespace EchoServiceApi.Verifiers
{
    public abstract class BaseVerifier
    {
        protected BaseVerifier(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public abstract Task<VerifyResult> VerifyAsync(string name);

        protected ProviderConnectionString GetConnection(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            IConnectionStringManager connectionStringManager = new ConnectionStringManager(Configuration);
            var connectionObj = connectionStringManager[name] ?? throw new Exception($"Connection string '{name}' not found");
            return connectionObj;
        }
    }
}
