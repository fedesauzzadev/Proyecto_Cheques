using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Coelsa.Infrastructure.Persistence;

/// <summary>
/// Factory para dotnet-ef: permite generar migraciones sin ejecutar el Program de la API.
/// La cadena de conexión solo se usa para construir el modelo en tiempo de diseño.
/// </summary>
public class CoelsaDbContextFactory : IDesignTimeDbContextFactory<CoelsaDbContext>
{
    public CoelsaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CoelsaDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=coelsa;Username=coelsa;Password=coelsa123");
        return new CoelsaDbContext(optionsBuilder.Options);
    }
}
