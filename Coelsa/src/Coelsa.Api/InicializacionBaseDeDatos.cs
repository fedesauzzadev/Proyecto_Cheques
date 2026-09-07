using Coelsa.Application;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;
using Coelsa.Domain.Validaciones;
using Coelsa.Domain.ValueObjects;
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

        // Fase B: una cuenta por CUIT demo (más una en dólares para cuits[0],
        // que emite los echeqs en dólares del seed). Cada una con su chequera N° 1.
        var nombres = new Dictionary<string, string>
        {
            [cuits[0]] = "Alfa S.R.L.",
            [cuits[1]] = "Beta S.A.",
            [cuits[2]] = "Gómez Juan Pérez",
            [cuits[3]] = "Delta Coop. Ltda."
        };
        var cuentasDemo = new Dictionary<(string Cuit, Moneda Moneda), Cuenta>();
        var chequerasDemo = new Dictionary<(string Cuit, Moneda Moneda), Chequera>();
        if (!await db.Cuentas.AnyAsync())
        {
            var combinaciones = new[]
            {
                (cuits[0], Moneda.Pesos), (cuits[0], Moneda.Dolares),
                (cuits[1], Moneda.Pesos), (cuits[2], Moneda.Pesos), (cuits[3], Moneda.Pesos)
            };

            var sucursal = 1;
            foreach (var (cuit, moneda) in combinaciones)
            {
                var cbu = ValidadorCbu.Crear("011", $"{sucursal++:D4}", $"{sucursal:D13}");
                var cuenta = Cuenta.Crear(cbu, cuit, nombres[cuit], moneda);
                db.Cuentas.Add(cuenta);
                cuentasDemo[(cuit, moneda)] = cuenta;

                var chequera = Chequera.Solicitar(cuenta.Id, 1);
                db.Chequeras.Add(chequera);
                chequerasDemo[(cuit, moneda)] = chequera;
            }
        }
        else if (!await db.Echeqs.AnyAsync())
        {
            // Las cuentas ya existen pero no hay echeqs (migración B1): se reutilizan
            // las chequeras con lugar de cada combinación (cuit, moneda).
            foreach (var cuenta in await db.Cuentas.ToListAsync())
            {
                var chequera = await db.Chequeras
                    .Where(c => c.CuentaId == cuenta.Id && c.Estado == EstadoChequera.Vigente)
                    .OrderBy(c => c.Numero)
                    .FirstOrDefaultAsync();
                if (chequera is not null)
                {
                    cuentasDemo[(cuenta.CuitTitular, cuenta.Moneda)] = cuenta;
                    chequerasDemo[(cuenta.CuitTitular, cuenta.Moneda)] = chequera;
                }
            }
        }

        if (!await db.Echeqs.AnyAsync())
        {
            var generadorIdEcheq = scope.ServiceProvider.GetRequiredService<IGeneradorIdEcheq>();
            var idsEcheq = new HashSet<string>();

            string IdUnico()
            {
                string id;
                do
                {
                    id = generadorIdEcheq.Generar();
                }
                while (!idsEcheq.Add(id));
                return id;
            }

            // Reserva un número de la chequera del (librador, moneda) y deriva su CMC7.
            Echeq NuevoEcheq(string cuitLibrador, string cuitBeneficiario, decimal monto, Moneda moneda,
                DateOnly emision, DateOnly? diferimiento, DateOnly vencimiento, DateOnly hoy,
                Caracter caracter = Caracter.AlaOrden, string? concepto = null, string? referencia = null)
            {
                var chequera = chequerasDemo[(cuitLibrador, moneda)];
                var cuenta = cuentasDemo[(cuitLibrador, moneda)];
                var numero = chequera.ReservarNumero();
                return Echeq.Crear(
                    IdUnico(),
                    cuenta.Cbu,
                    chequera.Numero,
                    numero,
                    caracter,
                    TipoDocumento.Cuit,
                    nombres[cuitLibrador],
                    nombres[cuitBeneficiario],
                    Cmc7.Derivar(cuenta.Cbu, numero).Valor,
                    cuitLibrador,
                    cuitBeneficiario,
                    monto,
                    moneda,
                    emision,
                    diferimiento,
                    vencimiento,
                    hoy,
                    concepto: concepto,
                    referencia: referencia);
            }

            for (var i = 1; i <= 12; i++)
            {
                var moneda = i % 4 == 0 ? Moneda.Dolares : Moneda.Pesos;
                var emision = hoy.AddDays(-(i % 8));
                DateOnly? diferimiento = i % 3 == 0 ? hoy.AddDays(i % 45) : null;
                var echeq = NuevoEcheq(
                    cuits[i % cuits.Length],
                    cuits[(i + 2) % cuits.Length],
                    monto: 80_000m * (i % 5 + 1),
                    moneda,
                    emision,
                    diferimiento,
                    (diferimiento ?? emision).AddDays(30),
                    hoy,
                    caracter: i % 4 == 1 ? Caracter.NoAlaOrden : Caracter.AlaOrden,
                    concepto: i % 2 == 0 ? "Pago a proveedores" : null,
                    referencia: $"SEED-{i:000}");
                echeq.Aceptar();

                AplicarEstadoDemo(echeq, i);

                db.Echeqs.Add(echeq);
            }

            // Vitrina Fase A: estados y cadenas que el flujo normal no genera solo.
            // 1. Pendiente de aceptación.
            db.Echeqs.Add(NuevoEcheq(cuits[0], cuits[1], 100_000m, Moneda.Pesos,
                hoy, null, hoy.AddDays(30), hoy));

            // 2. Repudiado por el beneficiario.
            var repudiado = NuevoEcheq(cuits[0], cuits[1], 200_000m, Moneda.Pesos,
                hoy, null, hoy.AddDays(30), hoy);
            repudiado.Repudiar("No reconozco la operación");
            db.Echeqs.Add(repudiado);

            // 3. En custodia.
            var custodia = NuevoEcheq(cuits[0], cuits[1], 300_000m, Moneda.Pesos,
                hoy, null, hoy.AddDays(30), hoy);
            custodia.Aceptar();
            custodia.PonerEnCustodia();
            db.Echeqs.Add(custodia);

            // 4. Con cadena de endosos (2 vigentes) y devolución solicitada.
            var cadena = NuevoEcheq(cuits[0], cuits[1], 400_000m, Moneda.Dolares,
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

            // 5. Vitrina Fase D1: echeq "no a la orden" con cesión solicitada.
            var cedido = NuevoEcheq(cuits[2], cuits[3], 150_000m, Moneda.Pesos,
                hoy, null, hoy.AddDays(30), hoy, caracter: Caracter.NoAlaOrden);
            cedido.Aceptar();
            db.Echeqs.Add(cedido);
            db.Cesiones.Add(Cesion.Solicitar(cedido.Id, 1, cuits[3], cuits[0], "Av. Siempreviva 742"));
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
