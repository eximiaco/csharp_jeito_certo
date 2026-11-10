using CSharpFunctionalExtensions;
using GymErp.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace GymErp.Api.Presentation;

// Círculo de adaptadores de interface: traduz o resultado do caso de uso para HTTP.
// É a versão pragmática do presenter de Martin: um tradutor único, fora da Application.
public static class ResultHttp
{
    public static IActionResult ToHttp<T>(this Result<T, EnrollmentError> result) =>
        result.IsSuccess
            ? new OkObjectResult(result.Value)
            : result.Error.Kind switch
            {
                EnrollmentErrorKind.NotFound => new NotFoundObjectResult(new { error = result.Error.Message }),
                _ => new UnprocessableEntityObjectResult(new { error = result.Error.Message })
            };
}
