using AppBackend.BusinessObjects.Models;
using AppBackend.BusinessObjects.Dtos;
using AutoMapper;
using AppBackend.Services.ApiModels;

namespace AppBackend.Services.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            #region AccountServices
            CreateMap<RegisterRequest, Account>();
            #endregion

            #region BookingDtos
            // Map BookingDtos entities
            CreateMap<Booking, BookingDto>();
            CreateMap<BookingDto, Booking>();
            #endregion

            #region Room
            // Map Room entities
            CreateMap<Room, RoomDto>();
            CreateMap<RoomDto, Room>();
            #endregion

            #region Customer
            // Map Customer entities
            CreateMap<Customer, CustomerDto>();
            CreateMap<CustomerDto, Customer>();
            #endregion

            #region Account
            CreateMap<Account, AccountDto>();
            CreateMap<AccountDto, Account>();
            #endregion

            #region Role
            CreateMap<Role, RoleDto>();
            CreateMap<RoleDto, Role>();
            #endregion

            #region Employee
            CreateMap<Employee, EmployeeDto>();
            CreateMap<EmployeeDto, Employee>();
            #endregion

            #region AmenityModel

            CreateMap<Amenity, AmenityWithMediumDto>()
                .ForMember(dest => dest.Images, opt => opt.Ignore());
            CreateMap<Medium, MediumDto>();
            #endregion
        }
    }
}