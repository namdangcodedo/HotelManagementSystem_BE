using AppBackend.BusinessObjects.Models;
using AppBackend.Services.ApiModels;
using AutoMapper;

namespace AppBackend.Services.Mappers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            #region User
            // Map RegisterRequest -> User
            // Ignore PasswordHash because it will be set after hashing
            CreateMap<RegisterRequest, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            // Map User -> UserDto for responses
            CreateMap<User, UserDto>();
            #endregion

            #region Account
            // Add mappings for Account entities here later
            #endregion

            #region Order
            // Add mappings for Order entities here later
            #endregion
        }
    }
}