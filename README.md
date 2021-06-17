# ASPNET.Core 5 Echo Service WebApi

ASPNET.Core 5 WebApi provide Echo Service Api for testing deployment and integration environment

## Build Status

[![Build Status](https://img.shields.io/endpoint.svg?url=https%3A%2F%2Factions-badge.atrox.dev%2FNetLah%2FEchoServiceApi%2Fbadge%3Fref%3Dmain&style=flat)](https://actions-badge.atrox.dev/NetLah/EchoServiceApi/goto?ref=main)

## Getting started

- Checkout source code from Github

### Run ASP.NETCore Echo Service API from command line using dotnet

Run the ASP.NET Core WebApi application usign command line

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

Use browser and hit the URL `https://localhost:5001/e/hello-world`

![browser-hello-world](https://raw.githubusercontent.com/NetLah/EchoServiceApi/main/docs/browser-hello-world.png)
