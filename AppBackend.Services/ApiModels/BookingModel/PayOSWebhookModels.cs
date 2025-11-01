using Newtonsoft.Json;

namespace AppBackend.Services.ApiModels.BookingModel
{
    /// <summary>
    /// PayOS Webhook Request Model - Chính xác theo format của PayOS
    /// </summary>
    public class PayOSWebhookRequest
    {
        [JsonProperty("code")]
        public string Code { get; set; } = string.Empty;

        [JsonProperty("desc")]
        public string Desc { get; set; } = string.Empty;

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("data")]
        public PayOSWebhookData Data { get; set; } = new();

        [JsonProperty("signature")]
        public string Signature { get; set; } = string.Empty;
    }

    /// <summary>
    /// PayOS Webhook Data - Dữ liệu chi tiết từ PayOS
    /// </summary>
    public class PayOSWebhookData
    {
        [JsonProperty("orderCode")]
        public long OrderCode { get; set; }

        [JsonProperty("amount")]
        public int Amount { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("reference")]
        public string? Reference { get; set; }

        [JsonProperty("transactionDateTime")]
        public string? TransactionDateTime { get; set; }

        [JsonProperty("currency")]
        public string? Currency { get; set; }

        [JsonProperty("paymentLinkId")]
        public string? PaymentLinkId { get; set; }

        [JsonProperty("code")]
        public string? Code { get; set; }

        [JsonProperty("desc")]
        public string? Desc { get; set; }

        [JsonProperty("counterAccountBankId")]
        public string? CounterAccountBankId { get; set; }

        [JsonProperty("counterAccountBankName")]
        public string? CounterAccountBankName { get; set; }

        [JsonProperty("counterAccountName")]
        public string? CounterAccountName { get; set; }

        [JsonProperty("counterAccountNumber")]
        public string? CounterAccountNumber { get; set; }

        [JsonProperty("virtualAccountName")]
        public string? VirtualAccountName { get; set; }

        [JsonProperty("virtualAccountNumber")]
        public string? VirtualAccountNumber { get; set; }

        [JsonProperty("accountNumber")]
        public string? AccountNumber { get; set; }
    }

    /// <summary>
    /// Response từ webhook endpoint
    /// </summary>
    public class PayOSWebhookResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}
