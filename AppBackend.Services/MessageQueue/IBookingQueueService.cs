namespace AppBackend.Services.MessageQueue
{
    public interface IBookingQueueService
    {
        Task EnqueueAsync(BookingQueueMessage message);
        Task<BookingQueueMessage?> DequeueAsync(CancellationToken cancellationToken);
        int GetQueueCount();
    }
}

