# Coding Conventions for Hotel Management System Backend

## General Rules
- **Language:** C# (.NET Core/ASP.NET Core)
- **Style:** Follow Microsoft C# Coding Guidelines
- **Indentation:** 4 spaces (no tabs)
- **Line Length:** Max 120 characters
- **Braces:** Opening brace on same line as declaration

## Naming Conventions
- **Classes/Interfaces:** PascalCase (e.g., `AccountController`, `IAccountService`)
- **Methods/Properties:** PascalCase (e.g., `GetCustomerProfileAsync`, `FullName`)
- **Variables/Parameters:** camelCase (e.g., `accountId`, `request`)
- **Constants:** PascalCase with underscores if needed (e.g., `MAX_LENGTH`)
- **Namespaces:** PascalCase, matching folder structure (e.g., `AppBackend.ApiCore.Controllers`)
- **Files:** Match class name (e.g., `AccountController.cs`)

## Code Structure
- **Using Statements:** At top, grouped by system then custom, alphabetical
- **Namespace Declaration:** One per file, matching folder
- **Class Declaration:** Public by default, inherit from base classes
- **Methods:** Async where applicable, use `Task<T>` return type
- **Properties:** Auto-implemented, nullable where optional
- **Comments:** XML comments for public APIs, inline for complex logic

## Patterns & Practices
- **Dependency Injection:** Constructor injection, readonly fields
- **Exception Handling:** Custom exceptions (e.g., `AppException`), global handling in middleware
- **Validation:** DataAnnotations on models, ModelState in controllers
- **Async/Await:** Always use for I/O operations
- **Result Models:** Use `ResultModel` for API responses
- **Helpers:** Static classes for reusable logic (e.g., `TokenHelper`)
- **Repositories:** Generic repository pattern with EF Core
- **Services:** Business logic, orchestrate repositories and helpers

## File Organization
- **Controllers:** Thin, delegate to services, handle HTTP
- **Services:** Implement interfaces, contain business rules
- **Repositories:** Data access, custom queries
- **Models:** Database entities
- **DTOs:** API request/response models
- **Helpers:** Cross-cutting concerns

## Example Code Snippet
```csharp
using System.ComponentModel.DataAnnotations;
using AppBackend.Services.Services.AccountServices;

namespace AppBackend.ApiCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccountController : BaseApiController
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var result = await _accountService.GetCustomerProfileAsync(CurrentUserId);
            return HandleResult(result);
        }
    }
}
```

## Validation Rules
- Use `[Required]`, `[StringLength]`, `[EmailAddress]`, etc.
- Custom error messages in Vietnamese where applicable
- Validate in controllers with `ModelState.IsValid`

## Security
- Use JWT tokens for authentication
- Authorize attributes on controllers/actions
- Hash passwords with BCrypt
- Avoid exposing sensitive data in DTOs

---
Follow these conventions strictly to maintain code quality and consistency.
