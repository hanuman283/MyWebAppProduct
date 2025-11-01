# Base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj and restore (do restore from /src so we don't create nested project folders)
COPY ["MyWebAppProduct/MyWebAppProduct.csproj", "MyWebAppProduct/"]
RUN dotnet restore "MyWebAppProduct/MyWebAppProduct.csproj"

# Copy the rest of the source code into /src (keeps repository layout consistent)
COPY . .

WORKDIR /src/MyWebAppProduct
# Build the project
RUN dotnet build "MyWebAppProduct.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/MyWebAppProduct
RUN dotnet publish "MyWebAppProduct.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyWebAppProduct.dll"]
