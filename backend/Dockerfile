# Use the official .NET 10 SDK image to build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["StAmanuelBloodDonation.Api.csproj", "./"]
RUN dotnet restore "StAmanuelBloodDonation.Api.csproj"

# Copy everything else and build
COPY . .
RUN dotnet publish "StAmanuelBloodDonation.Api.csproj" -c Release -o /app/publish --no-restore

# Use the lightweight runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install Noto Sans Ethiopic font for Amharic PDF rendering
RUN apt-get update && \
    apt-get install -y --no-install-recommends fonts-noto-core fonts-noto-extra wget fontconfig && \
    fc-cache -f -v && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Create a non-root user for security (Debian-style commands)
RUN groupadd --system appgroup && useradd --system --gid appgroup appuser
USER appuser

COPY --from=build /app/publish .

# Render provides PORT dynamically; ASP.NET Core reads ASPNETCORE_URLS or PORT in Program.cs
EXPOSE 10000
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "StAmanuelBloodDonation.Api.dll"]
