# Étape 1: Build de l'application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copier les fichiers du projet
COPY ["CharacterManager/CharacterManager.csproj", "CharacterManager/"]
RUN dotnet restore "CharacterManager/CharacterManager.csproj"

# Copier tout le code source
COPY . .

# Build de l'application
WORKDIR "/src/CharacterManager"
RUN dotnet build "CharacterManager.csproj" -c Release -o /app/build

# Étape 2: Publish
FROM build AS publish
RUN dotnet publish "CharacterManager.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Étape 3: Image finale
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Installer les dépendances SQLite
RUN apt-get update && apt-get install -y sqlite3 libsqlite3-dev && rm -rf /var/lib/apt/lists/*

# Copier l'application publiée
COPY --from=publish /app/publish .

# Créer le dossier pour la base de données
RUN mkdir -p /app/data

# Exposer le port
EXPOSE 8080

# Variables d'environnement
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Point d'entrée
ENTRYPOINT ["dotnet", "CharacterManager.dll"]
