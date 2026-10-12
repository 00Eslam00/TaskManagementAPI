FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY global.json ./
COPY src/TaskManagement.Domain/TaskManagement.Domain.csproj src/TaskManagement.Domain/
COPY src/TaskManagement.Application/TaskManagement.Application.csproj src/TaskManagement.Application/
COPY src/TaskManagement.Infrastructure/TaskManagement.Infrastructure.csproj src/TaskManagement.Infrastructure/
COPY src/TaskManagement.Api/TaskManagement.Api.csproj src/TaskManagement.Api/
RUN dotnet restore src/TaskManagement.Api/TaskManagement.Api.csproj

COPY src/ src/
RUN dotnet publish src/TaskManagement.Api/TaskManagement.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Run as the non-root user that the image provides.
USER $APP_UID

# ASP.NET Core listens on port 8080 inside the container by default.
EXPOSE 8080
ENTRYPOINT ["dotnet", "TaskManagement.Api.dll"]
