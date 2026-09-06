using System.Net;
using IrmaRios.Cartera.Application.Puertos;
using IrmaRios.Cartera.Domain;
using Microsoft.Extensions.Logging;

namespace IrmaRios.Cartera.Infrastructure.Integracion;

/// <summary>
/// Adaptador HTTP del antipuerto IEmpresas contra el servicio Identidad.
/// 404 =&gt; false (la empresa no existe en el banco); otro error =&gt; IntegracionException.
/// </summary>
public class EmpresasIdentidadApi(HttpClient http, ILogger<EmpresasIdentidadApi> logger) : IEmpresas
{
    public async Task<bool> ExisteAsync(string cuit, CancellationToken ct)
    {
        HttpResponseMessage respuesta;
        try
        {
            respuesta = await http.GetAsync($"/api/v1/empresas/{cuit}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Identidad inaccesible al validar la empresa {Cuit}.", cuit);
            throw new IntegracionException("El servicio de identidad no está disponible. Reintente en unos instantes.");
        }

        return respuesta.StatusCode switch
        {
            HttpStatusCode.OK => true,
            HttpStatusCode.NotFound => false,
            _ => throw new IntegracionException(
                $"El servicio de identidad respondió {(int)respuesta.StatusCode} al validar la empresa.")
        };
    }
}
