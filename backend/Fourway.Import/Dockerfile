FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Fourway.Core/Fourway.Core.csproj", "Fourway.Core/"]
RUN dotnet restore "Fourway.Core/Fourway.Core.csproj"
COPY Fourway.Core/ Fourway.Core/
WORKDIR /src/Fourway.Core
RUN dotnet build "Fourway.Core.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Fourway.Core.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgeos-dev libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Fourway.Core.dll"]