using Coelsa.Application.Dtos;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;

namespace Coelsa.Application.Common;

public static class Mapeadores
{
    public static ChequeResponse AResponse(this ChequeFisico cheque)
    {
        var desglose = cheque.DesglosarCmc7();

        return new ChequeResponse
        {
            Identificador = cheque.Cmc7,
            Tipo = "ChequeFisico",
            DesgloseCmc7 = new DesgloseCmc7Dto
            {
                Banco = desglose.Banco,
                Sucursal = desglose.Sucursal,
                CodigoPostal = desglose.CodigoPostal,
                NumeroCheque = desglose.NumeroCheque,
                NumeroCuenta = desglose.NumeroCuenta
            },
            CuitLibrador = cheque.CuitLibrador,
            CuitBeneficiario = cheque.CuitBeneficiario,
            Monto = cheque.Monto,
            Moneda = MonedaACodigo(cheque.Moneda),
            FechaEmision = cheque.FechaEmision,
            FechaDiferimiento = cheque.FechaDiferimiento,
            Estado = cheque.Estado.ToString(),
            MotivoRechazo = (int?)cheque.MotivoRechazo,
            FechaCreacion = cheque.FechaCreacion
        };
    }

    public static EcheqResponse AResponse(this Echeq echeq)
    {
        return new EcheqResponse
        {
            Identificador = echeq.IdEcheq,
            Tipo = "Echeq",
            Cud = echeq.Cud,
            CodigoBanco = echeq.CodigoBanco,
            NumeroCuenta = echeq.NumeroCuenta,
            CuitLibrador = echeq.CuitLibrador,
            CuitBeneficiario = echeq.CuitBeneficiario,
            Monto = echeq.Monto,
            Moneda = MonedaACodigo(echeq.Moneda),
            FechaEmision = echeq.FechaEmision,
            FechaDiferimiento = echeq.FechaDiferimiento,
            Estado = echeq.Estado.ToString(),
            MotivoRechazo = (int?)echeq.MotivoRechazo,
            CantidadEndosos = echeq.CantidadEndosos,
            FechaCreacion = echeq.FechaCreacion
        };
    }

    public static string MonedaACodigo(Moneda moneda) => moneda == Moneda.Pesos ? "P" : "D";

    public static Moneda CodigoAMoneda(string codigo) => codigo == "P" ? Moneda.Pesos : Moneda.Dolares;

    public static EstadoInstrumento ParseEstado(string estado)
    {
        if (!Enum.TryParse<EstadoInstrumento>(estado, ignoreCase: true, out var resultado))
        {
            throw new ValidacionException(
                $"El estado '{estado}' no es válido. Valores posibles: {string.Join(", ", Enum.GetNames<EstadoInstrumento>())}.");
        }

        return resultado;
    }

    public static MotivoRechazo? ParseMotivoRechazo(int? codigo)
    {
        if (codigo is null)
        {
            return null;
        }

        if (!Enum.IsDefined(typeof(MotivoRechazo), codigo.Value))
        {
            throw new ValidacionException(
                $"El motivo de rechazo {codigo} no es válido. Códigos válidos: " +
                string.Join(", ", Enum.GetValues<MotivoRechazo>().Select(m => (int)m)) + ".");
        }

        return (MotivoRechazo)codigo.Value;
    }
}
