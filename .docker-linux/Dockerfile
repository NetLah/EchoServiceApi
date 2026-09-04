ARG ASPNET_IMAGE_TAG=8.0-alpine
ARG ASPNET_IMAGE_REPO=netlah/aspnet-webssh
ARG ASPNET_IMAGE=${ASPNET_IMAGE_REPO}:${ASPNET_IMAGE_TAG}
FROM ${ASPNET_IMAGE}
WORKDIR /app
EXPOSE 80
RUN apk update && apk add --no-cache ca-certificates
COPY app .
ENTRYPOINT ["/opt/startup/init_container.sh", "exec", "dotnet", "EchoServiceApi.dll"]
