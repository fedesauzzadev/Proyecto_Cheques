using Coelsa.Application;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;
using Coelsa.Domain.Validaciones;
using Coelsa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Coelsa.Api;

/// <summary>
/// Aplica migraciones y genera el seed de demostración (~30 instrumentos variados)
/// solo si la base está vacía, en desarrollo o con Coelsa:Seed=true (SPEC sección 9).
/// </summary>
public static class InicializacionBaseDeDatos
{
    public static async Task InicializarAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoelsaDbContext>();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        if (db.Database.GetMigrations().Any())
        {
            await db.Database.MigrateAsync();
        }
        else
        {
            // Fallback si no hay migraciones generadas (primer arranque sin dotnet-ef).
            await db.Database.EnsureCreatedAsync();
        }

        var sembrar = environment.IsDevelopment() || configuration.GetValue<bool>("Coelsa:Seed");
        if (!sembrar)
        {
            return;
        }

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var cuits = new[]
        {
            ValidadorCuit.Completar("2012345678"),
            ValidadorCuit.Completar("2787654321"),
            ValidadorCuit.Completar("3051122233"),
            ValidadorCuit.Completar("2033445566")
        };

        var bancos = new[] { "060", "011", "029", "034", "065" };

        // Cada tipo se siembra por separado: si una migración vacía una tabla
        // (como el cambio de formato del echeq), ese tipo se resiembra sin tocar el otro.
        if (!await db.ChequesFisicos.AnyAsync())
        {
            for (var i = 1; i <= 18; i++)
            {
                var cmc7 = $"{bancos[i % bancos.Length]}{i % 9:D4}{1425:D4}{i:D8}{12345678901:D11}";
                var emision = hoy.AddDays(-(i % 10));
                DateOnly? diferimiento = i % 2 == 0 ? hoy.AddDays(i % 30) : null;
                var cheque = ChequeFisico.Crear(
                    cmc7,
                    cuits[i % cuits.Length],
                    cuits[(i + 1) % cuits.Length],
                    monto: 150_000m * (i % 7 + 1),
                    i % 3 == 0 ? Moneda.Dolares : Moneda.Pesos,
                    emision,
                    diferimiento,
                    (diferimiento ?? emision).AddDays(30),
                    hoy);

                AplicarEstadoDemo(cheque, i);

                db.ChequesFisicos.Add(cheque);
            }
        }

        if (!await db.Echeqs.AnyAsync())
        {
            var generadorIdEcheq = scope.ServiceProvider.GetRequiredService<IGeneradorIdEcheq>();
            var idsEcheq = new HashSet<string>();

            for (var i = 1; i <= 12; i++)
            {
                string idEcheq;
                do
                {
                    idEcheq = generadorIdEcheq.Generar();
                }
                while (!idsEcheq.Add(idEcheq));

                // CMC7 propio del echeq (CP 2077 para no cruzarse con los físicos).
                var cmc7 = $"{bancos[(i + 2) % bancos.Length]}{i:D4}{2077:D4}{(i + 40):D8}{(98765432100 + i):D11}";

                var emision = hoy.AddDays(-(i % 8));
                DateOnly? diferimiento = i % 3 == 0 ? hoy.AddDays(i % 45) : null;
                var echeq = Echeq.Crear(
                    idEcheq,
                    cmc7,
                    cuits[i % cuits.Length],
                    cuits[(i + 2) % cuits.Length],
                    monto: 80_000m * (i % 5 + 1),
                    i % 4 == 0 ? Moneda.Dolares : Moneda.Pesos,
                    emision,
                    diferimiento,
                    (diferimiento ?? emision).AddDays(30),
                    hoy);

                AplicarEstadoDemo(echeq, i);

                db.Echeqs.Add(echeq);
            }
        }

        await db.SaveChangesAsync();
    }

    private static void AplicarEstadoDemo(IInstrumento instrumento, int i)
    {
        switch (i % 5)
        {
            case 1:
                Avanzar(instrumento, EstadoInstrumento.Depositado);
                break;
            case 2:
                Avanzar(instrumento, EstadoInstrumento.Depositado, EstadoInstrumento.Compensado);
                break;
            case 3 when i % 2 == 0:
                Avanzar(instrumento, EstadoInstrumento.Depositado);
                if (instrumento is ChequeFisico cheque)
                {
                    cheque.CambiarEstado(EstadoInstrumento.Rechazado, MotivoRechazo.FaltaDeFondos);
                }
                else if (instrumento is Echeq echeq)
                {
                    echeq.CambiarEstado(EstadoInstrumento.Rechazado, MotivoRechazo.DefectoFormal);
                }
                break;
        }
    }

    private static void Avanzar(IInstrumento instrumento, params EstadoInstrumento[] estados)
    {
        foreach (var estado in estados)
        {
            if (instrumento is ChequeFisico cheque)
            {
                cheque.CambiarEstado(estado, null);
            }
            else if (instrumento is Echeq echeq)
            {
                echeq.CambiarEstado(estado, null);
            }
        }
    }
}
