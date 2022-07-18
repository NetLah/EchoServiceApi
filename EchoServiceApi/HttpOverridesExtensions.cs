using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace EchoServiceApi;

public static class HttpOverridesExtensions
{
    private static bool _isForwardedHeadersEnabled;
    private static bool _isHttpLoggingEnabled;

    public static IServiceCollection AddHttpOverrides(this IServiceCollection services, IConfiguration configuration, string httpOverridesSectionName = "HttpOverrides", string httpLoggingSectionName = "HttpLogging")
    {
        _isForwardedHeadersEnabled = configuration["ASPNETCORE_FORWARDEDHEADERS_ENABLED"].IsTrue();
        if (!_isForwardedHeadersEnabled)
        {
            var httpOverridesConfigurationSection = string.IsNullOrEmpty(httpOverridesSectionName) ? configuration : configuration.GetSection(httpOverridesSectionName);
            services.Configure<ForwardedHeadersOptions>(httpOverridesConfigurationSection);

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                if (httpOverridesConfigurationSection["ClearForwardLimit"].IsTrue())
                {
                    options.ForwardLimit = null;
                }

                ProcessKnownProxies(httpOverridesConfigurationSection, options);

                ProcessKnownNetworks(httpOverridesConfigurationSection, options);
            });
        }

        var httpLoggingEnabledKey = string.IsNullOrEmpty(httpLoggingSectionName) ? "HttpLoggingEnabled" : $"{httpLoggingSectionName}:Enabled";
        _isHttpLoggingEnabled = configuration[httpLoggingEnabledKey].IsTrue();
        if (_isHttpLoggingEnabled)
        {
            var httpLoggingConfigurationSection = string.IsNullOrEmpty(httpLoggingSectionName) ? configuration : configuration.GetSection(httpLoggingSectionName);
            services.Configure<HttpLoggingOptions>(httpLoggingConfigurationSection);
            var isClearRequestHeaders = httpLoggingConfigurationSection["ClearRequestHeaders"].IsTrue();
            var isClearResponseHeaders = httpLoggingConfigurationSection["ClearResponseHeaders"].IsTrue();
            var httpLoggingConfig = httpLoggingConfigurationSection.Get<HttpLoggingConfig>();
            var requestHeaders = httpLoggingConfig?.RequestHeaders.SplitSet() ?? new HashSet<string>();
            var responseHeaders = httpLoggingConfig?.ResponseHeaders.SplitSet() ?? new HashSet<string>();
            var mediaTypeOptions = httpLoggingConfig?.MediaTypeOptions ?? Enumerable.Empty<string>();

            if (isClearRequestHeaders || isClearResponseHeaders || requestHeaders.Any() || responseHeaders.Any() || mediaTypeOptions.Any())
            {
                services.AddHttpLogging(options =>
                {
                    if (isClearRequestHeaders) { options.RequestHeaders.Clear(); }
                    if (isClearResponseHeaders) { options.ResponseHeaders.Clear(); }
                    options.RequestHeaders.UnionWith(requestHeaders);
                    options.ResponseHeaders.UnionWith(responseHeaders);
                    foreach (var mediaType in mediaTypeOptions)
                    {
                        if (!string.IsNullOrEmpty(mediaType))
                        {
                            options.MediaTypeOptions.AddText(mediaType);
                        }
                    }
                });
            }
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
            if (_isForwardedHeadersEnabled)
            {
                logger.LogInformation("ForwardedHeaders: {forwardedHeaders}", forwardedHeaders);
            }
            else
            {
                app.UseForwardedHeaders();
                logger.LogInformation("Use ForwardedHeaders: {forwardedHeaders}", forwardedHeaders);
            }
        }

        if (fho.AllowedHosts.Count > 0)
        {
            var allowedHosts = string.Join(",", fho.AllowedHosts);
            logger.LogInformation("AllowedHosts: {allowedHosts}", allowedHosts);
        }

        if (_isHttpLoggingEnabled)
        {
            var httpLogging = sp.GetRequiredService<IOptions<HttpLoggingOptions>>()?.Value;
            logger.LogInformation("Use HttpLogging LoggingFields:{loggingFields} MediaTypeOptions:{mediaTypeOptions} RequestBodyLogLimit:{requestBodyLogLimit} ResponseBodyLogLimit:{responseBodyLogLimit} RequestHeaders:{requestHeaders} ResponseHeaders:{responseHeaders}",
                httpLogging?.LoggingFields, httpLogging?.MediaTypeOptions, httpLogging?.RequestBodyLogLimit, httpLogging?.ResponseBodyLogLimit,
                string.Join(',', httpLogging?.RequestHeaders ?? Enumerable.Empty<string>()),
                string.Join(',', httpLogging?.ResponseHeaders ?? Enumerable.Empty<string>()));
            app.UseHttpLogging();
        }

        return app;
    }

    private static void ProcessKnownNetworks(IConfiguration configuration, ForwardedHeadersOptions options)
    {
        var knownNetworks = configuration["KnownNetworks"];
        if (knownNetworks != null || configuration["ClearKnownNetworks"].IsTrue())
        {
            options.KnownNetworks.Clear();
        }

        foreach (var item in knownNetworks.SplitSet())
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

    private static void ProcessKnownProxies(IConfiguration configuration, ForwardedHeadersOptions options)
    {
        var knownProxies = configuration["KnownProxies"];
        if (knownProxies != null || configuration["ClearKnownProxies"].IsTrue())
        {
            options.KnownProxies.Clear();
        }

        foreach (var item in knownProxies.SplitSet())
        {
            options.KnownProxies.Add(System.Net.IPAddress.Parse(item));
        }
    }

    private static bool IsTrue(this string? configurationValue) => string.Equals(configurationValue, "true", StringComparison.OrdinalIgnoreCase);
    private static bool IsFalse(this string? configurationValue) => string.Equals(configurationValue, "false", StringComparison.OrdinalIgnoreCase);
    private static HashSet<string> SplitSet(this string? configurationValue)
        => configurationValue
            ?.Split(new char[] { ',', '|', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(i => !string.IsNullOrEmpty(i))
            .ToHashSet() ?? new HashSet<string>();

    private class HttpLoggingConfig
    {
        public string? RequestHeaders { get; set; }
        public string? ResponseHeaders { get; set; }
        public List<string>? MediaTypeOptions { get; set; }
    }
}
