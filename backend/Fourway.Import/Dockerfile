FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Fourway.Import/Fourway.Import.csproj", "Fourway.Import/"]
COPY ["Fourway.Core/Fourway.Core.csproj", "Fourway.Core/"]
RUN dotnet restore "Fourway.Import/Fourway.Import.csproj"

COPY . .
RUN dotnet publish "Fourway.Import/Fourway.Import.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:ErrorOnDuplicatePublishOutputFiles=false \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgeos-c1t64 libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID

WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Fourway.Import.dll"]
