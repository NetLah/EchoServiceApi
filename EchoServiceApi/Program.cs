﻿using EchoServiceApi;
using EchoServiceApi.Verifiers;
using Microsoft.Extensions.Hosting.WindowsServices;
using NetLah.Diagnostics;
using NetLah.Extensions.HttpOverrides;
using NetLah.Extensions.Logging;

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

    builder.Services.AddControllers();

    builder.Services.AddHealthChecks();     // Registers health checks services

    builder.Services.AddSingleton<TokenCredentialFactory>();

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

    builder.Services.AddHttpOverrides(builder.Configuration);

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

    app.UseHealthChecks("/healthz");

    // app.UseHttpsRedirection()

    app.UseStatusCodePages();

    app.UseStaticFiles();

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
