#pragma warning disable SKEXP0001 // Type is for evaluation purposes only

using System.ComponentModel;
using System.Text.Json;
using AppBackend.Services.ApiModels.RoomModel;
using AppBackend.Services.Services.RoomServices;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;

namespace AppBackend.Services.Services.AI;

/// <summary>
/// Semantic Kernel Plugin for Hotel Booking operations
/// AI can call these functions to search rooms, get details, etc.
/// </summary>
public class HotelBookingPlugin
{
    private readonly IRoomService _roomService;
    private readonly ILogger<HotelBookingPlugin> _logger;

    public HotelBookingPlugin(IRoomService roomService, ILogger<HotelBookingPlugin> logger)
    {
        _roomService = roomService;
        _logger = logger;
    }

    [KernelFunction("search_available_rooms")]
    [Description("Search for available hotel rooms based on dates, location, guest count, and price range")]
    public async Task<string> SearchAvailableRoomsAsync(
        [Description("Check-in date in format YYYY-MM-DD")] string checkInDate,
        [Description("Check-out date in format YYYY-MM-DD")] string checkOutDate,
        [Description("Location or city name")] string? location = null,
        [Description("Number of guests")] int? guestCount = null,
        [Description("Minimum price")] decimal? minPrice = null,
        [Description("Maximum price")] decimal? maxPrice = null)
    {
        try
        {
            _logger.LogInformation("🔧 FUNCTION CALLED: search_available_rooms");
            _logger.LogInformation("  CheckIn: {CheckIn}, CheckOut: {CheckOut}", checkInDate, checkOutDate);
            _logger.LogInformation("  Location: {Location}, Guests: {Guests}, PriceRange: {Min}-{Max}", 
                location ?? "N/A", guestCount?.ToString() ?? "N/A", minPrice, maxPrice);

            var request = new SearchRoomTypeRequest
            {
                CheckInDate = DateTime.Parse(checkInDate),
                CheckOutDate = DateTime.Parse(checkOutDate),
                NumberOfGuests = guestCount,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                PageIndex = 1,
                PageSize = 10  // ← GIỚI HẠN CHỈ LẤY 10 PHÒNG ĐẦU TIÊN
            };

            var result = await _roomService.SearchRoomTypesAsync(request);

            if (result.IsSuccess && result.Data != null)
            {
                // Lấy dữ liệu và giới hạn số lượng phòng trả về
                var pagedData = result.Data as dynamic;
                
                _logger.LogInformation("✅ Function returned successfully");
                    
                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = "Found available rooms (showing top 10 results)",
                    totalCount = pagedData?.TotalCount ?? 0,
                    showingCount = pagedData?.Items?.Count ?? 0,
                    data = new
                    {
                        rooms = pagedData?.Items,
                        totalCount = pagedData?.TotalCount ?? 0
                    }
                });
            }

