ARG ASPNET_IMAGE_TAG=6.0-nanoserver-1809
ARG ASPNET_IMAGE=mcr.microsoft.com/dotnet/aspnet:${ASPNET_IMAGE_TAG}
FROM ${ASPNET_IMAGE}
WORKDIR /app
EXPOSE 80
COPY app .
ENTRYPOINT ["dotnet", "EchoServiceApi.dll"]
