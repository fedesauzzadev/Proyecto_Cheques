namespace Coelsa.Domain;

public class ValidacionException : Exception
{
    public ValidacionException(string mensaje) : base(mensaje)
    {
    }
}

/// <summary>Transición de estado inválida (mapea a 422 Unprocessable Entity).</summary>
public class TransicionInvalidaException : Exception
{
    public TransicionInvalidaException(string mensaje) : base(mensaje)
    {
    }
}

/// <summary>Conflicto de unicidad de negocio (mapea a 409 Conflict).</summary>
public class ConflictoDominioException : Exception
{
    public ConflictoDominioException(string mensaje) : base(mensaje)
    {
    }
}

/// <summary>Recurso inexistente (mapea a 404 Not Found).</summary>
public class NoEncontradoException : Exception
{
    public NoEncontradoException(string mensaje) : base(mensaje)
    {
    }
}
