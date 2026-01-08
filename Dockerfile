# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY FinanceSimplify/FinanceSimplify.csproj ./FinanceSimplify/
WORKDIR /src/FinanceSimplify
RUN dotnet restore

# Copy everything else and build
COPY FinanceSimplify/ .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Expose port (Railway will set PORT environment variable)
EXPOSE 8080

# Set environment to Production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Run the application
ENTRYPOINT ["dotnet", "FinanceSimplify.dll"]