using System.Security.Cryptography;
using System.Text;
using AppBackend.BusinessObjects.AppSettings;
using AppBackend.BusinessObjects.Dtos;

namespace AppBackend.Services.Helpers
{
    public class PayOSHelper
    {
        private readonly PayOSSettings _settings;

        public PayOSHelper(PayOSSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Verify PayOS webhook signature
        /// </summary>
        public bool VerifyWebhook(PayOSWebhookRequestDto webhook)
        {
            if (string.IsNullOrEmpty(webhook.Signature))
                return false;

            // Build raw string theo đúng format PayOS
            var rawData = BuildRawDataForSignature(webhook.Data);

            // Compute HMAC SHA256
            var computedSig = ComputeHmacSha256(rawData, _settings.ChecksumKey);

            Console.WriteLine($"[Webhook Debug] rawData: {rawData}");
            Console.WriteLine($"[Webhook Debug] computedSig: {computedSig}");
            Console.WriteLine($"[Webhook Debug] signature: {webhook.Signature}");

            return string.Equals(webhook.Signature, computedSig, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Build rawData string for signature: key1=value1&key2=value2...
        /// Keys sorted alphabetically, null -> empty string
        /// </summary>
        private string BuildRawDataForSignature(PayOSWebhookData data)
        {
            var dict = new Dictionary<string, string?>
            {
                { "accountNumber", data.AccountNumber },
                { "amount", data.Amount?.ToString() },
                { "code", data.Code },
                { "counterAccountBankId", data.CounterAccountBankId },
                { "counterAccountBankName", data.CounterAccountBankName },
                { "counterAccountName", data.CounterAccountName },
                { "counterAccountNumber", data.CounterAccountNumber },
                { "currency", data.Currency },
                { "desc", data.Desc },
                { "description", data.Description },
                { "orderCode", data.OrderCode.ToString() },
                { "paymentLinkId", data.PaymentLinkId },
                { "reference", data.Reference },
                { "transactionDateTime", data.TransactionDateTime },
                { "virtualAccountName", data.VirtualAccountName },
                { "virtualAccountNumber", data.VirtualAccountNumber }
            };

            // Sort keys alphabetically and join into query string
            var query = string.Join("&",
                dict.OrderBy(kv => kv.Key, StringComparer.Ordinal)
                    .Select(kv => $"{kv.Key}={(kv.Value ?? "")}"));

            return query;
        }

        /// <summary>
        /// Compute HMAC_SHA256
        /// </summary>
        private static string ComputeHmacSha256(string data, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
