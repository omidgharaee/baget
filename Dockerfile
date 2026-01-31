# Use .NET 6 ASP.NET runtime image as base
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

# Use .NET 6 SDK image for build
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY /src .
# Restore and build the project
RUN dotnet publish BaGet -c Release -o /app

# Final image
FROM base AS final
LABEL org.opencontainers.image.source="https://github.com/loic-sharma/BaGet"
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "BaGet.dll"]