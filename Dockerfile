FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
WORKDIR /app

COPY . .

RUN dotnet publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/.bin/libplanet-node ./

COPY ./.data ./.data

# RUN ./libplanet-node init .data --single-node --private-key d4b0e9b6b5fb5ec609f5db9076c4a61901aa3a91b0f4aafad32da3bf23164426

# COPY --from=build /app/settings/node-settings.docker.json ./.data/node-settings.json

EXPOSE 5001

ENTRYPOINT ["./libplanet-node", "start", ".data"]
