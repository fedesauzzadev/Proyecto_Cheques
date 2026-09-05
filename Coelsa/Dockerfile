# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Coelsa.sln .
COPY src/Coelsa.Domain/Coelsa.Domain.csproj src/Coelsa.Domain/
COPY src/Coelsa.Application/Coelsa.Application.csproj src/Coelsa.Application/
COPY src/Coelsa.Infrastructure/Coelsa.Infrastructure.csproj src/Coelsa.Infrastructure/
COPY src/Coelsa.Api/Coelsa.Api.csproj src/Coelsa.Api/
RUN dotnet restore src/Coelsa.Api/Coelsa.Api.csproj

COPY . .
RUN dotnet publish src/Coelsa.Api/Coelsa.Api.csproj -c Release -o /app/publish --no-restore

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Coelsa.Api.dll"]
