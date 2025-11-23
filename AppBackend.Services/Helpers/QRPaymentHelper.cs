using AppBackend.BusinessObjects.Models;
using System.Text;

namespace AppBackend.Services.Helpers
{
    /// <summary>
    /// Helper class for generating QR payment codes using VietQR standard
    /// </summary>
    public class QRPaymentHelper
    {
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
    }
}
