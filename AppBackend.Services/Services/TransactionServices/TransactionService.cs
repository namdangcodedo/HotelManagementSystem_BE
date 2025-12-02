using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.TransactionModel;
using AppBackend.Services.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Net.payOS;
using Net.payOS.Types;
using Transaction = AppBackend.BusinessObjects.Models.Transaction;

namespace AppBackend.Services.Services.TransactionServices
{
    public class TransactionService : ITransactionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly QRPaymentHelper _qrPaymentHelper;
        private readonly CacheHelper _cacheHelper;
        private readonly ILogger<TransactionService> _logger;
        private readonly IConfiguration _configuration;
        private readonly PayOS _payOSClient;
        private readonly PayOSHelper _payOSHelper;

        public TransactionService(
            IUnitOfWork unitOfWork,
            QRPaymentHelper qrPaymentHelper,
            CacheHelper cacheHelper,
            ILogger<TransactionService> logger,
            IConfiguration configuration,
            PayOS payOSClient,
            PayOSHelper payOSHelper)
        {
            _unitOfWork = unitOfWork;
            _qrPaymentHelper = qrPaymentHelper;
            _cacheHelper = cacheHelper;
            _logger = logger;
            _configuration = configuration;
            _payOSClient = payOSClient;
            _payOSHelper = payOSHelper;
        }

        #region Payment Management

