using CSharpFunctionalExtensions;
using GymErp.Core.Ports.Driving;
using Microsoft.AspNetCore.Mvc;

namespace GymErp.Adapters.Http.Presentation;

// Parte do adaptador HTTP: só ele sabe que "não encontrado" é 404 e "regra violada" é 422.
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
