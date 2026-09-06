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
                echeq.Aceptar();

                AplicarEstadoDemo(echeq, i);

                db.Echeqs.Add(echeq);
            }

            // Vitrina Fase A: estados y cadenas que el flujo normal no genera solo.
            // CMC7 con CP 2078 para no cruzarse con el resto del seed.
            string Cmc7Extra(int n) =>
                $"034{n:D4}{2078:D4}{(n + 60):D8}{(11111111100 + n):D11}";

            string IdUnicoExtra()
            {
                string id;
                do
                {
                    id = generadorIdEcheq.Generar();
                }
                while (!idsEcheq.Add(id));
                return id;
            }

            // 1. Pendiente de aceptación.
            db.Echeqs.Add(Echeq.Crear(
                IdUnicoExtra(), Cmc7Extra(1), cuits[0], cuits[1], 100_000m, Moneda.Pesos,
                hoy, null, hoy.AddDays(30), hoy));

            // 2. Repudiado por el beneficiario.
            var repudiado = Echeq.Crear(
                IdUnicoExtra(), Cmc7Extra(2), cuits[0], cuits[1], 200_000m, Moneda.Pesos,
                hoy, null, hoy.AddDays(30), hoy);
            repudiado.Repudiar();
            db.Echeqs.Add(repudiado);

            // 3. En custodia.
            var custodia = Echeq.Crear(
                IdUnicoExtra(), Cmc7Extra(3), cuits[0], cuits[1], 300_000m, Moneda.Pesos,
                hoy, null, hoy.AddDays(30), hoy);
            custodia.Aceptar();
            custodia.PonerEnCustodia();
            db.Echeqs.Add(custodia);

            // 4. Con cadena de endosos (2 vigentes) y devolución solicitada.
            var cadena = Echeq.Crear(
                IdUnicoExtra(), Cmc7Extra(4), cuits[0], cuits[1], 400_000m, Moneda.Dolares,
                hoy, null, hoy.AddDays(30), hoy);
            cadena.Aceptar();
            db.Echeqs.Add(cadena);
            var endoso1 = Endoso.Proponer(cadena.Id, 1, cuits[1], cuits[2]);
            endoso1.Admitir();
            var endoso2 = Endoso.Proponer(cadena.Id, 2, cuits[2], cuits[3]);
            endoso2.Admitir();
            cadena.CambiarTenencia(cuits[3]);
            cadena.FijarCantidadEndosos(2);
            db.Endosos.AddRange(endoso1, endoso2);
            db.Devoluciones.Add(Devolucion.Solicitar(cadena.Id, 1, cuits[0], "Devolución de demostración"));
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
