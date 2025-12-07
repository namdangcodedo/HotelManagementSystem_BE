using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels.TransactionModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using AppBackend.Services.ApiModels.BookingModel;

namespace AppBackend.Services.Helpers
{
    /// <summary>
    /// Helper class for generating QR payment codes using VietQR standard
    /// </summary>
    public class QRPaymentHelper
    {
        private readonly ILogger<QRPaymentHelper> _logger;

        public QRPaymentHelper(ILogger<QRPaymentHelper> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Generate VietQR payment URL
        /// </summary>
        /// <param name="bankConfig">Bank configuration</param>
        /// <param name="amount">Payment amount</param>
        /// <param name="description">Payment description</param>
        /// <param name="orderCode">Optional order code</param>
        /// <returns>VietQR URL for QR code generation</returns>
        public string GenerateVietQRUrl(BankConfig bankConfig, decimal amount, string description, string? orderCode = null)
        {
            if (bankConfig == null)
                throw new ArgumentNullException(nameof(bankConfig));

            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than 0", nameof(amount));

            // VietQR API URL format
            // https://img.vietqr.io/image/{BANK_CODE}-{ACCOUNT_NUMBER}-{TEMPLATE}.png?amount={AMOUNT}&addInfo={DESCRIPTION}

            var accountName = UrlEncode(bankConfig.AccountName);
            var accountNumber = bankConfig.AccountNumber.Trim();
            var bankCode = bankConfig.BankCode.Trim().ToUpper();
            var template = "compact2"; // VietQR template style

            // Build description with order code if provided
            var finalDescription = string.IsNullOrEmpty(orderCode)
                ? description
                : $"{description} - {orderCode}";

            var encodedDescription = UrlEncode(finalDescription);

            // Build VietQR URL
            var qrUrl = $"https://img.vietqr.io/image/{bankCode}-{accountNumber}-{template}.png" +
                       $"?amount={amount:0}" +
                       $"&addInfo={encodedDescription}" +
                       $"&accountName={accountName}";

            return qrUrl;
        }

        /// <summary>
        /// Generate QR code data for manual QR generation libraries
        /// </summary>
        public string GenerateQRData(BankConfig bankConfig, decimal amount, string description)
        {
            // EMVCo QR Code format for Vietnam banks
            var qrData = new StringBuilder();
            qrData.Append($"Bank: {bankConfig.BankName}\n");
            qrData.Append($"Account: {bankConfig.AccountNumber}\n");
            qrData.Append($"Name: {bankConfig.AccountName}\n");
            qrData.Append($"Amount: {amount:N0} VND\n");
            qrData.Append($"Content: {description}");

            return qrData.ToString();
        }

        /// <summary>
        /// Validate bank configuration for QR generation
        /// </summary>
        public (bool IsValid, string? ErrorMessage) ValidateBankConfig(BankConfig? bankConfig)
        {
            if (bankConfig == null)
                return (false, "Bank configuration not found");

            if (!bankConfig.IsActive)
                return (false, "Bank configuration is inactive");

            if (string.IsNullOrWhiteSpace(bankConfig.BankCode))
                return (false, "Bank code is required");

            if (string.IsNullOrWhiteSpace(bankConfig.AccountNumber))
                return (false, "Account number is required");

            if (string.IsNullOrWhiteSpace(bankConfig.AccountName))
                return (false, "Account name is required");

            return (true, null);
        }

        /// <summary>
        /// Generate transaction reference code
        /// </summary>
        public string GenerateTransactionRef(int bookingId)
        {
            var timestamp = DateTime.UtcNow.ToString("yyMMddHHmmss");
            return $"TRX{bookingId:D6}{timestamp}";
        }

        /// <summary>
        /// Generate order code for payment
        /// </summary>
        public string GenerateOrderCode()
        {
            return DateTime.UtcNow.Ticks.ToString();
        }

        /// <summary>
        /// Format amount to Vietnamese currency
        /// </summary>
        public string FormatAmount(decimal amount)
        {
            return $"{amount:N0} VND";
        }

        /// <summary>
        /// URL encode string for QR parameters
        /// </summary>
        private string UrlEncode(string value)
        {
            return Uri.EscapeDataString(value);
        }

        /// <summary>
        /// Validate payment amount
        /// </summary>
        public (bool IsValid, string? ErrorMessage) ValidateAmount(decimal amount, decimal? minAmount = null, decimal? maxAmount = null)
        {
            if (amount <= 0)
                return (false, "Amount must be greater than 0");

            if (minAmount.HasValue && amount < minAmount.Value)
                return (false, $"Amount must be at least {FormatAmount(minAmount.Value)}");

            if (maxAmount.HasValue && amount > maxAmount.Value)
                return (false, $"Amount cannot exceed {FormatAmount(maxAmount.Value)}");

            return (true, null);
        }

        /// <summary>
        /// Get supported Vietnamese bank codes
        /// Reference: https://api.vietqr.io/v2/banks
        /// </summary>
        public Dictionary<string, string> GetSupportedBanks()
        {
            return new Dictionary<string, string>
            {
                { "VCB", "Vietcombank" },
                { "TCB", "Techcombank" },
                { "MB", "MBBank" },
                { "VIB", "VIB" },
                { "ICB", "VietinBank" },
                { "ACB", "ACB" },
                { "TPB", "TPBank" },
                { "STB", "Sacombank" },
                { "HDB", "HDBank" },
                { "BIDV", "BIDV" },
                { "VPB", "VPBank" },
                { "MSB", "MSB" },
                { "OCB", "OCB" },
                { "SHB", "SHB" },
                { "EIB", "Eximbank" },
                { "SEA", "SeABank" },
                { "ABB", "ABBANK" },
                { "VAB", "VietABank" },
                { "BAB", "BacABank" },
                { "NAB", "NamABank" },
                { "PGB", "PGBank" },
                { "GPB", "GPBank" },
                { "AGR", "Agribank" },
                { "SCB", "SCB" },
                { "LPB", "LienVietPostBank" }
            };
        }

        /// <summary>
        /// Validate bank code
        /// </summary>
        public bool IsValidBankCode(string bankCode)
        {
            var supportedBanks = GetSupportedBanks();
            return supportedBanks.ContainsKey(bankCode.ToUpper());
        }

        /// <summary>
        /// High-level helper: generate QRPaymentInfoDto using bank config from DB first, then fallback to appsettings VietQR section.
        /// Caller only needs to provide amount / description / transactionRef.
        /// </summary>
        public async Task<QRPaymentInfoDto?> GenerateQRPaymentInfoAsync(
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            decimal amount,
            string description,
            string transactionRef)
        {
            if (amount <= 0)
            {
                _logger.LogWarning("[QR Payment] Amount must be greater than 0");
                return null;
            }

            // 1. Try get active bank config from DB
            BankConfig? bankConfig = null;
            try
            {
                var configsList = await unitOfWork.BankConfigs.FindAsync(b => b.IsActive);
                bankConfig = configsList?.FirstOrDefault();
                if (bankConfig != null)
                {
                    _logger.LogInformation("[QR Payment] Using BankConfig from database");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[QR Payment] DB lookup failed: {ex.Message}");
            }

            // 2. If DB does not have active config, fallback to appsettings VietQR section
            if (bankConfig == null)
            {
                try
                {
                    var vietQrSection = configuration.GetSection("VietQR");
                    if (vietQrSection.Exists())
                    {
                        var bankName = vietQrSection["BankName"];
                        var bankCode = vietQrSection["BankCode"];
                        var accountNumber = vietQrSection["AccountNumber"];
                        var accountName = vietQrSection["AccountName"];

                        if (!string.IsNullOrWhiteSpace(bankName) &&
                            !string.IsNullOrWhiteSpace(bankCode) &&
                            !string.IsNullOrWhiteSpace(accountNumber) &&
                            !string.IsNullOrWhiteSpace(accountName))
                        {
                            bankConfig = new BankConfig
                            {
                                BankName = bankName,
                                BankCode = bankCode,
                                AccountNumber = accountNumber,
                                AccountName = accountName,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            };
                            _logger.LogInformation("[QR Payment] Using VietQR config from appsettings");
                        }
                        else
                        {
                            _logger.LogWarning("[QR Payment] VietQR config in appsettings is incomplete");
                            return null;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("[QR Payment] VietQR section not found in appsettings");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[QR Payment] Appsettings fallback failed: {ex.Message}");
                    return null;
                }
            }

            // 3. Validate bank config before using it
            var (isValid, errorMessage) = ValidateBankConfig(bankConfig);
            if (!isValid)
            {
                _logger.LogError($"[QR Payment] Invalid bank config: {errorMessage}");
                return null;
            }

            // 4. Build final description from template if available in appsettings
            var finalDescription = description;
            try
            {
                var descriptionTemplate = configuration["VietQR:DescriptionTemplate"];
                if (!string.IsNullOrWhiteSpace(descriptionTemplate))
                {
                    finalDescription = descriptionTemplate.Replace("{bookingReference}", transactionRef);
                    _logger.LogInformation($"[QR Payment] Using description template: {finalDescription}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[QR Payment] Failed to read description template: {ex.Message}, using default");
            }

            // 5. Build QR payment info (bankConfig is guaranteed non-null here)
            try
            {
                var qrUrl = GenerateVietQRUrl(bankConfig, amount, finalDescription, transactionRef);
                var qrDataText = GenerateQRData(bankConfig, amount, finalDescription);

                var qrPaymentInfo = new QRPaymentInfoDto
                {
                    QRCodeUrl = qrUrl,
                    BankName = bankConfig.BankName,
                    BankCode = bankConfig.BankCode,
                    AccountNumber = bankConfig.AccountNumber,
                    AccountName = bankConfig.AccountName,
                    Amount = amount,
                    Description = finalDescription,
                    TransactionRef = transactionRef,
                    QRDataText = qrDataText
                };

                _logger.LogInformation($"[QR Payment] QR payment info generated successfully for amount: {amount}");
                return qrPaymentInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[QR Payment] QR generation failed: {ex.Message}");
                return null;
            }
        }
    }
}
