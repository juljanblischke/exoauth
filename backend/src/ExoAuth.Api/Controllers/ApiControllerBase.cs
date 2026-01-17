using ExoAuth.Application.Common.Models;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace ExoAuth.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    private IMediator? _mediator;

    protected IMediator Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();

    protected string RequestId => HttpContext.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString();

    protected IActionResult ApiOk<T>(T data, string message = "OK")
    {
        var response = ApiResponse<T>.Success(data, message);
        return Ok(WithRequestId(response));
    }

    protected IActionResult ApiOk<T>(T data, PaginationMeta pagination, string message = "OK")
    {
        var response = ApiResponse<T>.Success(data, pagination, message);
        return Ok(WithRequestId(response));
    }

    protected IActionResult ApiCreated<T>(T data, string? actionName = null, object? routeValues = null)
    {
        var response = ApiResponse<T>.Success(data, "Created", StatusCodes.Status201Created);

        if (actionName is not null)
        {
            return CreatedAtAction(actionName, routeValues, WithRequestId(response));
        }

        return StatusCode(StatusCodes.Status201Created, WithRequestId(response));
    }

    protected IActionResult ApiNoContent()
    {
        return NoContent();
    }

    protected IActionResult ApiNotFound(string message = "Resource not found")
    {
        var response = ApiResponse<object>.Error(
            message,
            StatusCodes.Status404NotFound,
            ApiError.Create(ErrorCodes.ResourceNotFound, message));

        return NotFound(WithRequestId(response));
    }

    protected IActionResult ApiBadRequest(string message, IEnumerable<ApiError>? errors = null)
    {
        var response = ApiResponse<object>.Error(message, StatusCodes.Status400BadRequest, errors);
        return BadRequest(WithRequestId(response));
    }

    private ApiResponse<T> WithRequestId<T>(ApiResponse<T> response)
    {
        return response with
        {
            Meta = response.Meta is not null
                ? response.Meta with { RequestId = RequestId }
                : new ApiResponseMeta { RequestId = RequestId }
        };
    }
}
