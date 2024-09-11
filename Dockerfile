# syntax=docker/dockerfile:1

# Comments are provided throughout this file to help you get started.
# If you need more help, visit the Dockerfile reference guide at
# https://docs.docker.com/go/dockerfile-reference/

# Want to help us make this template better? Share your feedback here: https://forms.gle/ybq9Krt8jtBL3iCk7

################################################################################

# Learn about building .NET container images:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md

# Create a stage for building the application.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
WORKDIR /app

COPY . .

RUN dotnet build

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy everything needed to run the app from the "build" stage.
COPY --from=build /app/src/node/LibplanetConsole.Nodes.Executable/bin/Debug/net8.0 ./

# Create a non-privileged user that the app will run under.
# See https://docs.docker.com/go/dockerfile-user-best-practices/
ARG UID=10001
RUN adduser \
    --disabled-password \
    --gecos "" \
    --home "/nonexistent" \
    --shell "/sbin/nologin" \
    --no-create-home \
    --uid "${UID}" \
    appuser
RUN mkdir /data
RUN chown -R appuser:appuser /data
USER appuser

ENTRYPOINT ["dotnet", "./libplanet-node.dll", "--private-key", "b05c8b2bc981219c2afc32725f1dd7bdfce356ac7382699cb74647ab20895e32", "--end-point", "127.0.0.1:5353", "--explorer-end-point", "127.0.0.1:5001", "--store-path", "/data/.store", "--log-path", "/data/.log/node.log"]
