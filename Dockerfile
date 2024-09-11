FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
WORKDIR /app

COPY . .

RUN dotnet publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/.bin/libplanet-node ./

RUN mkdir /data

ENTRYPOINT ["./libplanet-node", "--private-key", "b05c8b2bc981219c2afc32725f1dd7bdfce356ac7382699cb74647ab20895e32", "--end-point", "127.0.0.1:5353", "--explorer-end-point", "127.0.0.1:5001", "--store-path", "/data/.store", "--log-path", "/data/.log/node.log", "--no-repl"]
