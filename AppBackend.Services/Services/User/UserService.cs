using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Exceptions;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ServicesHelpers;
using AutoMapper;
using Microsoft.AspNetCore.Http;

namespace AppBackend.Services.Services.Account
{
    public class AccountService : IAccountService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly UserHelper _userHelper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccountService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            UserHelper userHelper,
            IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userHelper = userHelper;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ResultModel> RegisterAsync(RegisterRequest request)
        {
            // Check email duplication
            var existing = await _unitOfWork.Accounts.GetByEmailAsync(request.Email);
            if (existing != null)
                throw new AppException(
                    CommonMessageConstants.EXISTED,
                    string.Format(CommonMessageConstants.VALUE_DUPLICATED, "Email"),
                    StatusCodes.Status400BadRequest
                );

            // Map & hash password
            var newAccount = _mapper.Map<Account>(request);
            newAccount.PasswordHash = _userHelper.HashPassword(request.Password);
            newAccount.CreatedAt = DateTime.UtcNow;
            newAccount.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Accounts.AddAsync(newAccount);
            await _unitOfWork.SaveChangesAsync();

            // Generate tokens
            var accessToken = _userHelper.CreateToken(newAccount);
            var refreshToken = _userHelper.GenerateRefreshToken();
            var refreshExpiry = _userHelper.GetRefreshTokenExpiry();

            SaveRefreshTokenToSession(newAccount.AccountId, refreshToken, refreshExpiry);

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
            var account = await _unitOfWork.Accounts.GetByEmailAsync(request.Email);
            if (account == null || !_userHelper.VerifyPassword(request.Password, account.PasswordHash ?? ""))
                throw new AppException(
                    CommonMessageConstants.UNAUTHORIZED,
                    CommonMessageConstants.PASSWORD_INCORRECT,
                    StatusCodes.Status401Unauthorized
                );

            var accessToken = _userHelper.CreateToken(account);
            var refreshToken = _userHelper.GenerateRefreshToken();
            var refreshExpiry = _userHelper.GetRefreshTokenExpiry();

            SaveRefreshTokenToSession(account.AccountId, refreshToken, refreshExpiry);

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

        public async Task<ResultModel> GetAllAccountsAsync()
        {
            var accounts = await _unitOfWork.Accounts.GetAllAsync();
            var accountDtos = _mapper.Map<IEnumerable<AccountDto>>(accounts);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = accountDtos,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> GetAccountByIdAsync(int id)
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(id);
            if (account == null)
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Account"),
                    StatusCodes.Status404NotFound
                );

            var dto = _mapper.Map<AccountDto>(account);

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
        private void SaveRefreshTokenToSession(int accountId, string refreshToken, DateTime expiry)
        {
            if (_httpContextAccessor.HttpContext?.Session == null) return;

            _httpContextAccessor.HttpContext.Session.SetString("RefreshToken", refreshToken);
            _httpContextAccessor.HttpContext.Session.SetString("AccountId", accountId.ToString());
            _httpContextAccessor.HttpContext.Session.SetString("RefreshExpiry", expiry.ToString("O"));
        }
    }
}
