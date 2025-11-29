#pragma warning disable SKEXP0001 // Type is for evaluation purposes only

using System.ComponentModel;
using System.Text.Json;
using AppBackend.Services.ApiModels.RoomModel;
using AppBackend.Services.Services.RoomServices;
using Microsoft.SemanticKernel;

namespace AppBackend.Services.Services.AI;

/// <summary>
/// Semantic Kernel Plugin for Hotel Booking operations
/// AI can call these functions to search rooms, get details, etc.
/// </summary>
public class HotelBookingPlugin
{
    private readonly IRoomService _roomService;

    public HotelBookingPlugin(IRoomService roomService)
    {
        _roomService = roomService;
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
            var request = new SearchRoomTypeRequest
            {
                CheckInDate = DateTime.Parse(checkInDate),
                CheckOutDate = DateTime.Parse(checkOutDate),
                NumberOfGuests = guestCount,
                MinPrice = minPrice,
                MaxPrice = maxPrice
            };

            var result = await _roomService.SearchRoomTypesAsync(request);

            if (result.IsSuccess)
            {
                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = "Found available rooms",
                    data = result.Data
                });
            }

            return JsonSerializer.Serialize(new
            {
                success = false,
                message = result.Message ?? "No rooms found"
            });
        }
        catch (Exception ex)
        {
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
            DateTime? checkIn = string.IsNullOrEmpty(checkInDate) ? null : DateTime.Parse(checkInDate);
            DateTime? checkOut = string.IsNullOrEmpty(checkOutDate) ? null : DateTime.Parse(checkOutDate);

            var result = await _roomService.GetRoomTypeDetailForCustomerAsync(roomTypeId, checkIn, checkOut);

            if (result.IsSuccess)
            {
                return JsonSerializer.Serialize(new
                {
                    success = true,
                    data = result.Data
                });
            }

            return JsonSerializer.Serialize(new
            {
                success = false,
                message = result.Message ?? "Room not found"
            });
        }
        catch (Exception ex)
        {
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
        return JsonSerializer.Serialize(new
        {
            currentDate = DateTime.Now.ToString("yyyy-MM-dd"),
            currentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            dayOfWeek = DateTime.Now.DayOfWeek.ToString()
        });
    }
}

#pragma warning restore SKEXP0001
