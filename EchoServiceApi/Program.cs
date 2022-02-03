using EchoServiceApi.Verifiers;
using NetLah.Diagnostics;
using NetLah.Extensions.Logging;
using Serilog.AspNetCore;
using Serilog.Events;

AppLog.InitLogger();
AppLog.Logger.LogInformation("Application configure...");
try
{
    var appInfo = ApplicationInfo.Initialize(null);
    var builder = WebApplication.CreateBuilder(args);

    builder.UseSerilog(logger => LogAppEvent(logger, "Application initializing...", appInfo));
    var logger = AppLog.Logger;

    // Add services to the container.

    builder.Services.AddControllers();

    builder.Services.AddHealthChecks();     // Registers health checks services

    builder.Services.AddSingleton<TokenCredentialFactory>();

    builder.Services.AddScoped<CosmosCacheVerifier>();
    builder.Services.AddScoped<CosmosVerifier>();
    builder.Services.AddScoped<PosgreSqlVerifier>();
    builder.Services.AddScoped<KeyVaultCertificateVerifier>();

    var app = builder.Build();

    logger.LogInformation("Environment: {environmentName}; DeveloperMode:{isDevelopment}", app.Environment.EnvironmentName, app.Environment.IsDevelopment());

#pragma warning disable S3923 // All branches in a conditional structure should not have exactly the same implementation
    if (app.Environment.IsDevelopment())
#pragma warning restore S3923 // All branches in a conditional structure should not have exactly the same implementation
    {
        // app.UseDeveloperExceptionPage()
    }
    else
    {
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        // app.UseHsts()
    }

    // app.UseSerilogRequestLoggingLevel(LogLevel.Information)
    Serilog.SerilogApplicationBuilderExtensions.UseSerilogRequestLogging(app, delegate (RequestLoggingOptions opt)
    {
        opt.GetLevel = ((HttpContext c, double d, Exception e) => (!(c.Response.StatusCode >= 500) && e == null) ? LogEventLevel.Information : LogEventLevel.Error);
    });

    app.UseHealthChecks("/healthz");

    // app.UseHttpsRedirection()

    app.UseStatusCodePages();

    app.UseStaticFiles();

    app.UseAuthorization();

    app.MapControllers();

    app.Lifetime.ApplicationStarted.Register(() => LogAppEvent(logger, "ApplicationStarted", appInfo));
    app.Lifetime.ApplicationStopping.Register(() => LogAppEvent(logger, "ApplicationStopping", appInfo));
    app.Lifetime.ApplicationStopped.Register(() => LogAppEvent(logger, "ApplicationStopped", appInfo));

    app.Run();

    static void LogAppEvent(ILogger logger, string appEvent, IAssemblyInfo appInfo)
        => logger.LogInformation("{ApplicationEvent} App:{title}; Version:{version} BuildTime:{buildTime}; Framework:{framework}",
        appEvent, appInfo.Title, appInfo.InformationalVersion, appInfo.BuildTimestampLocal, appInfo.FrameworkName);
}
catch (Exception ex)
{
    AppLog.Logger.LogCritical(ex, "Application terminated unexpectedly");
}
finally
{
    Serilog.Log.CloseAndFlush();
}
