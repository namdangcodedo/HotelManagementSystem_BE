using AppBackend.Services.Helpers;
using AppBackend.Repositories.UnitOfWork;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace AppBackend.Services.MessageQueue
{
    public class BookingQueueProcessor : BackgroundService
    {
        private readonly ILogger<BookingQueueProcessor> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IBookingQueueService _queueService;

        public BookingQueueProcessor(
            ILogger<BookingQueueProcessor> logger,
            IServiceProvider serviceProvider,
            IBookingQueueService queueService)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _queueService = queueService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BookingQueueProcessor is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var message = await _queueService.DequeueAsync(stoppingToken);
                    if (message != null)
                    {
                        await ProcessMessageAsync(message, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing booking queue message");
                    await Task.Delay(1000, stoppingToken); // Wait before retry
                }
            }

            _logger.LogInformation("BookingQueueProcessor is stopping.");
        }

        private async Task ProcessMessageAsync(BookingQueueMessage message, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var cacheHelper = scope.ServiceProvider.GetRequiredService<CacheHelper>();

            try
            {
                switch (message.MessageType)
                {
                    case BookingMessageType.CreateBooking:
                        await HandleCreateBookingAsync(message.Data, unitOfWork, cacheHelper);
                        break;

                    case BookingMessageType.ConfirmPayment:
                        await HandleConfirmPaymentAsync(message.Data, unitOfWork, cacheHelper);
                        break;

                    case BookingMessageType.CancelBooking:
                        await HandleCancelBookingAsync(message.Data, unitOfWork, cacheHelper);
                        break;

                    case BookingMessageType.ReleaseRoomLock:
                        await HandleReleaseRoomLockAsync(message.Data, cacheHelper);
                        break;

                    default:
                        _logger.LogWarning($"Unknown message type: {message.MessageType}");
                        break;
                }

                _logger.LogInformation($"Successfully processed {message.MessageType} for BookingId: {message.Data.BookingId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process {message.MessageType} for BookingId: {message.Data.BookingId}");
                
                // Retry logic
                if (message.RetryCount < 3)
                {
                    message.RetryCount++;
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, message.RetryCount)), cancellationToken);
                    await _queueService.EnqueueAsync(message);
                }
            }
        }

        private async Task HandleCreateBookingAsync(BookingMessage data, IUnitOfWork unitOfWork, CacheHelper cacheHelper)
        {
            // Verify room locks are still valid
            foreach (var roomId in data.RoomIds)
            {
                var lockKey = $"{roomId}_{data.CheckInDate:yyyyMMdd}_{data.CheckOutDate:yyyyMMdd}";
                var lockedBy = cacheHelper.Get<string>(CachePrefix.RoomBookingLock, lockKey);
                
                if (lockedBy != data.LockId)
                {
                    _logger.LogWarning($"Room lock expired or invalid for Room {roomId}");
                    throw new InvalidOperationException("Room lock expired. Please try again.");
                }
            }

            // Create booking in database
            // This will be handled by BookingService
        }

        private async Task HandleConfirmPaymentAsync(BookingMessage data, IUnitOfWork unitOfWork, CacheHelper cacheHelper)
        {
            var booking = await unitOfWork.Bookings.GetByIdAsync(data.BookingId);
            if (booking != null)
            {
                // Update booking status to confirmed
                // Release room locks
                foreach (var roomId in data.RoomIds)
                {
                    var lockKey = $"{roomId}_{data.CheckInDate:yyyyMMdd}_{data.CheckOutDate:yyyyMMdd}";
                    cacheHelper.ReleaseLock(CachePrefix.RoomBookingLock, lockKey, data.LockId);
                }

                // Remove payment info from cache
                cacheHelper.Remove(CachePrefix.BookingPayment, data.BookingId.ToString());
            }
        }

        private async Task HandleCancelBookingAsync(BookingMessage data, IUnitOfWork unitOfWork, CacheHelper cacheHelper)
        {
            // Release room locks
            foreach (var roomId in data.RoomIds)
            {
                var lockKey = $"{roomId}_{data.CheckInDate:yyyyMMdd}_{data.CheckOutDate:yyyyMMdd}";
                cacheHelper.ReleaseLock(CachePrefix.RoomBookingLock, lockKey, data.LockId);
            }

            // Remove booking from database if not confirmed
            var booking = await unitOfWork.Bookings.GetByIdAsync(data.BookingId);
            if (booking != null)
            {
                await unitOfWork.Bookings.DeleteAsync(booking);
                await unitOfWork.SaveChangesAsync();
            }

            cacheHelper.Remove(CachePrefix.BookingPayment, data.BookingId.ToString());
        }

        private Task HandleReleaseRoomLockAsync(BookingMessage data, CacheHelper cacheHelper)
        {
            foreach (var roomId in data.RoomIds)
            {
                var lockKey = $"{roomId}_{data.CheckInDate:yyyyMMdd}_{data.CheckOutDate:yyyyMMdd}";
                cacheHelper.ReleaseLock(CachePrefix.RoomBookingLock, lockKey, data.LockId);
            }
            return Task.CompletedTask;
        }
    }
}

