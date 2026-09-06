# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Cartera.sln .
COPY src/Cartera.Domain/Cartera.Domain.csproj src/Cartera.Domain/
COPY src/Cartera.Application/Cartera.Application.csproj src/Cartera.Application/
COPY src/Cartera.Infrastructure/Cartera.Infrastructure.csproj src/Cartera.Infrastructure/
COPY src/Cartera.Api/Cartera.Api.csproj src/Cartera.Api/
RUN dotnet restore src/Cartera.Api/Cartera.Api.csproj

COPY . .
RUN dotnet publish src/Cartera.Api/Cartera.Api.csproj -c Release -o /app/publish --no-restore

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Cartera.Api.dll"]
