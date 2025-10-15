## 🧭 Layer Responsibilities

scp AppBackend.ApiCore/appsettings.json root@103.38.236.148:/root/hotel_app/config/

- **ApiCore (Presentation Layer)**
  - Expose REST APIs.
  - Handle HTTP requests/responses.
  - Apply middleware, authentication, and rate-limiting.

- **BusinessObjects (Domain Layer)**
  - Core business entities *(User, Role, Order, etc.)*.
  - DTOs for API communication.
  - Constants, enums, and exceptions.

- **Repositories (Data Layer)**
  - Encapsulate data access logic with EF Core.
  - Provide generic repository pattern + custom repositories.
  - Optional `UnitOfWork` for managing multiple repositories.

- **Services (Business Logic Layer)**
  - Implement business rules *(e.g., user registration, login, token generation)*.
  - Orchestrate data access via repositories.
  - Use helpers (e.g., token generation, email sending, cloud upload).

---
---

## 🔄 Code Flow Example (User Registration)

### 1. **API Layer (Controller)**
- File: `AppBackend.ApiCore/Controllers/UserController.cs`
```csharp
[HttpPost("register")]
[AllowAnonymous]
[RateLimit(3, 60)] // Custom rate limit attribute
public async Task<IActionResult> Register([FromBody] RegisterRequest request)
{
    var result = await _userService.RegisterAsync(request);
    return StatusCode(result.StatusCode, result);
}


👉 Controller chỉ nhận request, gọi Service để xử lý, rồi trả response.

2. Service Layer

File: AppBackend.Services/Services/UserService.cs

public async Task<ResultModel> RegisterAsync(RegisterRequest request)
{
    var existing = await _userRepository.GetByEmailAsync(request.Email);
    if (existing != null)
        throw new AppException("Email already exists");

    var newUser = _mapper.Map<User>(request);
    newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
    newUser.CreatedAt = DateTime.UtcNow;

    await _userRepository.AddAsync(newUser);
    await _userRepository.SaveChangesAsync();

    var token = _userHelper.CreateToken(newUser);
    var refresh = _userHelper.GenerateRefreshToken();

    return new ResultModel
    {
        IsSuccess = true,
        Message = "Register success",
        Data = new { AccessToken = token, RefreshToken = refresh },
        StatusCode = StatusCodes.Status201Created
    };
}


👉 Service chứa logic nghiệp vụ (check trùng email, hash password, sinh token).

3. Repository Layer

File: AppBackend.Repositories/Repositories/UserRepo/UserRepository.cs

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(IOTShowroomContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }
}


👉 Repository giao tiếp với DbContext để CRUD dữ liệu.

4. BusinessObjects Layer

File: AppBackend.BusinessObjects/Models/User.cs

public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public DateTime CreatedAt { get; set; }
}


File: AppBackend.BusinessObjects/Dtos/UserDto.cs

public class UserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}


👉 Models = Entity trong database.
👉 DTOs = object để trả về API (giảm expose thông tin nhạy cảm như PasswordHash).

✅ Summary

Controller: Nhận request → gọi Service → trả response.

Service: Xử lý logic nghiệp vụ, gọi repository, helper.

Repository: Giao tiếp trực tiếp DB qua EF Core.

BusinessObjects: Chứa model, DTO, constants, enums, exceptions.