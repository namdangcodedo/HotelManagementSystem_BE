using AppBackend.BusinessObjects.Models;
using AppBackend.BusinessObjects.Dtos;
using AutoMapper;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.AttendanceModel;

namespace AppBackend.Services.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            #region AccountServices
            CreateMap<RegisterRequest, Account>();
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

            #region Employee
            CreateMap<Employee, EmployeeDto>();
            CreateMap<EmployeeDto, Employee>();
            #endregion

            #region CommonCode
            CreateMap<CommonCode, CommonCodeDto>();
            CreateMap<CommonCodeDto, CommonCode>();
            #endregion

            #region Amenity

            CreateMap<Amenity, AmenityWithMediumDto>()
                .ForMember(dest => dest.Images, opt => opt.Ignore());
            CreateMap<Medium, MediumDto>();
            #endregion
            #region Ảttendance
            CreateMap<Attendance, AttendanceDTO>()
                .ForMember(dto => dto.EmployeeName, opt => opt.MapFrom(att => att.Employee.FullName)).ReverseMap();
            CreateMap<Attendance, PostAttendanceRequest>().ReverseMap();
            CreateMap<EmpAttendInfo, EmpAttendInfoDTO>().ReverseMap();
            CreateMap<EmpAttendInfo, PostAttendInfosRequest>().ReverseMap();
            #endregion

            #region Comment
            CreateMap<Comment, CommentDTO>()
                .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => 
                    src.Account != null 
                        ? (src.Account.Customer != null 
                            ? src.Account.Customer.FullName 
                            : src.Account.Employee != null 
                                ? src.Account.Employee.FullName 
                                : null)
                        : null))
                .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => 
                    src.Account != null ? src.Account.Email : null))
                .ForMember(dest => dest.UserType, opt => opt.MapFrom(src => 
                    src.Account != null 
                        ? (src.Account.Customer != null 
                            ? "Customer" 
                            : src.Account.Employee != null 
                                ? "Employee" 
                                : null)
                        : null))
                .ReverseMap();
            CreateMap<Comment, PostCommentRequest>().ReverseMap();
            #endregion
        }
    }
}