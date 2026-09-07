using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Coelsa.Application.Common;
using Coelsa.Application.Dtos;
using Coelsa.Application.Puertos;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;
using Coelsa.Domain.Validaciones;

namespace Coelsa.Application.CasosUso;

/// <summary>
/// Gestión de cuentas corrientes emisoras y e-chequeras (Fase B, RFC-005).
/// Las creaciones son idempotentes por header Idempotency-Key (RF-02), con el
/// registro persistido en la misma transacción que la entidad.
/// </summary>
public sealed class CrearCuentaHandler(
    ICuentaRepository repository,
    IUnitOfWork unitOfWork,
    IAlmacenIdempotencia idempotencia)
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public async Task<(CuentaResponse Respuesta, bool EsReplay)> Ejecutar(
        CrearCuentaRequest request, string idempotencyKey, CancellationToken ct)
    {
        var bodyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request, JsonOpts))));

        var existente = await idempotencia.ObtenerAsync(idempotencyKey, ct);
        if (existente is not null)
        {
            if (!string.Equals(existente.BodyHash, bodyHash, StringComparison.Ordinal))
            {
                throw new ConflictoDominioException(
                    $"La Idempotency-Key '{idempotencyKey}' ya fue utilizada con otro cuerpo de request.");
            }

            return (JsonSerializer.Deserialize<CuentaResponse>(existente.ResponseJson, JsonOpts)
                ?? throw new ValidacionException("El registro de idempotencia contiene una respuesta inválida."), true);
        }

        if (await repository.ExisteCbuAsync(request.Cbu, ct))
        {
            throw new ConflictoDominioException($"Ya existe una cuenta con el CBU {request.Cbu}.");
        }

        var cuenta = Cuenta.Crear(request.Cbu, request.CuitTitular, request.NombreTitular, Mapeadores.CodigoAMoneda(request.Moneda));
        repository.Agregar(cuenta);

        var respuesta = cuenta.AResponse();
        // Cuentas y chequeras cuelgan del agregado de emisión de echeqs: se registran
        // con TipoInstrumento.Echeq (columna informativa; la búsqueda de replay es por key).
        idempotencia.Registrar(idempotencyKey, TipoInstrumento.Echeq, bodyHash, JsonSerializer.Serialize(respuesta, JsonOpts));

        await unitOfWork.SaveChangesAsync(ct);
        return (respuesta, false);
    }
}

public sealed class ListarCuentasHandler(ICuentaRepository repository)
{
    public async Task<IReadOnlyList<CuentaResponse>> Ejecutar(string? cuit, CancellationToken ct)
    {
        if (!ValidadorCuit.EsValido(cuit))
        {
            throw new ValidacionException(
                "El parámetro 'cuit' es obligatorio y debe ser un CUIT/CUIL válido de 11 dígitos.");
        }

        var cuentas = await repository.ListarPorCuitAsync(cuit!, ct);
        return cuentas.Select(c => c.AResponse()).ToList();
    }
}

public sealed class ObtenerCuentaHandler(ICuentaRepository repository)
{
    public async Task<CuentaResponse> Ejecutar(string cbu, CancellationToken ct)
    {
        if (!ValidadorCbu.EsValido(cbu))
        {
            throw new ValidacionException("El CBU debe tener 22 dígitos con verificadores válidos.");
        }

        var cuenta = await repository.ObtenerPorCbuAsync(cbu, ct)
            ?? throw new NoEncontradoException($"No existe ninguna cuenta con el CBU {cbu}.");

        return cuenta.AResponse();
    }
}

public sealed class SolicitarChequeraHandler(
    ICuentaRepository cuentas,
    IChequeraRepository chequeras,
    IUnitOfWork unitOfWork,
    IAlmacenIdempotencia idempotencia)
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public async Task<(ChequeraResponse Respuesta, bool EsReplay)> Ejecutar(
        string cbu, string idempotencyKey, CancellationToken ct)
    {
        if (!ValidadorCbu.EsValido(cbu))
        {
            throw new ValidacionException("El CBU debe tener 22 dígitos con verificadores válidos.");
        }

        // La idempotencia se evalúa sobre (cbu + key): reintentar el pedido con la
        // misma key devuelve la chequera original en vez de abrir otra numerada.
        var bodyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cbu)));

        var existente = await idempotencia.ObtenerAsync(idempotencyKey, ct);
        if (existente is not null)
        {
            if (!string.Equals(existente.BodyHash, bodyHash, StringComparison.Ordinal))
            {
                throw new ConflictoDominioException(
                    $"La Idempotency-Key '{idempotencyKey}' ya fue utilizada con otro cuerpo de request.");
            }

            return (JsonSerializer.Deserialize<ChequeraResponse>(existente.ResponseJson, JsonOpts)
                ?? throw new ValidacionException("El registro de idempotencia contiene una respuesta inválida."), true);
        }

        var cuenta = await cuentas.ObtenerPorCbuAsync(cbu, ct)
            ?? throw new NoEncontradoException($"No existe ninguna cuenta con el CBU {cbu}.");

        var numero = await chequeras.ContarPorCuentaAsync(cuenta.Id, ct) + 1;
        var chequera = Chequera.Solicitar(cuenta.Id, numero);
        chequeras.Agregar(chequera);

        var respuesta = chequera.AResponse();
        idempotencia.Registrar(idempotencyKey, TipoInstrumento.Echeq, bodyHash, JsonSerializer.Serialize(respuesta, JsonOpts));

        await unitOfWork.SaveChangesAsync(ct);
        return (respuesta, false);
    }
}

public sealed class ListarChequerasHandler(ICuentaRepository cuentas, IChequeraRepository chequeras)
{
    public async Task<IReadOnlyList<ChequeraResponse>> Ejecutar(string cbu, CancellationToken ct)
    {
        if (!ValidadorCbu.EsValido(cbu))
        {
            throw new ValidacionException("El CBU debe tener 22 dígitos con verificadores válidos.");
        }

        var cuenta = await cuentas.ObtenerPorCbuAsync(cbu, ct)
            ?? throw new NoEncontradoException($"No existe ninguna cuenta con el CBU {cbu}.");

        var lista = await chequeras.ListarPorCuentaAsync(cuenta.Id, ct);
        return lista.Select(c => c.AResponse()).ToList();
    }
}

/// <summary>
/// Padrón simulado de titulares (Fase B3, "lupa" del home banking): valida el
/// documento y devuelve el nombre si el titular tiene cuenta en el simulador.
/// </summary>
public sealed class ObtenerTitularHandler(ICuentaRepository cuentas)
{
    public async Task<TitularResponse> Ejecutar(string? tipoDoc, string? numero, CancellationToken ct)
    {
        var tipo = ValidadorDocumento.CodigoATipo(tipoDoc)
            ?? throw new ValidacionException("El tipo de documento debe ser 'CUIT', 'CUIL' o 'CDI'.");

        if (!ValidadorDocumento.EsValido(tipo, numero))
        {
            throw new ValidacionException(
                $"El documento '{numero}' no es válido para el tipo {ValidadorDocumento.TipoACodigo(tipo)}.");
        }

        var cuenta = (await cuentas.ListarPorCuitAsync(numero!, ct)).FirstOrDefault();

        return new TitularResponse
        {
            TipoDoc = ValidadorDocumento.TipoACodigo(tipo),
            Numero = numero!,
            Nombre = cuenta?.NombreTitular,
            Bancarizado = cuenta is not null
        };
    }
}
