ARG ASPNET_IMAGE=mcr.microsoft.com/dotnet/aspnet:6.0-nanoserver-1809
FROM ${ASPNET_IMAGE}
WORKDIR /app
EXPOSE 80
COPY app .
ENTRYPOINT ["dotnet", "EchoServiceApi.dll"]