            _logger.LogWarning("⚠️ Function found no rooms: {Message}", result.Message);
            return JsonSerializer.Serialize(new
            {
                success = false,
                message = result.Message ?? "No rooms found"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in search_available_rooms function");
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Error searching rooms: {ex.Message}"
            });
        }
    }

    [KernelFunction("get_room_details")]
    [Description("Get detailed information about a specific room type including amenities, pricing, and availability")]
    public async Task<string> GetRoomDetailsAsync(
        [Description("The room type ID")] int roomTypeId,
        [Description("Optional check-in date for availability check")] string? checkInDate = null,
        [Description("Optional check-out date for availability check")] string? checkOutDate = null)
    {
        try
        {
            _logger.LogInformation("🔧 FUNCTION CALLED: get_room_details");
            _logger.LogInformation("  RoomTypeId: {RoomTypeId}, CheckIn: {CheckIn}, CheckOut: {CheckOut}", 
                roomTypeId, checkInDate ?? "N/A", checkOutDate ?? "N/A");

            DateTime? checkIn = string.IsNullOrEmpty(checkInDate) ? null : DateTime.Parse(checkInDate);
            DateTime? checkOut = string.IsNullOrEmpty(checkOutDate) ? null : DateTime.Parse(checkOutDate);

            var result = await _roomService.GetRoomTypeDetailForCustomerAsync(roomTypeId, checkIn, checkOut);

            if (result.IsSuccess)
            {
                _logger.LogInformation("✅ Function returned room details successfully");
                return JsonSerializer.Serialize(new
                {
                    success = true,
                    data = result.Data
                });
            }

            _logger.LogWarning("⚠️ Function failed: {Message}", result.Message);
            return JsonSerializer.Serialize(new
            {
                success = false,
                message = result.Message ?? "Room not found"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in get_room_details function");
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Error getting room details: {ex.Message}"
            });
        }
    }

    [KernelFunction("get_current_date")]
    [Description("Get the current date and time - useful for checking availability")]
    public string GetCurrentDate()
    {
        _logger.LogInformation("🔧 FUNCTION CALLED: get_current_date");
        
        var result = JsonSerializer.Serialize(new
        {
            currentDate = DateTime.Now.ToString("yyyy-MM-dd"),
            currentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            dayOfWeek = DateTime.Now.DayOfWeek.ToString()
        });
        
        _logger.LogInformation("✅ Returning: {Result}", result);
        return result;
    }

    [KernelFunction("search_room_type_statistics")]
    [Description("Search and get statistics about room types: overview, most booked, by price range, by occupancy, or booking statistics")]
    public async Task<string> SearchRoomTypeStatisticsAsync(
        [Description("Type of statistics: 'overview' (general stats), 'most_booked' (top booked room types), 'by_price' (filter by price), 'by_occupancy' (filter by guest count), 'booking_stats' (booking statistics by date)")] 
        string statisticType = "overview",
        [Description("Number of top items to return (for most_booked)")] 
        int topCount = 10,
        [Description("Minimum price (for by_price)")] 
        decimal? minPrice = null,
        [Description("Maximum price (for by_price)")] 
        decimal? maxPrice = null,
        [Description("Minimum occupancy/guests (for by_occupancy)")] 
        int? minOccupancy = null,
        [Description("Maximum occupancy/guests (for by_occupancy)")] 
        int? maxOccupancy = null,
        [Description("Start date for filtering (for booking_stats) in format YYYY-MM-DD")] 
        string? fromDate = null,
        [Description("End date for filtering (for booking_stats) in format YYYY-MM-DD")] 
        string? toDate = null)
    {
        try
        {
            _logger.LogInformation("🔧 FUNCTION CALLED: search_room_type_statistics");
            _logger.LogInformation("  StatisticType: {Type}, TopCount: {Top}", statisticType, topCount);
            _logger.LogInformation("  Price: {Min}-{Max}, Occupancy: {MinOcc}-{MaxOcc}", 
                minPrice, maxPrice, minOccupancy, maxOccupancy);
            _logger.LogInformation("  DateRange: {From} to {To}", fromDate ?? "N/A", toDate ?? "N/A");

            var request = new RoomTypeStatisticsRequest
            {
                StatisticType = statisticType,
                TopCount = topCount,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                MinOccupancy = minOccupancy,
                MaxOccupancy = maxOccupancy,
                FromDate = string.IsNullOrEmpty(fromDate) ? null : DateTime.Parse(fromDate),
                ToDate = string.IsNullOrEmpty(toDate) ? null : DateTime.Parse(toDate),
                OnlyActive = true
            };

            var result = await _roomService.SearchRoomTypeStatisticsAsync(request);

            if (result.IsSuccess)
            {
                _logger.LogInformation("✅ Function returned statistics successfully");
                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = result.Message,
                    data = result.Data
                });
            }

            _logger.LogWarning("⚠️ Function failed: {Message}", result.Message);
            return JsonSerializer.Serialize(new
            {
                success = false,
                message = result.Message ?? "Failed to get statistics"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in search_room_type_statistics function");
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Error getting room type statistics: {ex.Message}"
            });
        }
    }
}

#pragma warning restore SKEXP0001
