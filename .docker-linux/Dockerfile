ARG ASPNET_IMAGE=mcr.microsoft.com/dotnet/aspnet:6.0-alpine
FROM ${ASPNET_IMAGE}
WORKDIR /app
EXPOSE 80
RUN apk update && apk add --no-cache ca-certificates tzdata
ENV TZ=UTC
COPY app .
ENTRYPOINT ["dotnet", "EchoServiceApi.dll"]
