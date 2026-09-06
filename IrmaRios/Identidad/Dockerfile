# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Identidad.sln .
COPY src/Identidad.Domain/Identidad.Domain.csproj src/Identidad.Domain/
COPY src/Identidad.Application/Identidad.Application.csproj src/Identidad.Application/
COPY src/Identidad.Infrastructure/Identidad.Infrastructure.csproj src/Identidad.Infrastructure/
COPY src/Identidad.Api/Identidad.Api.csproj src/Identidad.Api/
RUN dotnet restore src/Identidad.Api/Identidad.Api.csproj

COPY . .
RUN dotnet publish src/Identidad.Api/Identidad.Api.csproj -c Release -o /app/publish --no-restore

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Identidad.Api.dll"]
