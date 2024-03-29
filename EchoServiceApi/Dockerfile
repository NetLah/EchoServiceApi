#ARG ASPNET_IMAGE_TAG=6.0-alpine
#ARG ASPNET_IMAGE=mcr.microsoft.com/dotnet/aspnet:${ASPNET_IMAGE_TAG}
ARG ASPNET_IMAGE_TAG=lastest
ARG ASPNET_IMAGE=netlah/aspnet-webssh:${ASPNET_IMAGE_TAG}
FROM ${ASPNET_IMAGE} AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS restore
WORKDIR /src
COPY ["Directory.Build.props", "Directory.Build.targets", "./"]
COPY ["EchoServiceApi/EchoServiceApi.csproj", "EchoServiceApi/"]
RUN dotnet restore "EchoServiceApi/EchoServiceApi.csproj"

FROM restore AS build
COPY . .
WORKDIR "/src/EchoServiceApi"
ARG VERSION=1.0.0-dev
ENV MinVerVersionOverride=$VERSION
RUN dotnet build "EchoServiceApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EchoServiceApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["/opt/startup/init_container.sh"]
CMD ["dotnet", "EchoServiceApi.dll"]