        public async Task<ResultModel> CreatePaymentAsync(CreatePaymentRequest request, int createdBy)
        {
            try
            {
                // Validate booking exists
                var booking = await _unitOfWork.Bookings.GetByIdAsync(request.BookingId);
                if (booking == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "NOT_FOUND",
                        StatusCode = 404,
                        Message = "Booking not found"
                    };
                }

                // Get payment method from CommonCode
                var paymentMethod = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "PaymentMethod" && c.CodeValue == request.PaymentMethod))
                    .FirstOrDefault();

                if (paymentMethod == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "INVALID_PAYMENT_METHOD",
                        StatusCode = 400,
                        Message = "Invalid payment method"
                    };
                }

                // Get payment status - default to Unpaid
                var paymentStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "PaymentStatus" && c.CodeValue == "Unpaid"))
                    .FirstOrDefault();

                // Get transaction status - default to Pending
                var transactionStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "TransactionStatus" && c.CodeValue == "Pending"))
                    .FirstOrDefault();

                if (paymentStatus == null || transactionStatus == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "MISSING_STATUS_CONFIG",
                        StatusCode = 500,
                        Message = "Payment or transaction status not configured in system"
                    };
                }

                // Generate transaction reference
                var transactionRef = _qrPaymentHelper.GenerateTransactionRef(request.BookingId);
                var orderCode = _qrPaymentHelper.GenerateOrderCode();

                // Create transaction
                var transaction = new Transaction
                {
                    BookingId = request.BookingId,
                    TotalAmount = request.Amount,
                    PaidAmount = 0,
                    PaymentMethodId = paymentMethod.CodeId,
                    PaymentStatusId = paymentStatus.CodeId,
                    TransactionStatusId = transactionStatus.CodeId,
                    TransactionRef = transactionRef,
                    OrderCode = orderCode,
                    DepositAmount = request.DepositAmount,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy
                };

                await _unitOfWork.Transactions.AddAsync(transaction);
                await _unitOfWork.SaveChangesAsync();

                // Build response DTO
                var transactionDto = new TransactionDto
                {
                    TransactionId = transaction.TransactionId,
                    BookingId = transaction.BookingId,
                    TotalAmount = transaction.TotalAmount,
                    PaidAmount = transaction.PaidAmount,
                    DepositAmount = transaction.DepositAmount,
                    PaymentMethod = paymentMethod.CodeValue,
                    PaymentStatus = paymentStatus.CodeValue,
                    TransactionStatus = transactionStatus.CodeValue,
                    TransactionRef = transaction.TransactionRef,
                    OrderCode = transaction.OrderCode,
                    CreatedAt = transaction.CreatedAt
                };

                _logger.LogInformation($"Payment transaction created: {transaction.TransactionId} for Booking: {request.BookingId}");

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 201,
                    Data = transactionDto,
                    Message = "Payment transaction created successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating payment for Booking: {request.BookingId}");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error creating payment: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> UpdatePaymentStatusAsync(int transactionId, UpdatePaymentStatusRequest request)
        {
            try
            {
                var transaction = await _unitOfWork.Transactions.GetByIdAsync(transactionId);
                if (transaction == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "NOT_FOUND",
                        StatusCode = 404,
                        Message = "Transaction not found"
                    };
                }

                // Get new payment status
                var newPaymentStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "PaymentStatus" && c.CodeValue == request.Status))
                    .FirstOrDefault();

                if (newPaymentStatus == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "INVALID_STATUS",
                        StatusCode = 400,
                        Message = "Invalid payment status"
                    };
                }

                // Update transaction
                transaction.PaymentStatusId = newPaymentStatus.CodeId;

                if (!string.IsNullOrEmpty(request.TransactionRef))
                {
                    transaction.TransactionRef = request.TransactionRef;
                }

                // If status is Paid, update paid amount and transaction status
                if (request.Status == "Paid")
                {
                    transaction.PaidAmount = transaction.TotalAmount;

                    var completedStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                        c.CodeType == "TransactionStatus" && c.CodeValue == "Completed"))
                        .FirstOrDefault();

                    if (completedStatus != null)
                    {
                        transaction.TransactionStatusId = completedStatus.CodeId;
                    }
                }

                transaction.UpdatedAt = DateTime.UtcNow;
                transaction.UpdatedBy = request.UpdatedBy;

                await _unitOfWork.Transactions.UpdateAsync(transaction);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Payment status updated: Transaction {transactionId} to {request.Status}");

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Message = "Payment status updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating payment status for Transaction: {transactionId}");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error updating payment status: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> GetPaymentDetailsAsync(int transactionId)
        {
            try
            {
                var transaction = await _unitOfWork.Transactions.GetSingleAsync(
                    t => t.TransactionId == transactionId,
                    t => t.Booking,
                    t => t.PaymentMethod,
                    t => t.PaymentStatus,
                    t => t.TransactionStatus,
                    t => t.DepositStatus
                );

                if (transaction == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "NOT_FOUND",
                        StatusCode = 404,
                        Message = "Transaction not found"
                    };
                }

                var transactionDto = new TransactionDto
                {
                    TransactionId = transaction.TransactionId,
                    BookingId = transaction.BookingId,
                    TotalAmount = transaction.TotalAmount,
                    PaidAmount = transaction.PaidAmount,
                    DepositAmount = transaction.DepositAmount,
                    PaymentMethod = transaction.PaymentMethod?.CodeValue ?? "",
                    PaymentStatus = transaction.PaymentStatus?.CodeValue ?? "",
                    TransactionStatus = transaction.TransactionStatus?.CodeValue ?? "",
                    DepositStatus = transaction.DepositStatus?.CodeValue,
                    TransactionRef = transaction.TransactionRef,
                    OrderCode = transaction.OrderCode,
                    CreatedAt = transaction.CreatedAt,
                    UpdatedAt = transaction.UpdatedAt,
                    DepositDate = transaction.DepositDate
                };

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = transactionDto,
                    Message = "Transaction details retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting payment details for Transaction: {transactionId}");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error retrieving payment details: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> GetPaymentHistoryAsync(GetPaymentHistoryRequest request)
        {
            try
            {
                var query = _unitOfWork.Transactions.FindAsync(t => true);
                var transactions = await query;
                var filteredQuery = transactions.AsQueryable();

                // Apply filters
                if (request.UserId.HasValue)
                {
                    var userBookings = await _unitOfWork.Bookings.FindAsync(b => b.CustomerId == request.UserId.Value);
                    var bookingIds = userBookings.Select(b => b.BookingId).ToList();
                    filteredQuery = filteredQuery.Where(t => bookingIds.Contains(t.BookingId));
                }

                if (request.BookingId.HasValue)
                {
                    filteredQuery = filteredQuery.Where(t => t.BookingId == request.BookingId.Value);
                }

                if (request.DateFrom.HasValue)
                {
                    filteredQuery = filteredQuery.Where(t => t.CreatedAt >= request.DateFrom.Value);
                }

                if (request.DateTo.HasValue)
                {
                    filteredQuery = filteredQuery.Where(t => t.CreatedAt <= request.DateTo.Value);
                }

                if (!string.IsNullOrEmpty(request.PaymentMethod))
                {
                    var methodCode = (await _unitOfWork.CommonCodes.FindAsync(c =>
                        c.CodeType == "PaymentMethod" && c.CodeValue == request.PaymentMethod))
                        .FirstOrDefault();

                    if (methodCode != null)
                    {
                        filteredQuery = filteredQuery.Where(t => t.PaymentMethodId == methodCode.CodeId);
                    }
                }

                if (!string.IsNullOrEmpty(request.PaymentStatus))
                {
                    var statusCode = (await _unitOfWork.CommonCodes.FindAsync(c =>
                        c.CodeType == "PaymentStatus" && c.CodeValue == request.PaymentStatus))
                        .FirstOrDefault();

                    if (statusCode != null)
                    {
                        filteredQuery = filteredQuery.Where(t => t.PaymentStatusId == statusCode.CodeId);
                    }
                }

                var totalRecords = filteredQuery.Count();
                var totalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize);

                var pagedTransactions = filteredQuery
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // Load related data
                var paymentMethods = (await _unitOfWork.CommonCodes.FindAsync(c => c.CodeType == "PaymentMethod")).ToList();
                var paymentStatuses = (await _unitOfWork.CommonCodes.FindAsync(c => c.CodeType == "PaymentStatus")).ToList();
                var transactionStatuses = (await _unitOfWork.CommonCodes.FindAsync(c => c.CodeType == "TransactionStatus")).ToList();

                var transactionDtos = pagedTransactions.Select(t => new TransactionDto
                {
                    TransactionId = t.TransactionId,
                    BookingId = t.BookingId,
                    TotalAmount = t.TotalAmount,
                    PaidAmount = t.PaidAmount,
                    DepositAmount = t.DepositAmount,
                    PaymentMethod = paymentMethods.FirstOrDefault(pm => pm.CodeId == t.PaymentMethodId)?.CodeValue ?? "",
                    PaymentStatus = paymentStatuses.FirstOrDefault(ps => ps.CodeId == t.PaymentStatusId)?.CodeValue ?? "",
                    TransactionStatus = transactionStatuses.FirstOrDefault(ts => ts.CodeId == t.TransactionStatusId)?.CodeValue ?? "",
                    TransactionRef = t.TransactionRef,
                    OrderCode = t.OrderCode,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                }).ToList();

                var totalAmount = filteredQuery.Sum(t => t.TotalAmount);
                var totalPaidAmount = filteredQuery.Sum(t => t.PaidAmount);

                var historyDto = new PaymentHistoryDto
                {
                    Transactions = transactionDtos,
                    TotalRecords = totalRecords,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = totalPages,
                    TotalAmount = totalAmount,
                    TotalPaidAmount = totalPaidAmount
                };

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = historyDto,
                    Message = "Payment history retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment history");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error retrieving payment history: {ex.Message}"
                };
            }
        }

        #endregion

        #region Deposit Management

        public async Task<ResultModel> CreateDepositAsync(CreateDepositRequest request)
        {
            try
            {
                // Check if transaction already exists for this booking
                var existingTransaction = (await _unitOfWork.Transactions.GetByBookingIdAsync(request.BookingId))
                    .FirstOrDefault();

                if (existingTransaction != null && existingTransaction.DepositAmount.HasValue && existingTransaction.DepositAmount.Value > 0)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "DEPOSIT_EXISTS",
                        StatusCode = 409,
                        Message = "Deposit already exists for this booking"
                    };
                }

                // Get payment method
                var paymentMethod = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "PaymentMethod" && c.CodeValue == request.PaymentMethod))
                    .FirstOrDefault();

                if (paymentMethod == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "INVALID_PAYMENT_METHOD",
                        StatusCode = 400,
                        Message = "Invalid payment method"
                    };
                }

                // Get deposit status - Paid
                var depositStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "DepositStatus" && c.CodeValue == "Paid"))
                    .FirstOrDefault();

                if (depositStatus == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "MISSING_STATUS_CONFIG",
                        StatusCode = 500,
                        Message = "Deposit status not configured"
                    };
                }

                if (existingTransaction != null)
                {
                    // Update existing transaction with deposit
                    existingTransaction.DepositAmount = request.Amount;
                    existingTransaction.DepositStatusId = depositStatus.CodeId;
                    existingTransaction.DepositDate = DateTime.UtcNow;
                    existingTransaction.UpdatedAt = DateTime.UtcNow;
                    existingTransaction.UpdatedBy = request.CreatedBy;

                    await _unitOfWork.Transactions.UpdateAsync(existingTransaction);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation($"Deposit added to existing transaction: {existingTransaction.TransactionId}");

                    return new ResultModel
                    {
                        IsSuccess = true,
                        ResponseCode = "SUCCESS",
                        StatusCode = 200,
                        Message = "Deposit added successfully"
                    };
                }
                else
                {
                    // Create new transaction for deposit
                    var booking = await _unitOfWork.Bookings.GetByIdAsync(request.BookingId);
                    if (booking == null)
                    {
                        return new ResultModel
                        {
                            IsSuccess = false,
                            ResponseCode = "NOT_FOUND",
                            StatusCode = 404,
                            Message = "Booking not found"
                        };
                    }

                    var paymentStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                        c.CodeType == "PaymentStatus" && c.CodeValue == "Unpaid"))
                        .FirstOrDefault();

                    var transactionStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                        c.CodeType == "TransactionStatus" && c.CodeValue == "Pending"))
                        .FirstOrDefault();

                    var transactionRef = _qrPaymentHelper.GenerateTransactionRef(request.BookingId);

                    var transaction = new Transaction
                    {
                        BookingId = request.BookingId,
                        TotalAmount = booking.TotalAmount,
                        PaidAmount = 0,
                        PaymentMethodId = paymentMethod.CodeId,
                        PaymentStatusId = paymentStatus!.CodeId,
                        TransactionStatusId = transactionStatus!.CodeId,
                        TransactionRef = transactionRef,
                        DepositAmount = request.Amount,
                        DepositStatusId = depositStatus.CodeId,
                        DepositDate = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = request.CreatedBy
                    };

                    await _unitOfWork.Transactions.AddAsync(transaction);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation($"Deposit transaction created: {transaction.TransactionId}");

                    return new ResultModel
                    {
                        IsSuccess = true,
                        ResponseCode = "SUCCESS",
                        StatusCode = 201,
                        Message = "Deposit created successfully"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating deposit for Booking: {request.BookingId}");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error creating deposit: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> RefundDepositAsync(int transactionId, RefundDepositRequest request)
        {
            try
            {
                var transaction = await _unitOfWork.Transactions.GetByIdAsync(transactionId);
                if (transaction == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "NOT_FOUND",
                        StatusCode = 404,
                        Message = "Transaction not found"
                    };
                }

                if (!transaction.DepositAmount.HasValue || transaction.DepositAmount.Value <= 0)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "NO_DEPOSIT",
                        StatusCode = 400,
                        Message = "No deposit found for this transaction"
                    };
                }

                if (request.RefundAmount > transaction.DepositAmount.Value)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "INVALID_AMOUNT",
                        StatusCode = 400,
                        Message = "Refund amount exceeds deposit amount"
                    };
                }

                // Get refunded status
                var refundedStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "DepositStatus" && c.CodeValue == "Refunded"))
                    .FirstOrDefault();

                if (refundedStatus == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "MISSING_STATUS_CONFIG",
                        StatusCode = 500,
                        Message = "Refunded status not configured"
                    };
                }

                // Update transaction deposit status
                transaction.DepositStatusId = refundedStatus.CodeId;
                transaction.UpdatedAt = DateTime.UtcNow;
                transaction.UpdatedBy = request.ProcessedBy;

                await _unitOfWork.Transactions.UpdateAsync(transaction);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Deposit refunded: Transaction {transactionId}, Amount {request.RefundAmount}");

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Message = "Deposit refunded successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error refunding deposit for Transaction: {transactionId}");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error refunding deposit: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> GetDepositStatusAsync(int transactionId)
        {
            try
            {
                var transaction = await _unitOfWork.Transactions.GetSingleAsync(
                    t => t.TransactionId == transactionId,
                    t => t.DepositStatus
                );

                if (transaction == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "NOT_FOUND",
                        StatusCode = 404,
                        Message = "Transaction not found"
                    };
                }

                var depositStatusDto = new DepositStatusDto
                {
                    TransactionId = transaction.TransactionId,
                    BookingId = transaction.BookingId,
                    DepositAmount = transaction.DepositAmount ?? 0,
                    DepositStatus = transaction.DepositStatus?.CodeValue ?? "None",
                    DepositDate = transaction.DepositDate,
                    IsRefunded = transaction.DepositStatus?.CodeValue == "Refunded",
                    RefundedAmount = transaction.DepositStatus?.CodeValue == "Refunded" ? transaction.DepositAmount : null
                };

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = depositStatusDto,
                    Message = "Deposit status retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting deposit status for Transaction: {transactionId}");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error retrieving deposit status: {ex.Message}"
                };
            }
        }

        #endregion

        #region Refund Management

        public async Task<ResultModel> ProcessRefundAsync(ProcessRefundRequest request)
        {
            try
            {
                var transaction = await _unitOfWork.Transactions.GetByIdAsync(request.TransactionId);
                if (transaction == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "NOT_FOUND",
                        StatusCode = 404,
                        Message = "Transaction not found"
                    };
                }

                if (request.RefundAmount > transaction.PaidAmount)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "INVALID_AMOUNT",
                        StatusCode = 400,
                        Message = "Refund amount exceeds paid amount"
                    };
                }

                // Get refunded payment status
                var refundedStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "PaymentStatus" && c.CodeValue == "Refunded"))
                    .FirstOrDefault();

                if (refundedStatus == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "MISSING_STATUS_CONFIG",
                        StatusCode = 500,
                        Message = "Refunded status not configured"
                    };
                }

                // Update transaction
                transaction.PaidAmount -= request.RefundAmount;
                transaction.PaymentStatusId = refundedStatus.CodeId;
                transaction.UpdatedAt = DateTime.UtcNow;
                transaction.UpdatedBy = request.ProcessedBy;

                await _unitOfWork.Transactions.UpdateAsync(transaction);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Refund processed: Transaction {request.TransactionId}, Amount {request.RefundAmount}");

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Message = "Refund processed successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing refund for Transaction: {request.TransactionId}");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error processing refund: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> GetRefundHistoryAsync(int? bookingId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var refundedStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "PaymentStatus" && c.CodeValue == "Refunded"))
                    .FirstOrDefault();

                if (refundedStatus == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = true,
                        ResponseCode = "SUCCESS",
                        StatusCode = 200,
                        Data = new RefundHistoryDto { Refunds = new List<RefundDto>(), TotalRecords = 0, TotalRefundedAmount = 0 },
                        Message = "No refund history found"
                    };
                }

                var query = await _unitOfWork.Transactions.FindAsync(t => t.PaymentStatusId == refundedStatus.CodeId);
                var refundedTransactions = query.AsQueryable();

                if (bookingId.HasValue)
                {
                    refundedTransactions = refundedTransactions.Where(t => t.BookingId == bookingId.Value);
                }

                if (fromDate.HasValue)
                {
                    refundedTransactions = refundedTransactions.Where(t => t.UpdatedAt >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    refundedTransactions = refundedTransactions.Where(t => t.UpdatedAt <= toDate.Value);
                }

                var refundDtos = refundedTransactions
                    .OrderByDescending(t => t.UpdatedAt)
                    .Select(t => new RefundDto
                    {
                        TransactionId = t.TransactionId,
                        BookingId = t.BookingId,
                        OriginalAmount = t.TotalAmount,
                        RefundedAmount = t.TotalAmount - t.PaidAmount,
                        Reason = "Refund processed",
                        RefundedAt = t.UpdatedAt ?? t.CreatedAt
                    }).ToList();

                var totalRefunded = refundDtos.Sum(r => r.RefundedAmount);

                var historyDto = new RefundHistoryDto
                {
                    Refunds = refundDtos,
                    TotalRecords = refundDtos.Count,
                    TotalRefundedAmount = totalRefunded
                };

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = historyDto,
                    Message = "Refund history retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting refund history");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error retrieving refund history: {ex.Message}"
                };
            }
        }

        #endregion

        #region Payment Methods

        public async Task<ResultModel> GetAvailablePaymentMethodsAsync()
        {
            try
            {
                var paymentMethods = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "PaymentMethod" && c.IsActive))
                    .OrderBy(c => c.DisplayOrder)
                    .Select(c => new PaymentMethodDto
                    {
                        CodeId = c.CodeId,
                        CodeValue = c.CodeValue,
                        CodeName = c.CodeName,
                        Description = c.Description,
                        IsActive = c.IsActive,
                        DisplayOrder = c.DisplayOrder
                    }).ToList();

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = paymentMethods,
                    Message = "Payment methods retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment methods");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error retrieving payment methods: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> ValidatePaymentMethodAsync(ValidatePaymentMethodRequest request)
        {
            try
            {
                var paymentMethod = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "PaymentMethod" && c.CodeValue == request.PaymentMethod && c.IsActive))
                    .FirstOrDefault();

                if (paymentMethod == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "INVALID_PAYMENT_METHOD",
                        StatusCode = 400,
                        Message = "Payment method is not available"
                    };
                }

                // Validate amount
                var (isValid, errorMessage) = _qrPaymentHelper.ValidateAmount(request.Amount);
                if (!isValid)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "INVALID_AMOUNT",
                        StatusCode = 400,
                        Message = errorMessage
                    };
                }

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Message = "Payment method is valid"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating payment method");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error validating payment method: {ex.Message}"
                };
            }
        }

        #endregion

        #region QR Payment

        public async Task<ResultModel> GenerateQRCodeAsync(GenerateQRCodeRequest request)
        {
            try
            {
                // Try get active bank config from DB. If the table doesn't exist or DB access fails
                // fall back to reading VietQR configuration from appsettings.json.
                BankConfig? bankConfig = null;
                try
                {
                    bankConfig = await _unitOfWork.BankConfigs.GetActiveBankConfigAsync();
                }
                catch (Exception dbEx)
                {
                    // Log warning and fallback to appsettings. This handles the case where the DB
                    // doesn't have the BankConfig table yet (e.g. missing migration)
                    _logger.LogWarning(dbEx, "Failed to read BankConfig from database - falling back to appsettings. Message: {Message}", dbEx.Message);
                }

                // If not found in DB (or DB read failed), try to read from appsettings VietQR section
                if (bankConfig == null)
                {
                    var vietQrSection = _configuration.GetSection("VietQR");
                    if (vietQrSection.Exists())
                    {
                        bankConfig = new BankConfig
                        {
                            BankName = vietQrSection["BankName"] ?? string.Empty,
                            BankCode = vietQrSection["BankCode"] ?? string.Empty,
                            AccountNumber = vietQrSection["AccountNumber"] ?? string.Empty,
                            AccountName = vietQrSection["AccountName"] ?? vietQrSection["MerchantName"] ?? string.Empty,
                            BankBranch = vietQrSection["BankBranch"],
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        _logger.LogInformation("Using VietQR configuration from appsettings as fallback.");
                    }
                }

                if (bankConfig == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "NO_BANK_CONFIG",
                        StatusCode = 404,
                        Message = "No active bank configuration found. Please configure bank settings first."
                    };
                }

                // Validate bank config
                var (isValid, errorMessage) = _qrPaymentHelper.ValidateBankConfig(bankConfig);
                if (!isValid)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "INVALID_BANK_CONFIG",
                        StatusCode = 400,
                        Message = errorMessage
                    };
                }

                // Generate QR code URL (image link) only, do not upload image to API
                var qrUrl = _qrPaymentHelper.GenerateVietQRUrl(
                    bankConfig,
                    request.Amount,
                    request.Description,
                    request.OrderCode
                );

                var qrCodeDto = new QRCodeDto
                {
                    QRCodeUrl = qrUrl,
                    QRCodeBase64 = string.Empty, // intentionally left empty; we return only the link
                    BankName = bankConfig.BankName,
                    AccountNumber = bankConfig.AccountNumber,
                    AccountName = bankConfig.AccountName,
                    Amount = request.Amount,
                    Description = request.Description,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15) // QR code expires in 15 minutes
                };

                _logger.LogInformation($"QR code link generated for amount: {request.Amount}");

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = qrCodeDto,
                    Message = "QR code generated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error generating QR code: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> GetBankConfigAsync()
        {
            try
            {
                var bankConfig = await _unitOfWork.BankConfigs.GetActiveBankConfigAsync();
                if (bankConfig == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "NOT_FOUND",
                        StatusCode = 404,
                        Message = "No active bank configuration found"
                    };
                }

                var bankConfigDto = new BankConfigDto
                {
                    BankConfigId = bankConfig.BankConfigId,
                    BankName = bankConfig.BankName,
                    BankCode = bankConfig.BankCode,
                    AccountNumber = bankConfig.AccountNumber,
                    AccountName = bankConfig.AccountName,
                    BankBranch = bankConfig.BankBranch,
                    IsActive = bankConfig.IsActive,
                    CreatedAt = bankConfig.CreatedAt,
                    UpdatedAt = bankConfig.UpdatedAt
                };

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = bankConfigDto,
                    Message = "Bank configuration retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bank config");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error retrieving bank configuration: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> UpdateBankConfigAsync(UpdateBankConfigRequest request)
        {
            try
            {
                // Validate bank code
                if (!_qrPaymentHelper.IsValidBankCode(request.BankCode))
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "INVALID_BANK_CODE",
                        StatusCode = 400,
                        Message = "Invalid bank code. Please use a valid Vietnamese bank code."
                    };
                }

                // Get existing active config and deactivate it
                var existingConfig = await _unitOfWork.BankConfigs.GetActiveBankConfigAsync();
                if (existingConfig != null)
                {
                    existingConfig.IsActive = false;
                    existingConfig.UpdatedAt = DateTime.UtcNow;
                    existingConfig.UpdatedBy = request.UpdatedBy;
                    await _unitOfWork.BankConfigs.UpdateAsync(existingConfig);
                }

                // Create new config
                var newConfig = new BankConfig
                {
                    BankName = request.BankName,
                    BankCode = request.BankCode.ToUpper(),
                    AccountNumber = request.AccountNumber,
                    AccountName = request.AccountName,
                    BankBranch = request.BankBranch,
                    IsActive = request.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = request.UpdatedBy
                };

                await _unitOfWork.BankConfigs.AddAsync(newConfig);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Bank config updated: {newConfig.BankName} - {newConfig.AccountNumber}");

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Message = "Bank configuration updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating bank config");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error updating bank configuration: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> VerifyQRPaymentAsync(VerifyQRPaymentRequest request)
        {
            try
            {
                var transaction = await _unitOfWork.Transactions.GetByIdAsync(request.TransactionId);
                if (transaction == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "NOT_FOUND",
                        StatusCode = 404,
                        Message = "Transaction not found"
                    };
                }

                // In a real implementation, you would verify with bank API
                // For now, we'll do a manual verification

                // Update transaction ref
                transaction.TransactionRef = request.TransactionRef;

                // Get paid status
                var paidStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "PaymentStatus" && c.CodeValue == "Paid"))
                    .FirstOrDefault();

                var completedStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "TransactionStatus" && c.CodeValue == "Completed"))
                    .FirstOrDefault();

                if (paidStatus != null)
                {
                    transaction.PaymentStatusId = paidStatus.CodeId;
                    transaction.PaidAmount = request.Amount ?? transaction.TotalAmount;
                }

                if (completedStatus != null)
                {
                    transaction.TransactionStatusId = completedStatus.CodeId;
                }

                transaction.UpdatedAt = DateTime.UtcNow;
                transaction.UpdatedBy = request.VerifiedBy;

                await _unitOfWork.Transactions.UpdateAsync(transaction);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"QR payment verified: Transaction {request.TransactionId}");

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Message = "QR payment verified successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verifying QR payment for Transaction: {request.TransactionId}");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error verifying QR payment: {ex.Message}"
                };
            }
        }

        #endregion

        #region Statistics

        public async Task<ResultModel> GetTransactionStatsAsync(GetTransactionStatsRequest request)
        {
            try
            {
                var query = await _unitOfWork.Transactions.GetAllAsync();
                var transactions = query.AsQueryable();

                // Apply filters
                if (request.DateFrom.HasValue)
                {
                    transactions = transactions.Where(t => t.CreatedAt >= request.DateFrom.Value);
                }

                if (request.DateTo.HasValue)
                {
                    transactions = transactions.Where(t => t.CreatedAt <= request.DateTo.Value);
                }

                if (!string.IsNullOrEmpty(request.PaymentMethod))
                {
                    var methodCode = (await _unitOfWork.CommonCodes.FindAsync(c =>
                        c.CodeType == "PaymentMethod" && c.CodeValue == request.PaymentMethod))
                        .FirstOrDefault();

                    if (methodCode != null)
                    {
                        transactions = transactions.Where(t => t.PaymentMethodId == methodCode.CodeId);
                    }
                }

                // Get status codes
                var paidStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "PaymentStatus" && c.CodeValue == "Paid"))
                    .FirstOrDefault();

                var pendingStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "PaymentStatus" && c.CodeValue == "Unpaid"))
                    .FirstOrDefault();

                var refundedStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "PaymentStatus" && c.CodeValue == "Refunded"))
                    .FirstOrDefault();

                var transactionsList = transactions.ToList();

                var stats = new TransactionStatsDto
                {
                    TotalRevenue = transactionsList.Sum(t => t.TotalAmount),
                    TotalPaid = transactionsList.Where(t => paidStatus != null && t.PaymentStatusId == paidStatus.CodeId).Sum(t => t.PaidAmount),
                    TotalPending = transactionsList.Where(t => pendingStatus != null && t.PaymentStatusId == pendingStatus.CodeId).Sum(t => t.TotalAmount),
                    TotalRefunded = transactionsList.Where(t => refundedStatus != null && t.PaymentStatusId == refundedStatus.CodeId).Sum(t => t.TotalAmount - t.PaidAmount),
                    TotalTransactions = transactionsList.Count,
                    TotalPaidTransactions = transactionsList.Count(t => paidStatus != null && t.PaymentStatusId == paidStatus.CodeId),
                    TotalPendingTransactions = transactionsList.Count(t => pendingStatus != null && t.PaymentStatusId == pendingStatus.CodeId),
                    TotalRefundedTransactions = transactionsList.Count(t => refundedStatus != null && t.PaymentStatusId == refundedStatus.CodeId)
                };

                // Get payment methods for grouping
                var paymentMethods = (await _unitOfWork.CommonCodes.FindAsync(c => c.CodeType == "PaymentMethod")).ToList();

                stats.RevenueByMethod = transactionsList
                    .GroupBy(t => t.PaymentMethodId)
                    .ToDictionary(
                        g => paymentMethods.FirstOrDefault(pm => pm.CodeId == g.Key)?.CodeValue ?? "Unknown",
                        g => g.Sum(t => t.PaidAmount)
                    );

                stats.TransactionsByMethod = transactionsList
                    .GroupBy(t => t.PaymentMethodId)
                    .ToDictionary(
                        g => paymentMethods.FirstOrDefault(pm => pm.CodeId == g.Key)?.CodeValue ?? "Unknown",
                        g => g.Count()
                    );

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = stats,
                    Message = "Transaction statistics retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction statistics");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error retrieving statistics: {ex.Message}"
                };
            }
        }

        #endregion

        #region Booking Transactions

        public async Task<ResultModel> GetTransactionsByBookingIdAsync(int bookingId)
        {
            try
            {
                var transactions = await _unitOfWork.Transactions.GetByBookingIdAsync(bookingId);

                if (!transactions.Any())
                {
                    return new ResultModel
                    {
                        IsSuccess = true,
                        ResponseCode = "SUCCESS",
                        StatusCode = 200,
                        Data = new List<TransactionDto>(),
                        Message = "No transactions found for this booking"
                    };
                }

                var paymentMethods = (await _unitOfWork.CommonCodes.FindAsync(c => c.CodeType == "PaymentMethod")).ToList();
                var paymentStatuses = (await _unitOfWork.CommonCodes.FindAsync(c => c.CodeType == "PaymentStatus")).ToList();
                var transactionStatuses = (await _unitOfWork.CommonCodes.FindAsync(c => c.CodeType == "TransactionStatus")).ToList();

                var transactionDtos = transactions.Select(t => new TransactionDto
                {
                    TransactionId = t.TransactionId,
                    BookingId = t.BookingId,
                    TotalAmount = t.TotalAmount,
                    PaidAmount = t.PaidAmount,
                    DepositAmount = t.DepositAmount,
                    PaymentMethod = paymentMethods.FirstOrDefault(pm => pm.CodeId == t.PaymentMethodId)?.CodeValue ?? "",
                    PaymentStatus = paymentStatuses.FirstOrDefault(ps => ps.CodeId == t.PaymentStatusId)?.CodeValue ?? "",
                    TransactionStatus = transactionStatuses.FirstOrDefault(ts => ts.CodeId == t.TransactionStatusId)?.CodeValue ?? "",
                    TransactionRef = t.TransactionRef,
                    OrderCode = t.OrderCode,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    DepositDate = t.DepositDate
                }).ToList();

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = transactionDtos,
                    Message = "Transactions retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting transactions for Booking: {bookingId}");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error retrieving transactions: {ex.Message}"
                };
            }
        }

        #endregion

        #region PayOS Payment

        public async Task<ResultModel> CreatePayOSPaymentLinkAsync(CreatePayOSPaymentRequest request)
        {
            try
            {
                // Validate booking
                var booking = await _unitOfWork.Bookings.GetByIdAsync(request.BookingId);
                if (booking == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "NOT_FOUND",
                        StatusCode = 404,
                        Message = "Booking not found"
                    };
                }

                // Determine amount: prefer DepositAmount if present, else TotalAmount
                var amountDecimal = booking.DepositAmount > 0 ? booking.DepositAmount : booking.TotalAmount;
                if (amountDecimal <= 0)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "INVALID_AMOUNT",
                        StatusCode = 400,
                        Message = "Invalid payment amount for booking"
                    };
                }

                // Build PayOS payment data
                long orderCode = long.Parse(DateTimeOffset.Now.ToString("yyMMddHHmmss"));
                var returnUrl = _configuration["PayOS:ReturnUrl"] ?? _configuration.GetSection("PayOS")["ReturnUrl"] ?? string.Empty;
                var cancelUrl = (_configuration["PayOS:CancelUrl"] ?? _configuration.GetSection("PayOS")["CancelUrl"] ?? string.Empty) + $"?bookingId={booking.BookingId}";

                var description = $"Booking #{booking.BookingId}";
                if (description.Length > 25) description = description.Substring(0, 25);

                // Set expiration time to 30 minutes from now (use UTC to avoid timezone issues)
                var expiredAt = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds();

                var paymentData = new PaymentData(
                    orderCode: orderCode,
                    amount: (int)amountDecimal,
                    description: description,
                    items: new List<ItemData>
                    {
                        new ItemData($"Booking #{booking.BookingId}", 1, (int)amountDecimal)
                    },
                    cancelUrl: cancelUrl,
                    returnUrl: returnUrl,
                    expiredAt: (int)expiredAt
                );

                CreatePaymentResult createPayment = null;
                try
                {
                    createPayment = await _payOSClient.createPaymentLink(paymentData);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calling PayOS to create payment link");
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "PAYOS_ERROR",
                        StatusCode = 500,
                        Message = $"Failed to create PayOS payment link: {ex.Message}"
                    };
                }

                var dto = new PayOSPaymentLinkDto
                {
                    PaymentUrl = createPayment?.checkoutUrl ?? string.Empty,
                    OrderId = orderCode.ToString(),
                    Amount = amountDecimal,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15)
                };

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = dto,
                    Message = "PayOS payment link created"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayOS payment link");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error creating PayOS payment link: {ex.Message}"
                };
            }
        }

        #endregion
    }
}
