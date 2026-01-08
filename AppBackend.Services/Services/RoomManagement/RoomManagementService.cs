using AppBackend.BusinessObjects.Dtos;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.RoomManagement;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace AppBackend.Services.Services.RoomManagement
{
    public class RoomManagementService : IRoomManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RoomManagementService> _logger;
        private readonly IMapper _mapper;

        public RoomManagementService(IUnitOfWork unitOfWork, ILogger<RoomManagementService> logger, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<ResultModel> SearchRoomsAsync(SearchRoomsRequest request)
        {
            try
            {
                var query = await _unitOfWork.Rooms.GetAllAsync();
                var rooms = query.AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(request.RoomName))
                {
                    rooms = rooms.Where(r => r.RoomName.Contains(request.RoomName));
                }

                if (request.RoomTypeId.HasValue)
                {
                    rooms = rooms.Where(r => r.RoomTypeId == request.RoomTypeId.Value);
                }

                // Filter by StatusId (preferred method)
                if (request.StatusId.HasValue)
                {
                    rooms = rooms.Where(r => r.StatusId == request.StatusId.Value);
                }
                // Filter by Status string (deprecated - for backward compatibility)
                else if (!string.IsNullOrWhiteSpace(request.Status))
                {
                    var statusCode = await _unitOfWork.CommonCodes.FindAsync(c => 
                        c.CodeType == "RoomStatus" && c.CodeName == request.Status);
                    var status = statusCode.FirstOrDefault();
                    if (status != null)
                    {
                        rooms = rooms.Where(r => r.StatusId == status.CodeId);
                    }
                }

                if (request.Floor.HasValue)
                {
                    var floorPrefix = request.Floor.Value.ToString();
                    rooms = rooms.Where(r => r.RoomName.StartsWith(floorPrefix));
                }

                 // Filter by number of guests - only return rooms with maxOccupancy >= numberOfGuests
                if (request.NumberOfGuests.HasValue)
                {
                    rooms = rooms.Where(r => r.RoomType != null && r.RoomType.MaxOccupancy >= request.NumberOfGuests.Value);
                }

                // Filter by price range
                if (request.MinPrice.HasValue)
                {
                    rooms = rooms.Where(r => r.RoomType != null && r.RoomType.BasePriceNight >= request.MinPrice.Value);
                }

                if (request.MaxPrice.HasValue)
                {
                    rooms = rooms.Where(r => r.RoomType != null && r.RoomType.BasePriceNight <= request.MaxPrice.Value);
                }

                var totalRecords = rooms.Count();
                var totalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize);

                var pagedRooms = rooms
                    .OrderBy(r => r.RoomName)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // Load related data
                var roomDtos = new List<RoomDetailDto>();
                foreach (var room in pagedRooms)
                {
                    var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(room.RoomTypeId);
                    var status = await _unitOfWork.CommonCodes.GetByIdAsync(room.StatusId);
                    var media = (await _unitOfWork.Mediums.FindAsync(m =>
                        m.ReferenceTable == "Room" &&
                        m.ReferenceKey == room.RoomId.ToString()))
                        .OrderBy(m => m.DisplayOrder)
                        .ToList();

                    roomDtos.Add(new RoomDetailDto
                    {
                        RoomId = room.RoomId,
                        RoomName = room.RoomName,
                        RoomTypeId = room.RoomTypeId,
                        RoomTypeName = roomType?.TypeName ?? "",
                        RoomTypeCode = roomType?.TypeCode ?? "",
                        BasePriceNight = roomType?.BasePriceNight ?? 0,
                        StatusId = room.StatusId,
                        Status = status?.CodeValue ?? "",
                        StatusCode = status?.CodeName ?? "",
                        Description = room.Description,
                        MaxOccupancy = roomType?.MaxOccupancy ?? 0,
                        RoomSize = roomType?.RoomSize,
                        NumberOfBeds = roomType?.NumberOfBeds,
                        BedType = roomType?.BedType,
                        Images = _mapper.Map<List<MediumDto>>(media),
                        CreatedAt = room.CreatedAt,
                        UpdatedAt = room.UpdatedAt
                    });
                }

                var response = new RoomListResponse
                {
                    Rooms = roomDtos,
                    TotalRecords = totalRecords,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = totalPages
                };

                return new ResultModel
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Message = "Lấy danh sách phòng thành công",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching rooms");
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = $"Lỗi khi tìm kiếm phòng: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> GetRoomMapAsync(int? floor = null)
        {
            try
            {
                var allRooms = await _unitOfWork.Rooms.GetAllAsync();
                var rooms = allRooms.ToList();

                if (floor.HasValue)
                {
                    var floorPrefix = floor.Value.ToString();
                    rooms = rooms.Where(r => r.RoomName.StartsWith(floorPrefix)).ToList();
                }

                var roomMapList = new List<RoomMapDto>();
                var statusSummary = new Dictionary<string, int>();

                foreach (var room in rooms)
                {
                    var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(room.RoomTypeId);
                    var status = await _unitOfWork.CommonCodes.GetByIdAsync(room.StatusId);

                    var floorNumber = int.Parse(room.RoomName.Substring(0, 1));

                    var roomMap = new RoomMapDto
                    {
                        RoomId = room.RoomId,
                        RoomName = room.RoomName,
                        RoomTypeCode = roomType?.TypeCode ?? "",
                        RoomTypeName = roomType?.TypeName ?? "",
                        Status = status?.CodeValue ?? "",
                        StatusCode = status?.CodeName ?? "",
                        Floor = floorNumber,
                        BasePriceNight = roomType?.BasePriceNight ?? 0
                    };

                    roomMapList.Add(roomMap);

                    // Count status
                    var statusKey = status?.CodeValue ?? "Unknown";
                    if (!statusSummary.ContainsKey(statusKey))
                    {
                        statusSummary[statusKey] = 0;
                    }
                    statusSummary[statusKey]++;
                }

                // Group by floor if not specified
                if (!floor.HasValue)
                {
                    var groupedByFloor = roomMapList
                        .GroupBy(r => r.Floor)
                        .Select(g => new RoomMapResponse
                        {
                            Floor = g.Key,
                            Rooms = g.OrderBy(r => r.RoomName).ToList(),
                            StatusSummary = statusSummary
                        })
                        .OrderBy(r => r.Floor)
                        .ToList();

                    return new ResultModel
                    {
                        IsSuccess = true,
                        StatusCode = 200,
                        Message = "Lấy sơ đồ phòng thành công",
                        Data = groupedByFloor
                    };
                }
                else
                {
                    var response = new RoomMapResponse
                    {
                        Floor = floor.Value,
                        Rooms = roomMapList.OrderBy(r => r.RoomName).ToList(),
                        StatusSummary = statusSummary
                    };

                    return new ResultModel
                    {
                        IsSuccess = true,
                        StatusCode = 200,
                        Message = $"Lấy sơ đồ tầng {floor.Value} thành công",
                        Data = response
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting room map");
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = $"Lỗi khi lấy sơ đồ phòng: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> GetRoomDetailAsync(int roomId)
        {
            try
            {
                var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
                if (room == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        ResponseCode = "NOT_FOUND",
                        Message = "Không tìm thấy phòng"
                    };
                }

                var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(room.RoomTypeId);
                var status = await _unitOfWork.CommonCodes.GetByIdAsync(room.StatusId);
                var media = (await _unitOfWork.Mediums.FindAsync(m =>
                    m.ReferenceTable == "Room" &&
                    m.ReferenceKey == room.RoomId.ToString()))
                    .OrderBy(m => m.DisplayOrder)
                    .ToList();

                var roomDetail = new RoomDetailDto
                {
                    RoomId = room.RoomId,
                    RoomName = room.RoomName,
                    RoomTypeId = room.RoomTypeId,
                    RoomTypeName = roomType?.TypeName ?? "",
                    RoomTypeCode = roomType?.TypeCode ?? "",
                    BasePriceNight = roomType?.BasePriceNight ?? 0,
                    StatusId = room.StatusId,
                    Status = status?.CodeValue ?? "",
                    StatusCode = status?.CodeName ?? "",
                    Description = room.Description,
                    MaxOccupancy = roomType?.MaxOccupancy ?? 0,
                    RoomSize = roomType?.RoomSize,
                    NumberOfBeds = roomType?.NumberOfBeds,
                    BedType = roomType?.BedType,
                    Images = _mapper.Map<List<MediumDto>>(media),
                    CreatedAt = room.CreatedAt,
                    UpdatedAt = room.UpdatedAt
                };

                return new ResultModel
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Message = "Lấy thông tin phòng thành công",
                    Data = roomDetail
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting room detail for RoomId: {roomId}");
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = $"Lỗi khi lấy thông tin phòng: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> ChangeRoomStatusAsync(ChangeRoomStatusRequest request, int userId, string userRole)
        {
            try
            {
                var room = await _unitOfWork.Rooms.GetByIdAsync(request.RoomId);
                if (room == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        ResponseCode = "NOT_FOUND",
                        Message = "Không tìm thấy phòng"
                    };
                }

                // Validate new status exists and is a RoomStatus
                var newStatus = await _unitOfWork.CommonCodes.GetByIdAsync(request.NewStatusId);
                if (newStatus == null || newStatus.CodeType != "RoomStatus")
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        ResponseCode = "INVALID_STATUS",
                        Message = "Trạng thái không hợp lệ hoặc không phải là RoomStatus"
                    };
                }

                // Validate status transition based on role
                var canChange = await ValidateStatusTransition(room.StatusId, request.NewStatusId, userRole);
                if (!canChange)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = 403,
                        ResponseCode = "FORBIDDEN",
                        Message = $"Role {userRole} không có quyền chuyển đổi trạng thái này"
                    };
                }

                // Update room status
                var oldStatusId = room.StatusId;
                room.StatusId = request.NewStatusId;
                room.UpdatedAt = DateTime.UtcNow;
                room.UpdatedBy = userId;

                await _unitOfWork.Rooms.UpdateAsync(room);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Room {room.RoomName} status changed from {oldStatusId} to {request.NewStatusId} by user {userId} ({userRole})");

                return new ResultModel
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Message = $"Đã chuyển trạng thái phòng {room.RoomName} thành '{newStatus.CodeValue}'"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error changing room status for RoomId: {request.RoomId}");
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = $"Lỗi khi thay đổi trạng thái phòng: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> BulkChangeRoomStatusAsync(BulkChangeRoomStatusRequest request, int userId)
        {
            try
            {
                // Validate RoomIds list is not empty
                if (request.RoomIds == null || !request.RoomIds.Any())
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        ResponseCode = "INVALID_REQUEST",
                        Message = "Danh sách phòng không được trống"
                    };
                }

                // Validate new status exists and is a RoomStatus
                var newStatus = await _unitOfWork.CommonCodes.GetByIdAsync(request.NewStatusId);
                if (newStatus == null || newStatus.CodeType != "RoomStatus")
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        ResponseCode = "INVALID_STATUS",
                        Message = "Trạng thái không hợp lệ hoặc không phải là RoomStatus"
                    };
                }

                var successCount = 0;
                var failedRooms = new List<string>();

                foreach (var roomId in request.RoomIds)
                {
                    var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
                    if (room == null)
                    {
                        failedRooms.Add($"Room {roomId} không tồn tại");
                        continue;
                    }

                    room.StatusId = request.NewStatusId;
                    room.UpdatedAt = DateTime.UtcNow;
                    room.UpdatedBy = userId;

                    await _unitOfWork.Rooms.UpdateAsync(room);
                    successCount++;
                }

                await _unitOfWork.SaveChangesAsync();

                var message = $"Đã cập nhật trạng thái {successCount}/{request.RoomIds.Count} phòng";
                if (failedRooms.Any())
                {
                    message += $". Thất bại: {string.Join(", ", failedRooms)}";
                }

                return new ResultModel
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Message = message,
                    Data = new { SuccessCount = successCount, FailedCount = failedRooms.Count }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk changing room status");
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = $"Lỗi khi thay đổi trạng thái nhiều phòng: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> MarkRoomAsCleaningAsync(int roomId, int housekeeperId)
        {
            try
            {
                var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
                if (room == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        ResponseCode = "NOT_FOUND",
                        Message = "Không tìm thấy phòng"
                    };
                }

                var cleaningStatus = (await _unitOfWork.CommonCodes.FindAsync(c => 
                    c.CodeType == "RoomStatus" && c.CodeName == "Cleaning"))
                    .FirstOrDefault();

                if (cleaningStatus == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Message = "Không tìm thấy trạng thái 'Cleaning'"
                    };
                }

                room.StatusId = cleaningStatus.CodeId;
                room.UpdatedAt = DateTime.UtcNow;
                room.UpdatedBy = housekeeperId;

                await _unitOfWork.Rooms.UpdateAsync(room);
                await _unitOfWork.SaveChangesAsync();

                return new ResultModel
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Message = $"Phòng {room.RoomName} đã được đánh dấu đang dọn dẹp"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking room as cleaning for RoomId: {roomId}");
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> MarkRoomAsCleanedAsync(int roomId, int housekeeperId)
        {
            try
            {
                var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
                if (room == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        ResponseCode = "NOT_FOUND",
                        Message = "Không tìm thấy phòng"
                    };
                }

                var availableStatus = (await _unitOfWork.CommonCodes.FindAsync(c => 
                    c.CodeType == "RoomStatus" && c.CodeName == "Available"))
                    .FirstOrDefault();

                if (availableStatus == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Message = "Không tìm thấy trạng thái 'Available'"
                    };
                }

                room.StatusId = availableStatus.CodeId;
                room.UpdatedAt = DateTime.UtcNow;
                room.UpdatedBy = housekeeperId;

                await _unitOfWork.Rooms.UpdateAsync(room);
                await _unitOfWork.SaveChangesAsync();

                return new ResultModel
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Message = $"Phòng {room.RoomName} đã dọn dẹp xong và sẵn sàng cho thuê"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking room as cleaned for RoomId: {roomId}");
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> MarkRoomForMaintenanceAsync(int roomId, int userId, string reason)
        {
            try
            {
                var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
                if (room == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        ResponseCode = "NOT_FOUND",
                        Message = "Không tìm thấy phòng"
                    };
                }

                var maintenanceStatus = (await _unitOfWork.CommonCodes.FindAsync(c => 
                    c.CodeType == "RoomStatus" && c.CodeName == "Maintenance"))
                    .FirstOrDefault();

                if (maintenanceStatus == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Message = "Không tìm thấy trạng thái 'Maintenance'"
                    };
                }

                room.StatusId = maintenanceStatus.CodeId;
                room.UpdatedAt = DateTime.UtcNow;
                room.UpdatedBy = userId;

                await _unitOfWork.Rooms.UpdateAsync(room);
                await _unitOfWork.SaveChangesAsync();

                return new ResultModel
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Message = $"Phòng {room.RoomName} đã được chuyển sang trạng thái bảo trì"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking room for maintenance for RoomId: {roomId}");
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> CompleteMaintenanceAsync(int roomId, int userId)
        {
            try
            {
                var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
                if (room == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        ResponseCode = "NOT_FOUND",
                        Message = "Không tìm thấy phòng"
                    };
                }

                var availableStatus = (await _unitOfWork.CommonCodes.FindAsync(c => 
                    c.CodeType == "RoomStatus" && c.CodeName == "Available"))
                    .FirstOrDefault();

                if (availableStatus == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Message = "Không tìm thấy trạng thái 'Available'"
                    };
                }

                room.StatusId = availableStatus.CodeId;
                room.UpdatedAt = DateTime.UtcNow;
                room.UpdatedBy = userId;

                await _unitOfWork.Rooms.UpdateAsync(room);
                await _unitOfWork.SaveChangesAsync();

                return new ResultModel
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Message = $"Phòng {room.RoomName} đã hoàn tất bảo trì và sẵn sàng"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error completing maintenance for RoomId: {roomId}");
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> GetRoomStatusSummaryAsync()
        {
            try
            {
                var allRooms = await _unitOfWork.Rooms.GetAllAsync();
                var totalRooms = allRooms.Count();

                var statusSummary = new List<RoomStatusSummaryDto>();
                var roomStatuses = await _unitOfWork.CommonCodes.FindAsync(c => c.CodeType == "RoomStatus");

                foreach (var status in roomStatuses)
                {
                    var count = allRooms.Count(r => r.StatusId == status.CodeId);
                    var percentage = totalRooms > 0 ? (decimal)count / totalRooms * 100 : 0;

                    statusSummary.Add(new RoomStatusSummaryDto
                    {
                        Status = status.CodeValue,
                        StatusCode = status.CodeName,
                        Count = count,
                        Percentage = Math.Round(percentage, 2)
                    });
                }

                return new ResultModel
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Message = "Lấy thống kê trạng thái phòng thành công",
                    Data = new
                    {
                        TotalRooms = totalRooms,
                        StatusSummary = statusSummary
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting room status summary");
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> GetAvailableStatusTransitionsAsync(int roomId, string userRole)
        {
            try
            {
                var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
                if (room == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        ResponseCode = "NOT_FOUND",
                        Message = "Không tìm thấy phòng"
                    };
                }

                var currentStatus = await _unitOfWork.CommonCodes.GetByIdAsync(room.StatusId);
                var allStatuses = await _unitOfWork.CommonCodes.FindAsync(c => c.CodeType == "RoomStatus");

                var availableTransitions = new List<object>();

                foreach (var status in allStatuses)
                {
                    var canTransition = await ValidateStatusTransition(room.StatusId, status.CodeId, userRole);
                    if (canTransition && status.CodeId != room.StatusId)
                    {
                        availableTransitions.Add(new
                        {
                            StatusCode = status.CodeName,
                            StatusName = status.CodeValue,
                            Description = status.Description
                        });
                    }
                }

                return new ResultModel
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Message = "Lấy danh sách trạng thái có thể chuyển đổi",
                    Data = new
                    {
                        CurrentStatus = new
                        {
                            StatusCode = currentStatus?.CodeName,
                            StatusName = currentStatus?.CodeValue
                        },
                        AvailableTransitions = availableTransitions
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting available status transitions for RoomId: {roomId}");
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }

        private async Task<bool> ValidateStatusTransition(int currentStatusId, int newStatusId, string userRole)
        {
            var currentStatus = await _unitOfWork.CommonCodes.GetByIdAsync(currentStatusId);
            var newStatus = await _unitOfWork.CommonCodes.GetByIdAsync(newStatusId);

            if (currentStatus == null || newStatus == null)
                return false;

            // Admin và Manager có thể chuyển đổi tất cả trạng thái
            if (userRole == "Admin" || userRole == "Manager")
                return true;

            // Receptionist có thể chuyển: Available <-> Booked, Available <-> Occupied
            if (userRole == "Receptionist")
            {
                var allowedTransitions = new[]
                {
                    ("Available", "Booked"),
                    ("Booked", "Available"),
                    ("Available", "Occupied"),
                    ("Occupied", "Available"),
                    ("Booked", "Occupied")
                };

                return allowedTransitions.Any(t => 
                    (currentStatus.CodeName == t.Item1 && newStatus.CodeName == t.Item2));
            }

            // Housekeeper chỉ có thể: Available/Occupied -> Cleaning, Cleaning -> Available
            if (userRole == "Housekeeper")
            {
                var allowedTransitions = new[]
                {
                    ("Available", "Cleaning"),
                    ("Occupied", "Cleaning"),
                    ("Cleaning", "Available"),
                    ("Cleaning", "PendingInspection"),
                    ("PendingInspection", "Available")
                };

                return allowedTransitions.Any(t => 
                    (currentStatus.CodeName == t.Item1 && newStatus.CodeName == t.Item2));
            }

            // Technician chỉ có thể: Any -> Maintenance, Maintenance -> Available
            if (userRole == "Technician")
            {
                return newStatus.CodeName == "Maintenance" || 
                       (currentStatus.CodeName == "Maintenance" && newStatus.CodeName == "Available");
            }

            return false;
        }
    }
}

