using Microsoft.AspNetCore.Http;

namespace AppBackend.BusinessObjects.Dtos;

public class CloudinaryUploadRequestDto
{
    public IFormFile File { get; set; }
    public string? Folder { get; set; } = "SWP391"; // default folder
}