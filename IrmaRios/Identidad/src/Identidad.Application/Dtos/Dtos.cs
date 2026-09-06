using System.Text.Json.Serialization;

namespace IrmaRios.Identidad.Application.Dtos;

public sealed record PersonaRequest(
    string DocTipo,
    string DocNumero,
    string Nombre,
    string Apellido,
    string Email,
    string Rol);

/// <summary>Alta de empresa con personas autorizadas opcionales (HU-01).</summary>
public sealed record AltaEmpresaRequest(string Cuit, string RazonSocial, List<PersonaRequest>? Personas);

public sealed record VincularPersonaRequest(
    string DocTipo,
    string DocNumero,
    string Nombre,
    string Apellido,
    string Email,
    string Rol);

public sealed record PersonaResponse(
    string DocTipo,
    string DocNumero,
    string Nombre,
    string Apellido,
    string Email,
    string Rol);

public sealed record EmpresaResponse(string Cuit, string RazonSocial, DateTime CreadoUtc);

public sealed record EmpresaConPersonasResponse(string Cuit, string RazonSocial, List<PersonaResponse> Personas);

public sealed record PagedResponse<T>
{
    public List<T> Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    [JsonIgnore]
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
