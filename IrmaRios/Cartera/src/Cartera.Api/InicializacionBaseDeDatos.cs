using IrmaRios.Cartera.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IrmaRios.Cartera.Api;

/// <summary>
/// Inicialización del esquema. Slice v0: EnsureCreated; migraciones EF cuando
/// el modelo se estabilice (mismo estándar que Coelsa).
/// </summary>
public static class InicializacionBaseDeDatos
{
    public static async Task InicializarAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<CarteraDbContext>();
        await db.Database.EnsureCreatedAsync();
    }
}
