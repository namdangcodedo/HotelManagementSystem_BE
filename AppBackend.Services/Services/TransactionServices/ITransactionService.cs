using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.TransactionModel;

namespace AppBackend.Services.Services.TransactionServices
{
    public interface ITransactionService
    {
        #region Payment Management

        /// <summary>
        /// Create a new payment transaction
        /// </summary>
        Task<ResultModel> CreatePaymentAsync(CreatePaymentRequest request, int createdBy);

        /// <summary>
        /// Update payment status
        /// </summary>
        Task<ResultModel> UpdatePaymentStatusAsync(int transactionId, UpdatePaymentStatusRequest request);

        /// <summary>
        /// Get payment details by ID
        /// </summary>
        Task<ResultModel> GetPaymentDetailsAsync(int transactionId);

        /// <summary>
        /// Get payment history with filters
        /// </summary>
        Task<ResultModel> GetPaymentHistoryAsync(GetPaymentHistoryRequest request);

        #endregion

        #region Deposit Management

        /// <summary>
        /// Create deposit for booking
        /// </summary>
        Task<ResultModel> CreateDepositAsync(CreateDepositRequest request);

        /// <summary>
        /// Refund deposit
        /// </summary>
        Task<ResultModel> RefundDepositAsync(int transactionId, RefundDepositRequest request);

        /// <summary>
        /// Get deposit status
        /// </summary>
        Task<ResultModel> GetDepositStatusAsync(int transactionId);

        #endregion

        #region Refund Management

        /// <summary>
        /// Process refund
        /// </summary>
        Task<ResultModel> ProcessRefundAsync(ProcessRefundRequest request);

        /// <summary>
        /// Get refund history
        /// </summary>
        Task<ResultModel> GetRefundHistoryAsync(int? bookingId = null, DateTime? fromDate = null, DateTime? toDate = null);

        #endregion

        #region Payment Methods

        /// <summary>
        /// Get available payment methods
        /// </summary>
        Task<ResultModel> GetAvailablePaymentMethodsAsync();

        /// <summary>
        /// Validate payment method
        /// </summary>
        Task<ResultModel> ValidatePaymentMethodAsync(ValidatePaymentMethodRequest request);

        #endregion

        #region QR Payment

        /// <summary>
        /// Generate QR code for payment
        /// </summary>
        Task<ResultModel> GenerateQRCodeAsync(GenerateQRCodeRequest request);

        /// <summary>
        /// Get bank configuration
        /// </summary>
        Task<ResultModel> GetBankConfigAsync();

        /// <summary>
        /// Update bank configuration (Admin only)
        /// </summary>
        Task<ResultModel> UpdateBankConfigAsync(UpdateBankConfigRequest request);

        /// <summary>
        /// Verify QR payment
        /// </summary>
        Task<ResultModel> VerifyQRPaymentAsync(VerifyQRPaymentRequest request);

        #endregion

        #region Statistics

        /// <summary>
        /// Get transaction statistics
        /// </summary>
        Task<ResultModel> GetTransactionStatsAsync(GetTransactionStatsRequest request);

        #endregion

        #region Booking Transactions

        /// <summary>
        /// Get transactions by booking ID
        /// </summary>
        Task<ResultModel> GetTransactionsByBookingIdAsync(int bookingId);

        #endregion
    }
}
