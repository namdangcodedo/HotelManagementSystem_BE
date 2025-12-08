using AppBackend.Services.ApiModels.TransactionModel;
using AppBackend.Services.Services.TransactionServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppBackend.ApiCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransactionController : BaseApiController
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        #region Payment Management

        /// <summary>
        /// Create a new payment transaction
        /// </summary>
        [HttpPost("payment/create")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _transactionService.CreatePaymentAsync(request, CurrentUserId);
            return HandleResult(result);
        }

        /// <summary>
        /// Update payment status
        /// </summary>
        [HttpPut("payment/{id}/status")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> UpdatePaymentStatus(int id, [FromBody] UpdatePaymentStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            request.UpdatedBy = CurrentUserId;
            var result = await _transactionService.UpdatePaymentStatusAsync(id, request);
            return HandleResult(result);
        }

        /// <summary>
        /// Get payment details by ID
        /// </summary>
        [HttpGet("payment/{id}")]
        public async Task<IActionResult> GetPaymentDetails(int id)
        {
            var result = await _transactionService.GetPaymentDetailsAsync(id);
            return HandleResult(result);
        }

        /// <summary>
        /// Get payment history with filters
        /// </summary>
        [HttpGet("payment/history")]
        public async Task<IActionResult> GetPaymentHistory([FromQuery] GetPaymentHistoryRequest request)
        {
            var result = await _transactionService.GetPaymentHistoryAsync(request);
            return HandleResult(result);
        }

        #endregion

        #region Deposit Management

        /// <summary>
        /// Create deposit for booking
        /// </summary>
        [HttpPost("deposit/create")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> CreateDeposit([FromBody] CreateDepositRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            request.CreatedBy = CurrentUserId;
            var result = await _transactionService.CreateDepositAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Refund deposit
        /// </summary>
        [HttpPost("deposit/{id}/refund")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> RefundDeposit(int id, [FromBody] RefundDepositRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            request.ProcessedBy = CurrentUserId;
            var result = await _transactionService.RefundDepositAsync(id, request);
            return HandleResult(result);
        }

        /// <summary>
        /// Get deposit status
        /// </summary>
        [HttpGet("deposit/{id}/status")]
        public async Task<IActionResult> GetDepositStatus(int id)
        {
            var result = await _transactionService.GetDepositStatusAsync(id);
            return HandleResult(result);
        }

        #endregion

        #region Refund Management

        /// <summary>
        /// Process refund
        /// </summary>
        [HttpPost("refund/process")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> ProcessRefund([FromBody] ProcessRefundRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            request.ProcessedBy = CurrentUserId;
            var result = await _transactionService.ProcessRefundAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Get refund history
        /// </summary>
        [HttpGet("refund/history")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> GetRefundHistory(
            [FromQuery] int? bookingId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var result = await _transactionService.GetRefundHistoryAsync(bookingId, fromDate, toDate);
            return HandleResult(result);
        }

        #endregion

        #region Payment Methods

        /// <summary>
        /// Get available payment methods
        /// </summary>
        [HttpGet("payment/methods")]
        public async Task<IActionResult> GetAvailablePaymentMethods()
        {
            var result = await _transactionService.GetAvailablePaymentMethodsAsync();
            return HandleResult(result);
        }

        /// <summary>
        /// Validate payment method
        /// </summary>
        [HttpPost("payment/method/validate")]
        public async Task<IActionResult> ValidatePaymentMethod([FromBody] ValidatePaymentMethodRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _transactionService.ValidatePaymentMethodAsync(request);
            return HandleResult(result);
        }

        #endregion

        #region QR Payment

        /// <summary>
        /// Generate QR code for payment
        /// </summary>
        [HttpPost("payment/qr/generate")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> GenerateQRCode([FromBody] GenerateQRCodeRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _transactionService.GenerateQRCodeAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Get bank configuration for QR
        /// </summary>
        [HttpGet("payment/bank/config")]
        public async Task<IActionResult> GetBankConfig()
        {
            var result = await _transactionService.GetBankConfigAsync();
            return HandleResult(result);
        }

        /// <summary>
        /// Update bank configuration (Admin only)
        /// </summary>
        [HttpPut("payment/bank/config")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBankConfig([FromBody] UpdateBankConfigRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            request.UpdatedBy = CurrentUserId;
            var result = await _transactionService.UpdateBankConfigAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Verify QR payment (manual verification)
        /// </summary>
        [HttpPost("payment/qr/verify")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> VerifyQRPayment([FromBody] VerifyQRPaymentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            request.VerifiedBy = CurrentUserId;
            var result = await _transactionService.VerifyQRPaymentAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Customer confirms payment has been made (sends notification to staff)
        /// </summary>
        [HttpPost("payment/confirm")]
        [Authorize]
        public async Task<IActionResult> ConfirmPaymentByCustomer([FromBody] ConfirmPaymentByCustomerRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _transactionService.ConfirmPaymentByCustomerAsync(request);
            return HandleResult(result);
        }

        #endregion

        #region PayOS Payment Link

        /// <summary>
        /// Create a PayOS payment link for a booking
        /// </summary>
        [HttpPost("payment/payos/link")]
        public async Task<IActionResult> CreatePayOSPaymentLink([FromBody] CreatePayOSPaymentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _transactionService.CreatePayOSPaymentLinkAsync(request);
            return HandleResult(result);
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Get transaction statistics
        /// </summary>
        [HttpGet("stats")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> GetTransactionStats([FromQuery] GetTransactionStatsRequest request)
        {
            var result = await _transactionService.GetTransactionStatsAsync(request);
            return HandleResult(result);
        }

        #endregion

        #region Booking Transactions

        /// <summary>
        /// Get all transactions for a booking
        /// </summary>
        [HttpGet("booking/{bookingId}")]
        public async Task<IActionResult> GetTransactionsByBookingId(int bookingId)
        {
            var result = await _transactionService.GetTransactionsByBookingIdAsync(bookingId);
            return HandleResult(result);
        }

        #endregion
    }
}
