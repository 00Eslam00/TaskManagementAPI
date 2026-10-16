# Build stage: has the full SDK, only used to compile and publish.
# The build context is the repository root, so global.json is available; the .NET code lives in backend/TaskManagement.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy only the project files first, so the restored packages stay cached
# until a .csproj changes, even when the source code changes.
COPY global.json ./
COPY backend/TaskManagement/src/TaskManagement.Domain/TaskManagement.Domain.csproj src/TaskManagement.Domain/
COPY backend/TaskManagement/src/TaskManagement.Application/TaskManagement.Application.csproj src/TaskManagement.Application/
COPY backend/TaskManagement/src/TaskManagement.Infrastructure/TaskManagement.Infrastructure.csproj src/TaskManagement.Infrastructure/
COPY backend/TaskManagement/src/TaskManagement.Api/TaskManagement.Api.csproj src/TaskManagement.Api/
RUN dotnet restore src/TaskManagement.Api/TaskManagement.Api.csproj

COPY backend/TaskManagement/src/ src/
RUN dotnet publish src/TaskManagement.Api/TaskManagement.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage: the small ASP.NET Core runtime image with only the published output.
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Run as the non-root user that the image provides.
USER $APP_UID

# ASP.NET Core listens on port 8080 inside the container by default.
EXPOSE 8080
ENTRYPOINT ["dotnet", "TaskManagement.Api.dll"]
