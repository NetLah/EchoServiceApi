using EchoServiceApi;
using EchoServiceApi.Controllers;
using EchoServiceApi.Verifiers;
using Microsoft.Extensions.Hosting.WindowsServices;
using NetLah.Diagnostics;
using NetLah.Extensions.HttpOverrides;
using NetLah.Extensions.Logging;
using System.Text;

AppLog.InitLogger();
AppLog.Logger.LogInformation("Application configure...");
try
{
    var appInfo = ApplicationInfo.Initialize(null);

    // https://github.com/dotnet/runtime/issues/69212
    // https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/windows-service?view=aspnetcore-6.0&tabs=visual-studio
    var webApplicationOptions = new WebApplicationOptions
    {
        Args = args,
        ContentRootPath = WindowsServiceHelpers.IsWindowsService()
            ? AppContext.BaseDirectory
            : default
    };
    var builder = WebApplication.CreateBuilder(webApplicationOptions);

    builder.Host.UseWindowsService();
    builder.Host.UseSystemd();

    builder.Services.AddSingleton<IAssemblyInfo>(appInfo);

    builder.UseSerilog(logger => LogAppEvent(logger, "Application initializing...", appInfo));
    var logger = AppLog.Logger;
    void LogAssembly(AssemblyInfo assembly)
    {
        logger.LogInformation("{title}; Version:{version} Framework:{framework}",
        assembly.Title, assembly.InformationalVersion, assembly.FrameworkName);
    }

    LogAssembly(new AssemblyInfo(typeof(Serilog.SerilogApplicationBuilderExtensions).Assembly));

    // Add services to the container.

    builder.Services.AddApplicationInsightsTelemetry();

    builder.AddHttpOverrides();

    builder.Services.AddControllers();

    //builder.Services.AddHealthChecks();     // Registers health checks services

    var appOptions = builder.Configuration.Get<AppOptions>()!;

    builder.Services.AddSingleton<TokenCredentialFactory>();

    builder.Services.AddSingleton<AppOptions>(appOptions);
    builder.Services.AddScoped<CosmosCacheVerifier>();
    builder.Services.AddScoped<CosmosVerifier>();
    builder.Services.AddScoped<PosgreSqlVerifier>();
    builder.Services.AddScoped<KeyVaultCertificateVerifier>();
    builder.Services.AddScoped<KeyVaultKeyVerifier>();
    builder.Services.AddScoped<BlobUriVerifier>();
    builder.Services.AddScoped<DirVerifier>();
    builder.Services.AddScoped<ServiceBusVerifier>();
    builder.Services.AddScoped<CertificateVerifier>();
    builder.Services.AddScoped<DnsHostEntryVerifier>();
    builder.Services.AddHttpClient<HttpVerifier>();

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<HttpContextInfo>();

    builder.Services.AddScoped<DiagnosticInfo>();

    var app = builder.Build();

    logger.LogInformation("Environment: {environmentName}; DeveloperMode:{isDevelopment}", app.Environment.EnvironmentName, app.Environment.IsDevelopment());

    app.UseHttpOverrides();
    if (app.Environment.IsDevelopment())
    {
        // app.UseDeveloperExceptionPage()
    }
    else
    {
        // author: this in diagnostics tool for both HTTP and HTTPS, so DO NOT enable `app.UseHsts()` by mistake
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        // app.UseHsts()
    }

    app.UseSerilogRequestLoggingLevel(LogLevel.Information);

    ///Serilog.SerilogApplicationBuilderExtensions.UseSerilogRequestLogging(app, delegate (RequestLoggingOptions opt)
    ///{
    ///    opt.GetLevel = (HttpContext c, double d, Exception? e) => (c.Response.StatusCode < 500 && e == null) ? LogEventLevel.Information : LogEventLevel.Error;
    ///});

    // app.UseHealthChecks("/healthz");

    // app.UseHttpsRedirection()

    app.UseStatusCodePages();

    app.UseStaticFiles();

    app.UseRouting();

    var debugRoutes = "debugRoutes";

    if (!string.IsNullOrWhiteSpace(debugRoutes))
    {
        var debugRoutesPath = $"/diag/{debugRoutes}";
        logger.LogDebug("Debug routes: {debugRoutesPath}", debugRoutes);

        app.MapGet(debugRoutesPath, (IEnumerable<EndpointDataSource> endpointSources) =>
        {
            var sb = new StringBuilder();
            var endpoints = endpointSources.SelectMany(es => es.Endpoints);
            foreach (var endpoint in endpoints)
            {
                var routeNameMetadata = endpoint.Metadata.OfType<RouteNameMetadata>().FirstOrDefault();
                var httpMethodsMetadata = endpoint.Metadata.OfType<HttpMethodMetadata>().FirstOrDefault();

                sb.Append($"[{routeNameMetadata?.RouteName}] {(httpMethodsMetadata == null ? null : string.Join(",", httpMethodsMetadata.HttpMethods))}");
                if (endpoint is RouteEndpoint routeEndpoint)
                {
                    sb.AppendLine($" {routeEndpoint.RoutePattern.RawText} {routeEndpoint.DisplayName}");
                }
            }
            return sb.ToString();
        });
    }

    var defaultPath = appOptions.DefaultPath;
    if (defaultPath != null)
    {
        logger.LogDebug("Map Default/Home to {route}", defaultPath);
        app.MapControllerRoute(name: string.Empty,
            pattern: defaultPath,
            defaults: new { controller = "Default", action = nameof(DefaultController.Home) })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));
    }

    var namePath = appOptions.NamePath;
    if (namePath != null && !string.Equals(namePath, defaultPath, StringComparison.InvariantCultureIgnoreCase))
    {
        logger.LogDebug("Map Default/Name to {route}", namePath);
        app.MapControllerRoute(name: string.Empty,
            pattern: namePath,
            defaults: new { controller = "Default", action = nameof(DefaultController.Name) })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));
    }

    app.UseAuthorization();

    app.MapControllers();

    app.Lifetime.ApplicationStarted.Register(() => LogAppEvent(logger, "ApplicationStarted", appInfo));
    app.Lifetime.ApplicationStopping.Register(() => LogAppEvent(logger, "ApplicationStopping", appInfo));
    app.Lifetime.ApplicationStopped.Register(() => LogAppEvent(logger, "ApplicationStopped", appInfo));
    app.Logger.LogInformation("Finished configuring application");
    app.Run();

    static void LogAppEvent(ILogger logger, string appEvent, IAssemblyInfo appInfo)
    {
        logger.LogInformation("{ApplicationEvent} App:{title}; Version:{version} BuildTime:{buildTime}; Framework:{framework}",
            appEvent, appInfo.Title, appInfo.InformationalVersion, appInfo.BuildTimestampLocal, appInfo.FrameworkName);
    }
}
catch (Exception ex)
{
    AppLog.Logger.LogCritical(ex, "Application terminated unexpectedly");
}
finally
{
    Serilog.Log.CloseAndFlush();
}
