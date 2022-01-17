ARG ASPNET_IMAGE=mcr.microsoft.com/dotnet/aspnet:6.0
FROM ${ASPNET_IMAGE} AS base
WORKDIR /app

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
ENTRYPOINT ["dotnet", "EchoServiceApi.dll"]