using AppBackend.Repositories.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AppBackend.Services.BackgroundJobs;

/// <summary>
/// Delayed job để check và cancel booking sau 15 phút nếu vẫn Pending
/// </summary>
public class BookingTimeoutChecker
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BookingTimeoutChecker> _logger;

    public BookingTimeoutChecker(
        IServiceProvider serviceProvider,
        ILogger<BookingTimeoutChecker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Check và cancel booking nếu vẫn Pending sau 15 phút
    /// </summary>
    /// <param name="bookingId">ID của booking cần check</param>
    public async Task CheckAndCancelIfPendingAsync(int bookingId)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            _logger.LogInformation($"[BookingTimeout] Checking booking #{bookingId}");

            // 1. Lấy booking
            var booking = await unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null)
            {
                _logger.LogWarning($"[BookingTimeout] Booking #{bookingId} not found");
                return;
            }

            // 2. Lấy status codes
            var pendingStatus = (await unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "BookingStatus" && c.CodeName == "Pending")).FirstOrDefault();
            
            var cancelledStatus = (await unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "BookingStatus" && c.CodeName == "Cancelled")).FirstOrDefault();

            if (pendingStatus == null || cancelledStatus == null)
            {
                _logger.LogError($"[BookingTimeout] Missing status codes for booking #{bookingId}");
                return;
            }

            // 3. Check nếu vẫn Pending thì cancel
            if (booking.StatusId == pendingStatus.CodeId)
            {
                _logger.LogInformation($"[BookingTimeout] Booking #{bookingId} is still Pending. Cancelling...");

                booking.StatusId = cancelledStatus.CodeId;
                booking.UpdatedAt = DateTime.UtcNow;
                booking.SpecialRequests = $"Tự động hủy do không thanh toán trong 15 phút (Cancelled at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss})";
                
                await unitOfWork.Bookings.UpdateAsync(booking);
                await unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"[BookingTimeout] Successfully cancelled booking #{bookingId}");
            }
            else
            {
                // Booking đã được thanh toán hoặc đã cancel trước đó
                var currentStatus = await unitOfWork.CommonCodes.GetByIdAsync(booking.StatusId ?? 0);
                _logger.LogInformation($"[BookingTimeout] Booking #{bookingId} status: {currentStatus?.CodeValue ?? "Unknown"}. No action needed.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[BookingTimeout] Error checking booking #{bookingId}");
        }
    }

    /// <summary>
    /// Schedule delayed job để check booking sau một khoảng thời gian
    /// </summary>
    /// <param name="bookingId">ID của booking</param>
    /// <param name="delayMinutes">Số phút delay (mặc định 15)</param>
    public void ScheduleTimeoutCheck(int bookingId, int delayMinutes = 15)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation($"[BookingTimeout] Scheduled timeout check for booking #{bookingId} in {delayMinutes} minutes");
                
                // Delay
                await Task.Delay(TimeSpan.FromMinutes(delayMinutes));
                
                // Check và cancel nếu cần
                await CheckAndCancelIfPendingAsync(bookingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[BookingTimeout] Error in scheduled job for booking #{bookingId}");
            }
        });
    }
}

