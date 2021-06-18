# ASPNET.Core 5 Echo Service WebApi

ASPNET.Core 5 WebApi provide Echo Service Api for testing deployment and integration environment. This is useful on detect deployment (setup environment) and development issue OAuth and OIDC with deployment environment.

## Build Status

[![Build Status](https://img.shields.io/endpoint.svg?url=https%3A%2F%2Factions-badge.atrox.dev%2FNetLah%2FEchoServiceApi%2Fbadge%3Fref%3Dmain&style=flat)](https://actions-badge.atrox.dev/NetLah/EchoServiceApi/goto?ref=main)

## Getting started

### Use docker run

- Check docker repository on Docker Hub: https://hub.docker.com/r/netlah/echo-service-api

- Support Linux and Windows Server 2019 with nanoserver 1809 base.

- This docker respository not support tag `latest` and multi arch yet, there is 2 tags `linux` and `nanoserver-1809`

```
docker pull netlah/echo-service-api:linux
docker run -d -p 5000:80 --name echoapi netlah/echo-service-api:linux
docker logs- f echoapi
docker rm -f echoapi
```

### Run ASP.NETCore Echo Service API from command line using dotnet

- Checkout source code from Github

- Run the ASP.NET Core WebApi application using command line

```
dotnet run -p EchoServiceApi
```

Output

```
C:\Work\NetLah\EchoServiceApi>dotnet run -p EchoServiceApi
Building...
[16:44:23 INF] Application configure...
[16:44:23 DBG] Logger has been initialized.
[16:44:23 INF] Application initializing...
[16:44:23 INF] Startup ...
[16:44:23 INF] ConfigureServices ...
[16:44:23 INF] WebApplication configure ...
[16:44:23 INF] Environment: Development; DeveloperMode:True
[16:44:23 INF] Now listening on: https://localhost:5001
[16:44:23 INF] Now listening on: http://localhost:5000
[16:44:23 INF] Application started. Press Ctrl+C to shut down.
[16:44:23 INF] Hosting environment: Development
[16:44:23 INF] Content root path: C:\Work\NetLah\EchoServiceApi\EchoServiceApi
[16:44:25 INF] HTTP GET /e/hello-world responded 200 in 77.2946 ms
```

![dotnet-run-output](https://raw.githubusercontent.com/NetLah/EchoServiceApi/main/docs/dotnet-run-output.png)

### Hit the echo service api

- Using any REST tool or browser to hit the Echo service API on URL

```
https://<domain:port>/e/<what-ever-path-and-method>
```

- Use browser and hit the URL `https://localhost:5001/e/hello-world`

![browser-hello-world](https://raw.githubusercontent.com/NetLah/EchoServiceApi/main/docs/browser-hello-world.png)

### Test with hosting and Reverse Proxy

- This Echo Service Api support both IIS In process, IIS out process, IIS ARR Urlrewite or NGINX.

- For forwarded headers issue when behind Reverse Proxy, can check this article for further understanding: https://devblogs.microsoft.com/aspnet/forwarded-headers-middleware-updates-in-net-core-3-0-preview-6/

- To add support Forwarded Headers when use with Reverse Proxy, make sure add this environment setting (environment setings, it is not support appsettings.json).

```
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
```

- For IIS ARR UrlRewrite, make sure add Server variable `HTTP_X_Forwarded_Proto` and provide proper value, the below is the sample for reference (do not use for production):

web.config

```
<configuration>
    <system.webServer>
        <rewrite>
            <rules>
                <rule name="ARR_Proxy" patternSyntax="Wildcard" stopProcessing="true">
                    <match url="*" />
                    <action type="Rewrite" url="http://web-farm-name/{R:1}" />
                    <serverVariables>
                        <set name="HTTP_X_Forwarded_Proto" value="https" />
                    </serverVariables>
                </rule>
            </rules>
        </rewrite>
    </system.webServer>
</configuration>
```
