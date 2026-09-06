using System.Text.RegularExpressions;
using Coelsa.Application.Common;
using Coelsa.Application.Dtos;
using Coelsa.Application.Puertos;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;

namespace Coelsa.Application.CasosUso;

/// <summary>
/// Consulta individual por identificador de negocio (SPEC RF-04):
/// CMC7 para cheques físicos, IDECHEQ para echeqs. Sin cache (va directo a PostgreSQL).
/// </summary>
public abstract class ObtenerInstrumentoHandler<TEntidad, TResponse>(IInstrumentoRepository<TEntidad> repository)
    where TEntidad : class, IInstrumento
    where TResponse : class
{
    private readonly IInstrumentoRepository<TEntidad> _repository = repository;

    protected abstract string NombreRecurso { get; }

    protected abstract void ValidarFormatoIdentificador(string identificador);

    protected abstract TResponse Mapear(TEntidad entidad);

    public async Task<TResponse> Ejecutar(string identificador, CancellationToken ct)
    {
        ValidarFormatoIdentificador(identificador);

        var entidad = await _repository.ObtenerPorIdentificadorAsync(identificador, ct)
            ?? throw new NoEncontradoException($"No existe {NombreRecurso} con el identificador {identificador}.");

        return Mapear(entidad);
    }
}

public sealed partial class ObtenerChequeHandler(IInstrumentoRepository<ChequeFisico> repository)
    : ObtenerInstrumentoHandler<ChequeFisico, ChequeResponse>(repository)
{
    protected override string NombreRecurso => "ningún cheque físico";

    [GeneratedRegex(@"^\d{30}$")]
    private static partial Regex PatronCmc7();

    protected override void ValidarFormatoIdentificador(string identificador)
    {
        if (!PatronCmc7().IsMatch(identificador))
        {
            throw new ValidacionException("El CMC7 debe ser un código magnetizable de 30 dígitos.");
        }
    }

    protected override ChequeResponse Mapear(ChequeFisico entidad) => entidad.AResponse();
}

public sealed partial class ObtenerEcheqHandler(IInstrumentoRepository<Echeq> repository)
    : ObtenerInstrumentoHandler<Echeq, EcheqResponse>(repository)
{
    protected override string NombreRecurso => "ningún echeq";

    [GeneratedRegex("^[A-Z]{11}$")]
    private static partial Regex PatronIdEcheq();

    protected override void ValidarFormatoIdentificador(string identificador)
    {
        if (!PatronIdEcheq().IsMatch(identificador))
        {
            throw new ValidacionException("El IDECHEQ debe ser alfabético de 11 letras mayúsculas.");
        }
    }

    protected override EcheqResponse Mapear(Echeq entidad) => entidad.AResponse();
}
