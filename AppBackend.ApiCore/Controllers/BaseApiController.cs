using Microsoft.AspNetCore.Mvc;
using AppBackend.Services.ApiModels;
using System.Security.Claims;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// Base controller providing common functionality for all API controllers
    /// </summary>
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        /// <summary>
        /// Get the current authenticated user's account ID
        /// </summary>
        protected int CurrentUserId
        {
            get
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(userIdClaim, out var userId) ? userId : 0;
            }
        }

        /// <summary>
        /// Get the current authenticated user's email
        /// </summary>
        protected string? CurrentUserEmail => User.FindFirst(ClaimTypes.Email)?.Value;

        /// <summary>
        /// Get the current authenticated user's roles
        /// </summary>
        protected List<string> CurrentUserRoles => User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        /// <summary>
        /// Check if current user has a specific role
        /// </summary>
        protected bool HasRole(string role) => CurrentUserRoles.Contains(role);

        /// <summary>
        /// Check if current user is Admin
        /// </summary>
        protected bool IsAdmin => HasRole("Admin");

        /// <summary>
        /// Check if current user is Manager
        /// </summary>
        protected bool IsManager => HasRole("Manager");

        /// <summary>
        /// Handle service result and return appropriate HTTP response
        /// </summary>
        protected IActionResult HandleResult<T>(ResultModel<T> result)
        {
            if (!result.IsSuccess)
            {
                return result.ResponseCode switch
                {
                    "NOT_FOUND" => NotFound(result),
                    "UNAUTHORIZED" => Unauthorized(result),
                    "FORBIDDEN" => StatusCode(403, result),
                    "CONFLICT" => Conflict(result),
                    _ => BadRequest(result)
                };
            }

            return result.StatusCode switch
            {
                201 => CreatedAtAction(null, result),
                204 => NoContent(),
                _ => Ok(result)
            };
        }

        /// <summary>
        /// Handle service result without generic type
        /// </summary>
        protected IActionResult HandleResult(ResultModel result)
        {
            if (!result.IsSuccess)
            {
                return result.ResponseCode switch
                {
                    "NOT_FOUND" => NotFound(result),
                    "UNAUTHORIZED" => Unauthorized(result),
                    "FORBIDDEN" => StatusCode(403, result),
                    "CONFLICT" => Conflict(result),
                    _ => BadRequest(result)
                };
            }

            return result.StatusCode switch
            {
                201 => CreatedAtAction(null, result),
                204 => NoContent(),
                _ => Ok(result)
            };
        }

        /// <summary>
        /// Return validation error response
        /// </summary>
        protected IActionResult ValidationError(string message)
        {
            return BadRequest(new ResultModel
            {
                IsSuccess = false,
                ResponseCode = "VALIDATION_ERROR",
                StatusCode = 400,
                Message = message
            });
        }
    }
}
