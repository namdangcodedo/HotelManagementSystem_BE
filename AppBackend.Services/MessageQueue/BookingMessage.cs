namespace AppBackend.Services.MessageQueue
{
    public class BookingMessage
    {
        public int BookingId { get; set; }
        public int CustomerId { get; set; }
        public List<int> RoomIds { get; set; } = new List<int>();
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string BookingType { get; set; } = "Online"; // Online, Walkin
        public DateTime CreatedAt { get; set; }
        public string LockId { get; set; } = string.Empty; // Unique lock ID
    }

    public enum BookingMessageType
    {
        CreateBooking,
        ConfirmPayment,
        CancelBooking,
        ReleaseRoomLock
    }

    public class BookingQueueMessage
    {
        public BookingMessageType MessageType { get; set; }
        public BookingMessage Data { get; set; } = null!;
        public int RetryCount { get; set; } = 0;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}

