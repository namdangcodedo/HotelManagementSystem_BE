using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Dtos;
using AppBackend.BusinessObjects.Exceptions;
using AppBackend.Repositories.UnitOfWork;
using AutoMapper;
using Microsoft.AspNetCore.Http;

namespace AppBackend.Services.AccountServices
{
    public interface IAccountService
    {
        Task<ResultModel> GetAccountByIdAsync(int id);
        Task<ResultModel> EditProfileAsync(EditProfileRequest request);
    }

    public class AccountService : IAccountService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AccountService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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

        public async Task<ResultModel> EditProfileAsync(EditProfileRequest request)
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId);
            if (account == null)
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Account"),
                    StatusCodes.Status404NotFound
                );

            // Update properties
            account.Email = request.Email ?? account.Email;
            account.Phone = request.Phone ?? account.Phone;
            // Add more fields as needed

            await _unitOfWork.SaveChangesAsync();

            var dto = _mapper.Map<AccountDto>(account);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.UPDATE_SUCCESS,
                Data = dto,
                StatusCode = StatusCodes.Status200OK
            };
        }
    }
}
