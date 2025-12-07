namespace AppBackend.Services.ApiModels.BookingModel
{
    /// <summary>
    /// Request để Admin verify thanh toán online của User/Guest
    /// Sau khi user bấm "Tôi đã thanh toán", admin cần verify lại
    /// </summary>
    public class VerifyPaymentRequest
    {
        /// <summary>
        /// Có chấp nhận thanh toán không
        /// </summary>
        public bool IsApproved { get; set; }

        /// <summary>
        /// Mã tham chiếu giao dịch (từ ngân hàng)
        /// </summary>
        public string? TransactionReference { get; set; }

        /// <summary>
        /// URL ảnh chứng từ thanh toán (screenshot)
        /// </summary>
        public string? PaymentProofUrl { get; set; }

        /// <summary>
        /// Ghi chú của admin
        /// </summary>
        public string? Note { get; set; }
    }

    /// <summary>
    /// Request để user/guest thông báo đã thanh toán (trigger verify)
    /// </summary>
    public class NotifyPaymentRequest
    {
        public int BookingId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        
        /// <summary>
        /// URL ảnh chứng từ thanh toán (screenshot) - optional
        /// </summary>
        public string? PaymentProofUrl { get; set; }
        
        /// <summary>
        /// Ghi chú từ khách hàng
        /// </summary>
        public string? Note { get; set; }
    }

    /// <summary>
    /// Response danh sách booking cần verify
    /// </summary>
    public class PendingVerificationListDto
    {
        public int BookingId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public decimal DepositAmount { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string? PaymentProofUrl { get; set; }
        public DateTime NotifiedAt { get; set; }
        public string? CustomerNote { get; set; }
    }

    public class ConfirmBookingByQRRequest
    {
        public string Token { get; set; } = string.Empty;
    }

    public class PendingVerificationBookingDto
    {
        public int BookingId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? NotifiedAt { get; set; }
    }
}
