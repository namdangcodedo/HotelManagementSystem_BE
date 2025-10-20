using System.Threading.Channels;

namespace AppBackend.Services.MessageQueue
{
    public class BookingQueueService : IBookingQueueService
    {
        private readonly Channel<BookingQueueMessage> _queue;

        public BookingQueueService()
        {
            var options = new BoundedChannelOptions(1000) // Max 1000 messages
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<BookingQueueMessage>(options);
        }

        public async Task EnqueueAsync(BookingQueueMessage message)
        {
            await _queue.Writer.WriteAsync(message);
        }

        public async Task<BookingQueueMessage?> DequeueAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await _queue.Reader.ReadAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        public int GetQueueCount()
        {
            return _queue.Reader.Count;
        }
    }
}

