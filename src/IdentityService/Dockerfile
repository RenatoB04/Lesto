# Fase de execução
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Fase de compilação
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Copia o csproj e restaura as dependências
COPY ["IdentityService.csproj", "./"]
RUN dotnet restore "./IdentityService.csproj"
# Copia o resto do código e compila
COPY . .
RUN dotnet build "IdentityService.csproj" -c Release -o /app/build

# Fase de publicação
FROM build AS publish
RUN dotnet publish "IdentityService.csproj" -c Release -o /app/publish

# Imagem final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IdentityService.dll"]