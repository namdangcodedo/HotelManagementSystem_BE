FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AppBackend.ApiCore/AppBackend.ApiCore.csproj", "AppBackend.ApiCore/"]
COPY ["AppBackend.BusinessObjects/AppBackend.BusinessObjects.csproj", "AppBackend.BusinessObjects/"]
COPY ["AppBackend.Services/AppBackend.Services.csproj", "AppBackend.Services/"]
COPY ["AppBackend.Repositories/AppBackend.Repositories.csproj", "AppBackend.Repositories/"]
RUN dotnet restore "AppBackend.ApiCore/AppBackend.ApiCore.csproj"
COPY . .
WORKDIR "/src/AppBackend.ApiCore"
RUN dotnet build "./AppBackend.ApiCore.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AppBackend.ApiCore.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AppBackend.ApiCore.dll"]
