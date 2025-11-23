namespace AppBackend.ApiCore.Settings
{
    public class VietQRSettings
    {
        public string BankName { get; set; } = string.Empty;
        public string BankCode { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string MerchantName { get; set; } = string.Empty;
        public string MerchantCity { get; set; } = string.Empty;
        public string Currency { get; set; } = "VND";
        public string CountryCode { get; set; } = "VN";
        public string DescriptionTemplate { get; set; } = string.Empty;
        public string AdditionalDataReferenceKey { get; set; } = string.Empty;

        public PayloadSettingsModel PayloadSettings { get; set; } = new PayloadSettingsModel();

        public class PayloadSettingsModel
        {
            public bool IncludeMerchantName { get; set; } = true;
            public bool IncludeMerchantCity { get; set; } = true;
            public bool IncludeAccountInfo { get; set; } = true;
            public string PointOfInitiationMethod { get; set; } = "12";
        }
    }
}

