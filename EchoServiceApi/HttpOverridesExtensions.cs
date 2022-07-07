using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace EchoServiceApi;

public static class HttpOverridesExtensions
{
    private static bool _notForwardedHeadersEnabled;

    public static IServiceCollection AddHttpOverrides(this IServiceCollection services, IConfiguration configuration, string httpOverridesSectionName = "HttpOverrides")
    {
        var forwardedHeadersEnabled = configuration["ASPNETCORE_FORWARDEDHEADERS_ENABLED"];
        _notForwardedHeadersEnabled = !string.Equals(forwardedHeadersEnabled, "true", StringComparison.OrdinalIgnoreCase);
        if (_notForwardedHeadersEnabled)
        {
            var configurationSection = string.IsNullOrEmpty(httpOverridesSectionName) ? configuration : configuration.GetSection(httpOverridesSectionName);
            services.Configure<ForwardedHeadersOptions>(configurationSection);

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                ProcessKnownProxies(configurationSection, options);

                ProcessKnownNetworks(configurationSection, options);
            });
        }

        return services;
    }

    public static IApplicationBuilder UseHttpOverrides(this IApplicationBuilder app, ILogger logger)
    {
        var sp = app.ApplicationServices;
        var optionsForwardedHeadersOptions = sp.GetRequiredService<IOptions<ForwardedHeadersOptions>>();
        var fho = optionsForwardedHeadersOptions.Value;

        var hostFilteringOptions = sp.GetRequiredService<IOptions<Microsoft.AspNetCore.HostFiltering.HostFilteringOptions>>();
        if (hostFilteringOptions?.Value is { } hostFiltering)
        {
            logger.LogInformation("HostFiltering: {@hostFiltering}", hostFiltering);
        }

        if (fho.KnownProxies.Count > 0 || fho.KnownNetworks.Count > 0 || fho.ForwardedHeaders != ForwardedHeaders.None)
        {
            logger.LogInformation("ForwardLimit: {forwardLimit}", fho.ForwardLimit);
        }

        if (fho.KnownProxies.Count > 0)
        {
            var knownProxies = string.Join(",", fho.KnownProxies);
            logger.LogInformation("KnownProxies: {knownProxies}", knownProxies);
        }

        if (fho.KnownNetworks.Count > 0)
        {
            var knownNetworks = string.Join(",", fho.KnownNetworks.Select(net => ToString(net)));
            logger.LogInformation("KnownNetworks: {knownNetworks}", knownNetworks);
            static string ToString(IPNetwork ipNetwork) => $"{ipNetwork.Prefix}/{ipNetwork.PrefixLength}";
        }

        if (fho.ForwardedHeaders != ForwardedHeaders.None)
        {
            var forwardedHeaders = string.Join(",", fho.ForwardedHeaders);
            if (_notForwardedHeadersEnabled)
            {
                app.UseForwardedHeaders();
                logger.LogInformation("Use ForwardedHeaders: {forwardedHeaders}", forwardedHeaders);
            }
            else
            {
                logger.LogInformation("ForwardedHeaders: {forwardedHeaders}", forwardedHeaders);
            }
        }

        if (fho.AllowedHosts.Count > 0)
        {
            var allowedHosts = string.Join(",", fho.AllowedHosts);
            logger.LogInformation("AllowedHosts: {allowedHosts}", allowedHosts);
        }

        return app;
    }

    private static void ProcessKnownNetworks(IConfiguration configuration, ForwardedHeadersOptions options)
    {
        var knownNetworks = configuration["KnownNetworks"];
        var clearKnownNetworks = configuration["ClearKnownNetworks"];
        if (knownNetworks != null || string.Equals(clearKnownNetworks, "true", StringComparison.OrdinalIgnoreCase))
        {
            options.KnownNetworks.Clear();
        }

        if (!string.IsNullOrEmpty(knownNetworks))
        {
            foreach (var item in knownNetworks.Split(new char[] { ',', '|', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!string.IsNullOrEmpty(item))
                {
                    var net = item.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    if (net.Length == 2)
                    {
                        var prefix = System.Net.IPAddress.Parse(net[0]);
                        var prefixLength = int.Parse(net[1]);
                        options.KnownNetworks.Add(new IPNetwork(prefix, prefixLength));
                    }
                    else if (net.Length == 1)
                    {
                        var prefix = System.Net.IPAddress.Parse(net[0]);
                        var prefixLength = prefix.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? 128 : 32;
                        options.KnownNetworks.Add(new IPNetwork(prefix, prefixLength));
                    }
                }
            }
        }
    }

    private static void ProcessKnownProxies(IConfiguration configuration, ForwardedHeadersOptions options)
    {
        var knownProxies = configuration["KnownProxies"];
        var clearKnownProxies = configuration["ClearKnownProxies"];
        if (knownProxies != null || string.Equals(clearKnownProxies, "true", StringComparison.OrdinalIgnoreCase))
        {
            options.KnownProxies.Clear();
        }

        if (!string.IsNullOrEmpty(knownProxies))
        {
            foreach (var item in knownProxies.Split(new char[] { ',', '|', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!string.IsNullOrEmpty(item))
                {
                    options.KnownProxies.Add(System.Net.IPAddress.Parse(item));
                }
            }
        }
    }
}
