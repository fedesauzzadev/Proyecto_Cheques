using Coelsa.Application.Common;
using Coelsa.Application.Dtos;
using Coelsa.Application.Puertos;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;

namespace Coelsa.Application.CasosUso;

/// <summary>
/// Certificado para ejercer acciones civiles (Fase D3): solo sobre echeqs
/// rechazados. El CUD es determinista (derivado de los datos); no se persiste.
/// Lectura directa a PostgreSQL, sin cache (como el resto de RF-04).
/// </summary>
public sealed class ObtenerCertificadoHandler(IInstrumentoRepository<Echeq> repository)
{
    public async Task<CertificadoResponse> Ejecutar(string idecheq, CancellationToken ct)
    {
        if (idecheq is null || !System.Text.RegularExpressions.Regex.IsMatch(idecheq, "^[A-Z]{11}$"))
        {
            throw new ValidacionException("El IDECHEQ debe ser alfabético de 11 letras mayúsculas.");
        }

        var echeq = await repository.ObtenerPorIdentificadorAsync(idecheq, ct)
            ?? throw new NoEncontradoException($"No existe ningún echeq con el IDECHEQ {idecheq}.");

        if (echeq.Estado != EstadoInstrumento.Rechazado)
        {
            throw new TransicionInvalidaException(
                $"Solo un echeq rechazado tiene certificado para acciones civiles (actual: {echeq.Estado}).");
        }

        return new CertificadoResponse
        {
            Cud = echeq.CalcularCud(),
            CodigoVisualizacion = echeq.CodigoVisualizacion(),
            IdEcheq = echeq.IdEcheq,
            Cmc7 = echeq.Cmc7,
            Estado = echeq.Estado.ToString(),
            MotivoRechazo = (int?)echeq.MotivoRechazo,
            CuitLibrador = echeq.CuitLibrador,
            NombreLibrador = echeq.NombreLibrador,
            CuitBeneficiario = echeq.CuitBeneficiario,
            NombreBeneficiario = echeq.NombreBeneficiario,
            Monto = echeq.Monto,
            Moneda = Mapeadores.MonedaACodigo(echeq.Moneda),
            FechaEmision = echeq.FechaEmision,
            FechaVencimiento = echeq.FechaVencimiento,
            FechaRechazo = echeq.FechaModificacion
        };
    }
}
