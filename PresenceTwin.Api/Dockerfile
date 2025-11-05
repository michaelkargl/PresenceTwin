FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["PresenceTwin.Api/PresenceTwin.Api.fsproj", "PresenceTwin.Api/"]
RUN dotnet restore "PresenceTwin.Api/PresenceTwin.Api.fsproj"
COPY . .
WORKDIR "/src/PresenceTwin.Api"
RUN dotnet build "./PresenceTwin.Api.fsproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PresenceTwin.Api.fsproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PresenceTwin.Api.dll"]
