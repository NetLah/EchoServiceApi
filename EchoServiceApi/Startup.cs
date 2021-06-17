using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetLah.Diagnostics;
using NetLah.Extensions.Logging;

namespace EchoServiceApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            AppLog.Logger.LogInformation("Startup ...");
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            AppLog.Logger.LogInformation("ConfigureServices ...");
            services.AddControllers();
            services.AddHealthChecks();     // Registers health checks services
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory, IHostApplicationLifetime applicationLifetime)
        {
            var logger = AppLog.Logger;
            logger.LogInformation("WebApplication configure ...");
            logger.LogInformation("Environment: {environmentName}; DeveloperMode:{isDevelopment}", env.EnvironmentName, env.IsDevelopment());

#pragma warning disable S3923 // All branches in a conditional structure should not have exactly the same implementation
            if (env.IsDevelopment())
#pragma warning restore S3923 // All branches in a conditional structure should not have exactly the same implementation
            {
                // app.UseDeveloperExceptionPage()
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                // app.UseHsts()
            }

            app.UseSerilogRequestLoggingLevel(LogLevel.Information);

            app.UseHealthChecks("/healthz");
            //app.UseHttpsRedirection()
            app.UseStatusCodePages();
            //app.UseStatusCodePagesWithReExecute("/ErrorPages/{0}.html")
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            applicationLifetime.ApplicationStarted.Register(() =>
            {
                var appInfo = ApplicationInfo.Instance ?? ApplicationInfo.Initialize(null);
                logger.LogInformation("App:{title}; Version:{version} BuildTime:{buildTime}; Framework:{framework}",
                    appInfo.Title, appInfo.InformationalVersion, appInfo.BuildTimestampLocal, appInfo.FrameworkName);
            });
        }
    }
}
