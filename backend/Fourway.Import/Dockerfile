FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Fourway.Core/Fourway.Core.csproj", "Fourway.Core/"]
COPY ["Fourway.Import/Fourway.Import.csproj", "Fourway.Import/"]
RUN dotnet restore "Fourway.Import/Fourway.Import.csproj"
COPY . .
WORKDIR "/src/Fourway.Import"
RUN dotnet build "Fourway.Import.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Fourway.Import.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Fourway.Import.dll"]
