namespace CashFlow.SharedKernel;

/// <summary>
/// Categoria do erro de negócio. Permite que a camada de apresentação traduza
/// o erro para o status HTTP adequado sem conhecer regras de domínio.
/// </summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Failure
}

/// <summary>
/// Erro de negócio esperado (não excepcional). Exceções ficam reservadas para falhas
/// inesperadas de infraestrutura; regras de negócio retornam <see cref="Result"/>.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);
}
