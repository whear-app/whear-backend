FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_ENVIRONMENT=Development
#ENV ASPNETCORE_URLS=http://+:$PORT

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/WhearApp.WebApi/WhearApp.WebApi.csproj", "src/WhearApp.WebApi/"]
COPY ["src/WhearApp.Application/WhearApp.Application.csproj", "src/WhearApp.Application/"]
COPY ["src/WhearApp.Infrastructure/WhearApp.Infrastructure.csproj", "src/WhearApp.Infrastructure/"]
COPY ["src/WhearApp.Infrastructure.Caching/WhearApp.Infrastructure.Caching.csproj", "src/WhearApp.Infrastructure.Caching/"]
COPY ["src/WhearApp.Infrastructure.Database/WhearApp.Infrastructure.Database.csproj", "src/WhearApp.Infrastructure.Database/"]
COPY ["src/WhearApp.Core/WhearApp.Core.csproj", "src/WhearApp.Core/"]
COPY ["src/BuildingBlocks/WhearApp.BuildingBlocks.SharedKernel/WhearApp.BuildingBlocks.SharedKernel.csproj", "src/BuildingBlocks/WhearApp.BuildingBlocks.SharedKernel/"]
COPY ["src/BuildingBlocks/WhearApp.BuildingBlocks.Utils/WhearApp.BuildingBlocks.Utils.csproj", "src/BuildingBlocks/WhearApp.BuildingBlocks.Utils/"]
RUN dotnet restore "src/WhearApp.WebApi/WhearApp.WebApi.csproj"
COPY . .
WORKDIR "/src/src/WhearApp.WebApi"
RUN dotnet build "./WhearApp.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WhearApp.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY docker-build-entrypoint.sh /
COPY --chmod=755 docker-build-entrypoint.sh /
USER $APP_UID
ENTRYPOINT ["/docker-build-entrypoint.sh"]
