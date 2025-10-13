using AppBackend.BusinessObjects.Dtos;
using AppBackend.Services.ApiModels;

namespace AppBackend.Services;

public interface ICloudinaryService
{
    Task<ResultModel<CloudinaryUploadResponseDto>> UploadAsync(CloudinaryUploadRequestDto request);
    Task<ResultModel<CloudinaryDeleteResponseDto>> DeleteAsync(string publicId);
}