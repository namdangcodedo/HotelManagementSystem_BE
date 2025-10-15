using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.CommonCodeModel;

namespace AppBackend.Services.Services.CommonCodeServices
{
    public interface ICommonCodeService
    {
        Task<ResultModel> GetCommonCodeListAsync(GetCommonCodeListRequest request);
        Task<ResultModel> GetCommonCodeByIdAsync(int codeId);
        Task<ResultModel> GetCodeTypeListAsync();
        Task<ResultModel> GetCommonCodesByTypeAsync(string codeType);
        Task<ResultModel> AddCommonCodeAsync(AddCommonCodeRequest request);
        Task<ResultModel> UpdateCommonCodeAsync(UpdateCommonCodeRequest request);
        Task<ResultModel> DeleteCommonCodeAsync(int codeId);
    }
}

