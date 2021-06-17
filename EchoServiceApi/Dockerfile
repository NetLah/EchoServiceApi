FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS restore
WORKDIR /src
COPY ["Directory.Build.props", "Directory.Build.targets", "./"]
COPY ["EchoServiceApi/EchoServiceApi.csproj", "EchoServiceApi/"]
RUN dotnet restore "EchoServiceApi/EchoServiceApi.csproj"

FROM restore AS build
COPY . .
WORKDIR "/src/EchoServiceApi"
RUN dotnet build "EchoServiceApi.csproj" -c Release -o /app/build

FROM build AS publish
ARG VERSION=1.0.0-dev
RUN export MinVerVersionOverride=$VERSION && \
    dotnet publish "EchoServiceApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EchoServiceApi.dll"]