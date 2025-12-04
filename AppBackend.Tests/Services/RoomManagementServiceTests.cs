using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels.RoomManagement;
using AppBackend.Services.Services.RoomManagement;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AppBackend.Tests.Services
{
    public class RoomManagementServiceTests : IDisposable
    {
        private readonly HotelManagementContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly Mock<ILogger<RoomManagementService>> _loggerMock;
        private readonly RoomManagementService _service;

        public RoomManagementServiceTests()
        {
            // Setup InMemory Database
            var options = new DbContextOptionsBuilder<HotelManagementContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            _context = new HotelManagementContext(options);
            _unitOfWork = new AppBackend.Repositories.UnitOfWork.UnitOfWork(_context);
            _loggerMock = new Mock<ILogger<RoomManagementService>>();
            _service = new RoomManagementService(_unitOfWork, _loggerMock.Object);

            // Seed test data
            SeedTestData().Wait();
        }

        private async Task SeedTestData()
        {
            // Seed RoomStatuses
            var roomStatuses = new List<CommonCode>
            {
                new() { CodeId = 1, CodeType = "RoomStatus", CodeValue = "Trống", CodeName = "Available", IsActive = true },
                new() { CodeId = 2, CodeType = "RoomStatus", CodeValue = "Đã đặt", CodeName = "Booked", IsActive = true },
                new() { CodeId = 3, CodeType = "RoomStatus", CodeValue = "Đang sử dụng", CodeName = "Occupied", IsActive = true },
                new() { CodeId = 4, CodeType = "RoomStatus", CodeValue = "Đang dọn dẹp", CodeName = "Cleaning", IsActive = true },
                new() { CodeId = 5, CodeType = "RoomStatus", CodeValue = "Bảo trì", CodeName = "Maintenance", IsActive = true },
                new() { CodeId = 6, CodeType = "RoomStatus", CodeValue = "Chờ kiểm tra", CodeName = "PendingInspection", IsActive = true },
                new() { CodeId = 7, CodeType = "RoomStatus", CodeValue = "Ngừng hoạt động", CodeName = "OutOfService", IsActive = true }
            };
            _context.CommonCodes.AddRange(roomStatuses);

            // Seed RoomTypes
            var roomTypes = new List<RoomType>
            {
                new() { RoomTypeId = 1, TypeName = "Standard", TypeCode = "STD", BasePriceNight = 500000, MaxOccupancy = 2, IsActive = true },
                new() { RoomTypeId = 2, TypeName = "Deluxe", TypeCode = "DLX", BasePriceNight = 1000000, MaxOccupancy = 3, IsActive = true }
            };
            _context.RoomTypes.AddRange(roomTypes);

            // Seed Rooms - Added Floor information
            var rooms = new List<Room>
            {
                new() { RoomId = 1, RoomName = "101", RoomTypeId = 1, StatusId = 1, Description = "Standard Room", CreatedAt = DateTime.UtcNow },
                new() { RoomId = 2, RoomName = "102", RoomTypeId = 1, StatusId = 2, Description = "Standard Room", CreatedAt = DateTime.UtcNow },
                new() { RoomId = 3, RoomName = "103", RoomTypeId = 1, StatusId = 3, Description = "Standard Room", CreatedAt = DateTime.UtcNow },
                new() { RoomId = 4, RoomName = "201", RoomTypeId = 2, StatusId = 1, Description = "Deluxe Room", CreatedAt = DateTime.UtcNow },
                new() { RoomId = 5, RoomName = "202", RoomTypeId = 2, StatusId = 4, Description = "Deluxe Room", CreatedAt = DateTime.UtcNow }
            };
            _context.Rooms.AddRange(rooms);

            await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region SearchRoomsAsync Tests

        [Fact]
        public async Task SearchRoomsAsync_ShouldReturnAllRooms_WhenNoFilter()
        {
            // Arrange
            var request = new SearchRoomsRequest { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _service.SearchRoomsAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            
            var response = result.Data as RoomListResponse;
            response.Should().NotBeNull();
            response!.Rooms.Should().HaveCount(5);
            response.TotalRecords.Should().Be(5);
            response.TotalPages.Should().Be(1);
        }

        [Fact]
        public async Task SearchRoomsAsync_ShouldFilterByRoomName()
        {
            // Arrange
            var request = new SearchRoomsRequest { RoomName = "101", PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _service.SearchRoomsAsync(request);

            // Assert
            var response = result.Data as RoomListResponse;
            response.Should().NotBeNull();
            response!.Rooms.Should().HaveCount(1);
            response.Rooms.First().RoomName.Should().Be("101");
        }

        [Fact]
        public async Task SearchRoomsAsync_ShouldFilterByRoomType()
        {
            // Arrange
            var request = new SearchRoomsRequest { RoomTypeId = 1, PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _service.SearchRoomsAsync(request);

            // Assert
            var response = result.Data as RoomListResponse;
            response.Should().NotBeNull();
            response!.Rooms.Should().HaveCount(3); // Rooms 101, 102, 103
            response.Rooms.Should().AllSatisfy(r => r.RoomTypeId.Should().Be(1));
        }

        [Fact]
        public async Task SearchRoomsAsync_ShouldFilterByStatusId()
        {
            // Arrange
            var request = new SearchRoomsRequest { StatusId = 1, PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _service.SearchRoomsAsync(request);

            // Assert
            var response = result.Data as RoomListResponse;
            response.Should().NotBeNull();
            response!.Rooms.Should().HaveCount(2); // Rooms 101, 201
            response.Rooms.Should().AllSatisfy(r => r.StatusCode.Should().Be("Available"));
        }

        [Fact]
        public async Task SearchRoomsAsync_ShouldFilterByFloor()
        {
            // Arrange
            var request = new SearchRoomsRequest { Floor = 2, PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _service.SearchRoomsAsync(request);

            // Assert
            var response = result.Data as RoomListResponse;
            response.Should().NotBeNull();
            response!.Rooms.Should().HaveCount(2); // Rooms 201, 202
            response.Rooms.Should().AllSatisfy(r => r.RoomName.Should().StartWith("2"));
        }

        [Fact]
        public async Task SearchRoomsAsync_ShouldSupportPagination()
        {
            // Arrange
            var request = new SearchRoomsRequest { PageNumber = 1, PageSize = 2 };

            // Act
            var result = await _service.SearchRoomsAsync(request);

            // Assert
            var response = result.Data as RoomListResponse;
            response.Should().NotBeNull();
            response!.Rooms.Should().HaveCount(2);
            response.TotalRecords.Should().Be(5);
            response.TotalPages.Should().Be(3);
            response.PageNumber.Should().Be(1);
            response.PageSize.Should().Be(2);
        }

        #endregion

        #region GetRoomMapAsync Tests

        [Fact]
        public async Task GetRoomMapAsync_ShouldReturnAllFloorsMap_WhenFloorIsNull()
        {
            // Arrange & Act
            var result = await _service.GetRoomMapAsync(null);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GetRoomMapAsync_ShouldReturnSpecificFloorMap_WhenFloorIsProvided()
        {
            // Arrange & Act
            var result = await _service.GetRoomMapAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GetRoomMapAsync_ShouldReturnEmptyMap_WhenFloorHasNoRooms()
        {
            // Arrange & Act
            var result = await _service.GetRoomMapAsync(99);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
        }

        #endregion

        #region GetRoomDetailAsync Tests

        [Fact]
        public async Task GetRoomDetailAsync_ShouldReturnRoomDetails_WhenRoomExists()
        {
            // Arrange
            int roomId = 1;

            // Act
            var result = await _service.GetRoomDetailAsync(roomId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            var room = result.Data as RoomDetailDto;
            room.Should().NotBeNull();
            room!.RoomId.Should().Be(1);
            room.RoomName.Should().Be("101");
            room.RoomTypeName.Should().Be("Standard");
            room.Status.Should().Be("Trống");
            room.StatusCode.Should().Be("Available");
        }

        [Fact]
        public async Task GetRoomDetailAsync_ShouldReturnNotFound_WhenRoomDoesNotExist()
        {
            // Arrange
            int roomId = 999;

            // Act
            var result = await _service.GetRoomDetailAsync(roomId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.ResponseCode.Should().Be("NOT_FOUND");
            result.Message.Should().Contain("Không tìm thấy phòng");
        }

        #endregion

        #region GetRoomStatusSummaryAsync Tests

        [Fact]
        public async Task GetRoomStatusSummaryAsync_ShouldReturnCorrectSummary()
        {
            // Act
            var result = await _service.GetRoomStatusSummaryAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNull();
            
            // Verify counts based on seed data
            // Available: 2 (rooms 101, 201)
            // Booked: 1 (room 102)
            // Occupied: 1 (room 103)
            // Cleaning: 1 (room 202)
        }

        #endregion

        #region ChangeRoomStatusAsync - Role-based Permission Tests

        [Fact]
        public async Task ChangeRoomStatusAsync_Admin_ShouldAllowAllTransitions()
        {
            // Arrange
            var request = new ChangeRoomStatusRequest { RoomId = 1, NewStatusId = 5, Reason = "Admin maintenance" };
            string userRole = "Admin";
            int userId = 1;

            // Act
            var result = await _service.ChangeRoomStatusAsync(request, userId, userRole);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Contain("Bảo trì");

            // Verify status was actually changed
            var room = await _unitOfWork.Rooms.GetByIdAsync(1);
            room.Should().NotBeNull();
            room!.StatusId.Should().Be(5); // Maintenance
        }

        [Fact]
        public async Task ChangeRoomStatusAsync_Manager_ShouldAllowAllTransitions()
        {
            // Arrange
            var request = new ChangeRoomStatusRequest { RoomId = 1, NewStatusId = 7 }; // OutOfService
            string userRole = "Manager";
            int userId = 1;

            // Act
            var result = await _service.ChangeRoomStatusAsync(request, userId, userRole);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task ChangeRoomStatusAsync_Receptionist_ShouldAllow_AvailableToBooked()
        {
            // Arrange
            var request = new ChangeRoomStatusRequest { RoomId = 1, NewStatusId = 2 }; // Booked
            string userRole = "Receptionist";
            int userId = 1;

            // Act
            var result = await _service.ChangeRoomStatusAsync(request, userId, userRole);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Contain("Đã đặt");

            var room = await _unitOfWork.Rooms.GetByIdAsync(1);
            room!.StatusId.Should().Be(2); // Booked
        }

        [Fact]
        public async Task ChangeRoomStatusAsync_Receptionist_ShouldAllow_BookedToOccupied()
        {
            // Arrange
            var request = new ChangeRoomStatusRequest { RoomId = 2, NewStatusId = 3 }; // Occupied
            string userRole = "Receptionist";
            int userId = 1;

            // Act
            var result = await _service.ChangeRoomStatusAsync(request, userId, userRole);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task ChangeRoomStatusAsync_Receptionist_ShouldDeny_AvailableToCleaning()
        {
            // Arrange
            var request = new ChangeRoomStatusRequest { RoomId = 1, NewStatusId = 4 }; // Cleaning
            string userRole = "Receptionist";
            int userId = 1;

            // Act
            var result = await _service.ChangeRoomStatusAsync(request, userId, userRole);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            result.ResponseCode.Should().Be("FORBIDDEN");
            result.Message.Should().Contain("không có quyền");

            // Verify status was NOT changed
            var room = await _unitOfWork.Rooms.GetByIdAsync(1);
            room!.StatusId.Should().Be(1); // Still Available
        }

        [Fact]
        public async Task ChangeRoomStatusAsync_Receptionist_ShouldDeny_AvailableToMaintenance()
        {
            // Arrange
            var request = new ChangeRoomStatusRequest { RoomId = 1, NewStatusId = 5 }; // Maintenance
            string userRole = "Receptionist";
            int userId = 1;

            // Act
            var result = await _service.ChangeRoomStatusAsync(request, userId, userRole);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            result.ResponseCode.Should().Be("FORBIDDEN");
        }

        [Fact]
        public async Task ChangeRoomStatusAsync_Housekeeper_ShouldAllow_AvailableToCleaning()
        {
            // Arrange
            var request = new ChangeRoomStatusRequest { RoomId = 1, NewStatusId = 4 }; // Cleaning
            string userRole = "Housekeeper";
            int userId = 1;

            // Act
            var result = await _service.ChangeRoomStatusAsync(request, userId, userRole);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            var room = await _unitOfWork.Rooms.GetByIdAsync(1);
            room!.StatusId.Should().Be(4); // Cleaning
        }

        [Fact]
        public async Task ChangeRoomStatusAsync_Housekeeper_ShouldAllow_CleaningToAvailable()
        {
            // Arrange
            var request = new ChangeRoomStatusRequest { RoomId = 5, NewStatusId = 1 }; // Available
            string userRole = "Housekeeper";
            int userId = 1;

            // Act
            var result = await _service.ChangeRoomStatusAsync(request, userId, userRole);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task ChangeRoomStatusAsync_Housekeeper_ShouldDeny_AvailableToMaintenance()
        {
            // Arrange
            var request = new ChangeRoomStatusRequest { RoomId = 1, NewStatusId = 5 }; // Maintenance
            string userRole = "Housekeeper";
            int userId = 1;

            // Act
            var result = await _service.ChangeRoomStatusAsync(request, userId, userRole);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task ChangeRoomStatusAsync_Technician_ShouldAllow_AvailableToMaintenance()
        {
            // Arrange
            var request = new ChangeRoomStatusRequest { RoomId = 1, NewStatusId = 5 }; // Maintenance
            string userRole = "Technician";
            int userId = 1;

            // Act
            var result = await _service.ChangeRoomStatusAsync(request, userId, userRole);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            var room = await _unitOfWork.Rooms.GetByIdAsync(1);
            room!.StatusId.Should().Be(5); // Maintenance
        }

        [Fact]
        public async Task ChangeRoomStatusAsync_Technician_ShouldDeny_AvailableToCleaning()
        {
            // Arrange
            var request = new ChangeRoomStatusRequest { RoomId = 1, NewStatusId = 4 }; // Cleaning
            string userRole = "Technician";
            int userId = 1;

            // Act
            var result = await _service.ChangeRoomStatusAsync(request, userId, userRole);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task ChangeRoomStatusAsync_ShouldReturnNotFound_WhenRoomDoesNotExist()
        {
            // Arrange
            var request = new ChangeRoomStatusRequest { RoomId = 999, NewStatusId = 1 };
            string userRole = "Admin";
            int userId = 1;

            // Act
            var result = await _service.ChangeRoomStatusAsync(request, userId, userRole);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task ChangeRoomStatusAsync_ShouldReturnBadRequest_WhenInvalidStatusId()
        {
            // Arrange
            var request = new ChangeRoomStatusRequest { RoomId = 1, NewStatusId = 999 };
            string userRole = "Admin";
            int userId = 1;

            // Act
            var result = await _service.ChangeRoomStatusAsync(request, userId, userRole);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.ResponseCode.Should().Be("INVALID_STATUS");
        }

        #endregion

        #region MarkRoomAsCleaningAsync Tests (Housekeeper - Start Cleaning)

        [Fact]
        public async Task MarkRoomAsCleaningAsync_ShouldChangeStatus_WhenRoomIsAvailable()
        {
            // Arrange
            int roomId = 1; // Available
            int userId = 1;

            // Act
            var result = await _service.MarkRoomAsCleaningAsync(roomId, userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
            room!.StatusId.Should().Be(4); // Cleaning
        }

        [Fact]
        public async Task MarkRoomAsCleaningAsync_ShouldReturnNotFound_WhenRoomDoesNotExist()
        {
            // Arrange
            int roomId = 999;
            int userId = 1;

            // Act
            var result = await _service.MarkRoomAsCleaningAsync(roomId, userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        #endregion

        #region MarkRoomAsCleanedAsync Tests (Housekeeper - Complete Cleaning)

        [Fact]
        public async Task MarkRoomAsCleanedAsync_ShouldChangeToAvailable_WhenRoomIsCleaning()
        {
            // Arrange
            int roomId = 5; // Currently Cleaning
            int userId = 1;

            // Act
            var result = await _service.MarkRoomAsCleanedAsync(roomId, userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
            room!.StatusId.Should().Be(1); // Available
        }

        [Fact]
        public async Task MarkRoomAsCleanedAsync_ShouldReturnNotFound_WhenRoomDoesNotExist()
        {
            // Arrange
            int roomId = 999;
            int userId = 1;

            // Act
            var result = await _service.MarkRoomAsCleanedAsync(roomId, userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        #endregion

        #region MarkRoomForMaintenanceAsync Tests (Technician - Start Maintenance)

        [Fact]
        public async Task MarkRoomForMaintenanceAsync_ShouldChangeStatus()
        {
            // Arrange
            int roomId = 1;
            int userId = 1;
            string reason = "Air conditioner repair";

            // Act
            var result = await _service.MarkRoomForMaintenanceAsync(roomId, userId, reason);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
            room!.StatusId.Should().Be(5); // Maintenance
        }

        [Fact]
        public async Task MarkRoomForMaintenanceAsync_ShouldReturnNotFound_WhenRoomDoesNotExist()
        {
            // Arrange
            int roomId = 999;
            int userId = 1;

            // Act
            var result = await _service.MarkRoomForMaintenanceAsync(roomId, userId, "Test");

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        #endregion

        #region CompleteMaintenanceAsync Tests (Technician - Complete Maintenance)

        [Fact]
        public async Task CompleteMaintenanceAsync_ShouldChangeToAvailable()
        {
            // Arrange - First set room to maintenance
            var room = await _unitOfWork.Rooms.GetByIdAsync(1);
            room!.StatusId = 5; // Maintenance
            await _unitOfWork.Rooms.UpdateAsync(room);
            await _unitOfWork.SaveChangesAsync();

            int userId = 1;

            // Act
            var result = await _service.CompleteMaintenanceAsync(1, userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            var updatedRoom = await _unitOfWork.Rooms.GetByIdAsync(1);
            updatedRoom!.StatusId.Should().Be(1); // Available
        }

        [Fact]
        public async Task CompleteMaintenanceAsync_ShouldReturnNotFound_WhenRoomDoesNotExist()
        {
            // Arrange
            int roomId = 999;
            int userId = 1;

            // Act
            var result = await _service.CompleteMaintenanceAsync(roomId, userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        #endregion

        #region BulkChangeRoomStatusAsync Tests (Manager only)

        [Fact]
        public async Task BulkChangeRoomStatusAsync_ShouldChangeMultipleRooms()
        {
            // Arrange
            var request = new BulkChangeRoomStatusRequest
            {
                RoomIds = new List<int> { 1, 4 },
                NewStatusId = 4, // Cleaning
                Reason = "Bulk cleaning after event"
            };
            int userId = 1;

            // Act
            var result = await _service.BulkChangeRoomStatusAsync(request, userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            // Verify both rooms changed
            var room1 = await _unitOfWork.Rooms.GetByIdAsync(1);
            var room4 = await _unitOfWork.Rooms.GetByIdAsync(4);
            room1!.StatusId.Should().Be(4); // Cleaning
            room4!.StatusId.Should().Be(4); // Cleaning
        }

        [Fact]
        public async Task BulkChangeRoomStatusAsync_ShouldReturnBadRequest_WhenInvalidStatusId()
        {
            // Arrange
            var request = new BulkChangeRoomStatusRequest
            {
                RoomIds = new List<int> { 1, 2 },
                NewStatusId = 999 // Invalid ID
            };
            int userId = 1;

            // Act
            var result = await _service.BulkChangeRoomStatusAsync(request, userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.ResponseCode.Should().Be("INVALID_STATUS");
        }

        [Fact]
        public async Task BulkChangeRoomStatusAsync_ShouldReturnBadRequest_WhenEmptyRoomIds()
        {
            // Arrange
            var request = new BulkChangeRoomStatusRequest
            {
                RoomIds = new List<int>(),
                NewStatusId = 4
            };
            int userId = 1;

            // Act
            var result = await _service.BulkChangeRoomStatusAsync(request, userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
        }

        #endregion

        #region GetAvailableStatusTransitionsAsync Tests

        [Fact]
        public async Task GetAvailableStatusTransitionsAsync_Admin_ShouldReturnAllTransitions()
        {
            // Arrange
            int roomId = 1; // Available
            string userRole = "Admin";

            // Act
            var result = await _service.GetAvailableStatusTransitionsAsync(roomId, userRole);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNull();
            // Admin should see all statuses except current one (6 statuses available)
        }

        [Fact]
        public async Task GetAvailableStatusTransitionsAsync_Receptionist_ShouldReturnLimitedTransitions()
        {
            // Arrange
            int roomId = 1; // Available
            string userRole = "Receptionist";

            // Act
            var result = await _service.GetAvailableStatusTransitionsAsync(roomId, userRole);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNull();
            // Receptionist from Available should only see: Booked, Occupied
        }

        [Fact]
        public async Task GetAvailableStatusTransitionsAsync_ShouldReturnNotFound_WhenRoomDoesNotExist()
        {
            // Arrange
            int roomId = 999;
            string userRole = "Admin";

            // Act
            var result = await _service.GetAvailableStatusTransitionsAsync(roomId, userRole);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        #endregion

        #region Status Transition Workflow Tests

        [Fact]
        public async Task CompleteBookingWorkflow_ShouldSucceed()
        {
            // This test simulates a complete booking workflow:
            // Available → Booked → Occupied → Available → Cleaning → Available

            int roomId = 1;
            int userId = 1;

            // Step 1: Receptionist books the room (Available → Booked)
            var step1 = await _service.ChangeRoomStatusAsync(
                new ChangeRoomStatusRequest { RoomId = roomId, NewStatusId = 2 }, // Booked
                userId, "Receptionist");
            step1.IsSuccess.Should().BeTrue();

            // Step 2: Receptionist checks in guest (Booked → Occupied)
            var step2 = await _service.ChangeRoomStatusAsync(
                new ChangeRoomStatusRequest { RoomId = roomId, NewStatusId = 3 }, // Occupied
                userId, "Receptionist");
            step2.IsSuccess.Should().BeTrue();

            // Step 3: Receptionist checks out guest (Occupied → Available)
            var step3 = await _service.ChangeRoomStatusAsync(
                new ChangeRoomStatusRequest { RoomId = roomId, NewStatusId = 1 }, // Available
                userId, "Receptionist");
            step3.IsSuccess.Should().BeTrue();

            // Step 4: Housekeeper starts cleaning (Available → Cleaning)
            var step4 = await _service.MarkRoomAsCleaningAsync(roomId, userId);
            step4.IsSuccess.Should().BeTrue();

            // Step 5: Housekeeper completes cleaning (Cleaning → Available)
            var step5 = await _service.MarkRoomAsCleanedAsync(roomId, userId);
            step5.IsSuccess.Should().BeTrue();

            // Final verification
            var finalRoom = await _unitOfWork.Rooms.GetByIdAsync(roomId);
            finalRoom!.StatusId.Should().Be(1); // Back to Available
        }

        [Fact]
        public async Task MaintenanceWorkflow_ShouldSucceed()
        {
            // This test simulates a maintenance workflow:
            // Available → Maintenance → Available → Cleaning → Available

            int roomId = 4;
            int userId = 1;

            // Step 1: Technician marks for maintenance (Available → Maintenance)
            var step1 = await _service.MarkRoomForMaintenanceAsync(roomId, userId, "Fix AC");
            step1.IsSuccess.Should().BeTrue();

            // Step 2: Technician completes maintenance (Maintenance → Available)
            var step2 = await _service.CompleteMaintenanceAsync(roomId, userId);
            step2.IsSuccess.Should().BeTrue();

            // Step 3: Housekeeper cleans after maintenance (Available → Cleaning)
            var step3 = await _service.MarkRoomAsCleaningAsync(roomId, userId);
            step3.IsSuccess.Should().BeTrue();

            // Step 4: Housekeeper completes cleaning (Cleaning → Available)
            var step4 = await _service.MarkRoomAsCleanedAsync(roomId, userId);
            step4.IsSuccess.Should().BeTrue();

            // Final verification
            var finalRoom = await _unitOfWork.Rooms.GetByIdAsync(roomId);
            finalRoom!.StatusId.Should().Be(1); // Back to Available
        }

        #endregion
    }
}

