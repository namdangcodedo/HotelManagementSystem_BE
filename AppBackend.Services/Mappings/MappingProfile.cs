using AppBackend.BusinessObjects.Models;
using AppBackend.BusinessObjects.Dtos;
using AutoMapper;

namespace AppBackend.Services.Mappers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            #region User
            
            #endregion

            #region Booking
            // Map Booking entities
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
        }
    }
}