using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Exceptions;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Repositories.UserRepo;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ServicesHelpers;
using AutoMapper;
using Microsoft.AspNetCore.Http;

namespace AppBackend.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly UserHelper _userHelper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(
            IUserRepository userRepository,
            IMapper mapper,
            UserHelper userHelper,
            IHttpContextAccessor httpContextAccessor)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _userHelper = userHelper;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ResultModel> RegisterAsync(RegisterRequest request)
        {
            // Check email duplication
            var existing = await _userRepository.GetByEmailAsync(request.Email);
            if (existing != null)
                throw new AppException(
                    CommonMessageConstants.EXISTED,
                    string.Format(CommonMessageConstants.VALUE_DUPLICATED, "Email"),
                    StatusCodes.Status400BadRequest
                );

            // Map & hash password
            var newUser = _mapper.Map<User>(request);
            newUser.PasswordHash = _userHelper.HashPassword(request.Password);
            newUser.CreatedAt = DateTime.UtcNow;
            newUser.UpdatedAt = DateTime.UtcNow;

            await _userRepository.AddAsync(newUser);
            await _userRepository.SaveChangesAsync();
            var role = await _userRepository.GetByIdAsync(newUser.RoleId ?? 3);
            
            // Generate tokens
            var accessToken = _userHelper.CreateToken(newUser);
            var refreshToken = _userHelper.GenerateRefreshToken();
            var refreshExpiry = _userHelper.GetRefreshTokenExpiry();

            SaveRefreshTokenToSession(newUser.UserId, refreshToken, refreshExpiry);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.REGISTER_SUCCESS,
                Data = new
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    RefreshTokenExpiry = refreshExpiry
                },
                StatusCode = StatusCodes.Status201Created
            };
        }

        public async Task<ResultModel> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null || !_userHelper.VerifyPassword(request.Password, user.PasswordHash ?? ""))
                throw new AppException(
                    CommonMessageConstants.UNAUTHORIZED,
                    CommonMessageConstants.PASSWORD_INCORRECT,
                    StatusCodes.Status401Unauthorized
                );

            var accessToken = _userHelper.CreateToken(user);
            var refreshToken = _userHelper.GenerateRefreshToken();
            var refreshExpiry = _userHelper.GetRefreshTokenExpiry();

            SaveRefreshTokenToSession(user.UserId, refreshToken, refreshExpiry);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.LOGIN_SUCCESS,
                Data = new
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    RefreshTokenExpiry = refreshExpiry
                },
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            var userDtos = _mapper.Map<IEnumerable<UserDto>>(users);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = userDtos,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> GetUserByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "User"),
                    StatusCodes.Status404NotFound
                );

            var dto = _mapper.Map<UserDto>(user);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = dto,
                StatusCode = StatusCodes.Status200OK
            };
        }

        // --- Private Helper ---
        private void SaveRefreshTokenToSession(int userId, string refreshToken, DateTime expiry)
        {
            if (_httpContextAccessor.HttpContext?.Session == null) return;

            _httpContextAccessor.HttpContext.Session.SetString("RefreshToken", refreshToken);
            _httpContextAccessor.HttpContext.Session.SetString("UserId", userId.ToString());
            _httpContextAccessor.HttpContext.Session.SetString("RefreshExpiry", expiry.ToString("O"));
        }
    }
}
