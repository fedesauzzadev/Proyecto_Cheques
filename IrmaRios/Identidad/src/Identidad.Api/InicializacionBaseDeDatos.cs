using IrmaRios.Identidad.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IrmaRios.Identidad.Api;

/// <summary>
/// Inicialización del esquema. Slice v0: EnsureCreated (el modelo aún se mueve);
/// cuando el esquema se estabilice se reemplaza por migraciones EF como en Coelsa.
/// </summary>
public static class InicializacionBaseDeDatos
{
    public static async Task InicializarAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<IdentidadDbContext>();
        await db.Database.EnsureCreatedAsync();
    }
}
