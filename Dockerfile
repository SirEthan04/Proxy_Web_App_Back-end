FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY BodeGo.API/BodeGo.API.csproj BodeGo.API/
RUN dotnet restore BodeGo.API/BodeGo.API.csproj
COPY BodeGo.API/ BodeGo.API/
RUN dotnet publish BodeGo.API/BodeGo.API.csproj \
    --configuration Release --no-restore \
    --output /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_ENVIRONMENT=Production
COPY --from=build /out/ ./
COPY --chmod=755 docker-entrypoint.sh /app/docker-entrypoint.sh
USER $APP_UID
EXPOSE 10000
ENTRYPOINT ["/app/docker-entrypoint.sh"]
