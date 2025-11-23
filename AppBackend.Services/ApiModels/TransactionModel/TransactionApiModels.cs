using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.TransactionModel
{
    #region Payment Management DTOs

    /// <summary>
    /// Request to create a new payment transaction
    /// </summary>
    public class CreatePaymentRequest
    {
        [Required(ErrorMessage = "BookingId is required")]
        public int BookingId { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Payment method is required")]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = string.Empty; // "Cash", "Card", "QR"

        [StringLength(500)]
        public string? Description { get; set; }

        public decimal? DepositAmount { get; set; }
    }

    /// <summary>
    /// Request to update payment status
    /// </summary>
    public class UpdatePaymentStatusRequest
    {
        [Required(ErrorMessage = "Status is required")]
        [StringLength(50)]
        public string Status { get; set; } = string.Empty; // "Paid", "Unpaid", "Refunded"

        [StringLength(100)]
        public string? TransactionRef { get; set; }

        public int? UpdatedBy { get; set; }
    }

    /// <summary>
    /// Request to get payment history with filters
    /// </summary>
    public class GetPaymentHistoryRequest
    {
        public int? UserId { get; set; }
        public int? BookingId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentStatus { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    /// <summary>
    /// Transaction details response DTO
    /// </summary>
    public class TransactionDto
    {
        public int TransactionId { get; set; }
        public int BookingId { get; set; }
        public string? BookingReference { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal? DepositAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string TransactionStatus { get; set; } = string.Empty;
        public string? DepositStatus { get; set; }
        public string? TransactionRef { get; set; }
        public string? OrderCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DepositDate { get; set; }
        public string? CreatedByName { get; set; }
        public string? UpdatedByName { get; set; }
    }

    /// <summary>
    /// Paginated payment history response
    /// </summary>
    public class PaymentHistoryDto
    {
        public List<TransactionDto> Transactions { get; set; } = new();
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
    }

    #endregion

    #region Deposit Management DTOs

    /// <summary>
    /// Request to create a deposit for booking
    /// </summary>
    public class CreateDepositRequest
    {
        [Required(ErrorMessage = "BookingId is required")]
        public int BookingId { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Payment method is required")]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        public int? CreatedBy { get; set; }
    }

    /// <summary>
    /// Request to refund a deposit
    /// </summary>
    public class RefundDepositRequest
    {
        [Required(ErrorMessage = "Reason is required")]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        [Required(ErrorMessage = "Refund amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Refund amount must be greater than 0")]
        public decimal RefundAmount { get; set; }

        public int? ProcessedBy { get; set; }
    }

    /// <summary>
    /// Deposit status response DTO
    /// </summary>
    public class DepositStatusDto
    {
        public int TransactionId { get; set; }
        public int BookingId { get; set; }
        public decimal DepositAmount { get; set; }
        public string DepositStatus { get; set; } = string.Empty;
        public DateTime? DepositDate { get; set; }
        public bool IsRefunded { get; set; }
        public decimal? RefundedAmount { get; set; }
    }

    #endregion

    #region Refund Management DTOs

    /// <summary>
    /// Request to process a refund
    /// </summary>
    public class ProcessRefundRequest
    {
        [Required(ErrorMessage = "TransactionId is required")]
        public int TransactionId { get; set; }

        [Required(ErrorMessage = "Refund amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Refund amount must be greater than 0")]
        public decimal RefundAmount { get; set; }

        [Required(ErrorMessage = "Reason is required")]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        [StringLength(50)]
        public string? RefundMethod { get; set; }

        public int? ProcessedBy { get; set; }
    }

    /// <summary>
    /// Refund history item DTO
    /// </summary>
    public class RefundDto
    {
        public int TransactionId { get; set; }
        public int BookingId { get; set; }
        public string? BookingReference { get; set; }
        public decimal OriginalAmount { get; set; }
        public decimal RefundedAmount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? RefundMethod { get; set; }
        public DateTime RefundedAt { get; set; }
        public string? ProcessedByName { get; set; }
    }

    /// <summary>
    /// Refund history response
    /// </summary>
    public class RefundHistoryDto
    {
        public List<RefundDto> Refunds { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalRefundedAmount { get; set; }
    }

    #endregion

    #region Payment Method DTOs

    /// <summary>
    /// Payment method DTO
    /// </summary>
    public class PaymentMethodDto
    {
        public int CodeId { get; set; }
        public string CodeValue { get; set; } = string.Empty;
        public string CodeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int? DisplayOrder { get; set; }
    }

    /// <summary>
    /// Request to validate payment method
    /// </summary>
    public class ValidatePaymentMethodRequest
    {
        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        [Required]
        public decimal Amount { get; set; }
    }

    #endregion

    #region QR Payment DTOs

    /// <summary>
    /// Request to generate QR code for payment
    /// </summary>
    public class GenerateQRCodeRequest
    {
        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public int? BookingId { get; set; }

        [StringLength(100)]
        public string? OrderCode { get; set; }
    }

    /// <summary>
    /// QR code response DTO
    /// </summary>
    public class QRCodeDto
    {
        public string QRCodeBase64 { get; set; } = string.Empty;
        public string QRCodeUrl { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    /// <summary>
    /// Bank configuration DTO
    /// </summary>
    public class BankConfigDto
    {
        public int BankConfigId { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string BankCode { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string? BankBranch { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Request to update bank configuration
    /// </summary>
    public class UpdateBankConfigRequest
    {
        [Required(ErrorMessage = "Bank name is required")]
        [StringLength(100)]
        public string BankName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bank code is required")]
        [StringLength(20)]
        public string BankCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Account number is required")]
        [StringLength(50)]
        public string AccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Account name is required")]
        [StringLength(100)]
        public string AccountName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? BankBranch { get; set; }

        public bool IsActive { get; set; } = true;

        public int? UpdatedBy { get; set; }
    }

    /// <summary>
    /// Request to verify QR payment
    /// </summary>
    public class VerifyQRPaymentRequest
    {
        [Required]
        public int TransactionId { get; set; }

        [Required]
        [StringLength(100)]
        public string TransactionRef { get; set; } = string.Empty;

        public decimal? Amount { get; set; }

        public int? VerifiedBy { get; set; }
    }

    #endregion

    #region Transaction Statistics DTOs

    /// <summary>
    /// Transaction statistics DTO
    /// </summary>
    public class TransactionStatsDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalPending { get; set; }
        public decimal TotalRefunded { get; set; }
        public int TotalTransactions { get; set; }
        public int TotalPaidTransactions { get; set; }
        public int TotalPendingTransactions { get; set; }
        public int TotalRefundedTransactions { get; set; }
        public Dictionary<string, decimal> RevenueByMethod { get; set; } = new();
        public Dictionary<string, int> TransactionsByMethod { get; set; } = new();
    }

    /// <summary>
    /// Request for transaction statistics
    /// </summary>
    public class GetTransactionStatsRequest
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? PaymentMethod { get; set; }
        public int? EmployeeId { get; set; }
    }

    #endregion
}
