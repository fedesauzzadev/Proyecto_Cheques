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
            FechaVencimiento = cheque.FechaVencimiento,
            Estado = cheque.Estado.ToString(),
            MotivoRechazo = (int?)cheque.MotivoRechazo,
            FechaCreacion = cheque.FechaCreacion
        };
    }

    public static EcheqResponse AResponse(this Echeq echeq)
    {
        var desglose = echeq.DesglosarCmc7();

        return new EcheqResponse
        {
            Identificador = echeq.IdEcheq,
            Tipo = "Echeq",
            CbuEmisor = echeq.CbuEmisor,
            NumeroChequera = echeq.NumeroChequera,
            NumeroCheque = echeq.NumeroCheque,
            Caracter = CaracterACodigo(echeq.Caracter),
            Modo = "Cruzado",
            TipoDocBeneficiario = TipoDocACodigo(echeq.TipoDocBeneficiario),
            NombreLibrador = echeq.NombreLibrador,
            NombreBeneficiario = echeq.NombreBeneficiario,
            Concepto = echeq.Concepto,
            Motivo = echeq.Motivo,
            Referencia = echeq.Referencia,
            EmailNotificacion = echeq.EmailNotificacion,
            Cmc7 = echeq.Cmc7,
            DesgloseCmc7 = new DesgloseCmc7Dto
            {
                Banco = desglose.Banco,
                Sucursal = desglose.Sucursal,
                CodigoPostal = desglose.CodigoPostal,
                NumeroCheque = desglose.NumeroCheque,
                NumeroCuenta = desglose.NumeroCuenta
            },
            CuitLibrador = echeq.CuitLibrador,
            CuitBeneficiario = echeq.CuitBeneficiario,
            Monto = echeq.Monto,
            Moneda = MonedaACodigo(echeq.Moneda),
            FechaEmision = echeq.FechaEmision,
            FechaDiferimiento = echeq.FechaDiferimiento,
            FechaVencimiento = echeq.FechaVencimiento,
            Estado = echeq.Estado.ToString(),
            MotivoRechazo = (int?)echeq.MotivoRechazo,
            MotivoRepudio = echeq.MotivoRepudio,
            CantidadEndosos = echeq.CantidadEndosos,
            FechaCreacion = echeq.FechaCreacion
        };
    }

    public static EndosoResponse AResponse(this Endoso endoso)
    {
        return new EndosoResponse
        {
            Orden = endoso.Orden,
            CuitEndosante = endoso.CuitEndosante,
            CuitEndosatario = endoso.CuitEndosatario,
            Estado = endoso.Estado.ToString(),
            FechaCreacion = endoso.FechaCreacion
        };
    }

    public static DevolucionResponse AResponse(this Devolucion devolucion)
    {
        return new DevolucionResponse
        {
            Numero = devolucion.Numero,
            CuitSolicitante = devolucion.CuitSolicitante,
            Motivo = devolucion.Motivo,
            Estado = devolucion.Estado.ToString(),
            FechaCreacion = devolucion.FechaCreacion
        };
    }

    public static CesionResponse AResponse(this Cesion cesion)
    {
        return new CesionResponse
        {
            Numero = cesion.Numero,
            CuitCedente = cesion.CuitCedente,
            CuitCesionario = cesion.CuitCesionario,
            DomicilioCesionario = cesion.DomicilioCesionario,
            Estado = cesion.Estado.ToString(),
            FechaCreacion = cesion.FechaCreacion
        };
    }

    public static CuentaResponse AResponse(this Cuenta cuenta)
    {
        return new CuentaResponse
        {
            Cbu = cuenta.Cbu,
            Banco = cuenta.Banco,
            Sucursal = cuenta.Sucursal,
            NumeroCuenta = cuenta.NumeroCuenta,
            CuitTitular = cuenta.CuitTitular,
            NombreTitular = cuenta.NombreTitular,
            Moneda = MonedaACodigo(cuenta.Moneda),
            FechaCreacion = cuenta.FechaCreacion
        };
    }

    public static ChequeraResponse AResponse(this Chequera chequera)
    {
        return new ChequeraResponse
        {
            Numero = chequera.Numero,
            CantidadTotal = Chequera.CantidadNumeros,
            ProximoNumero = chequera.ProximoNumero,
            Disponibles = Math.Max(0, Chequera.CantidadNumeros - chequera.ProximoNumero + 1),
            Estado = chequera.Estado.ToString(),
            FechaSolicitud = chequera.FechaSolicitud,
            FechaHabilitacion = chequera.FechaHabilitacion
        };
    }

    public static string MonedaACodigo(Moneda moneda) => moneda == Moneda.Pesos ? "P" : "D";

    public static Moneda CodigoAMoneda(string codigo) => codigo == "P" ? Moneda.Pesos : Moneda.Dolares;

    public static string CaracterACodigo(Caracter caracter) => caracter == Caracter.AlaOrden ? "AlaOrden" : "NoAlaOrden";

    public static Caracter CodigoACaracter(string codigo) => codigo == "NoAlaOrden" ? Caracter.NoAlaOrden : Caracter.AlaOrden;

    public static string TipoDocACodigo(TipoDocumento tipo) => Domain.Validaciones.ValidadorDocumento.TipoACodigo(tipo);

    public static TipoDocumento CodigoATipoDoc(string codigo)
        => Domain.Validaciones.ValidadorDocumento.CodigoATipo(codigo)
            ?? throw new ValidacionException("El tipo de documento debe ser 'CUIT', 'CUIL' o 'CDI'.");

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
