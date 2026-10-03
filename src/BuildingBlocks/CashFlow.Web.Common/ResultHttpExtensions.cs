using CashFlow.SharedKernel;
using Microsoft.AspNetCore.Http;

namespace CashFlow.Web.Common;

/// <summary>Traduz erros de negócio para respostas HTTP padronizadas (RFC 7807 – Problem Details).</summary>
public static class ResultHttpExtensions
{
    public static IResult ToProblem(this Error error)
    {
        var (status, title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Requisição inválida"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflito"),
            _ => (StatusCodes.Status500InternalServerError, "Erro ao processar a requisição")
        };

        return Results.Problem(
            title: title,
            detail: error.Message,
            statusCode: status,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }

    public static IResult ToOk<T>(this Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
}
