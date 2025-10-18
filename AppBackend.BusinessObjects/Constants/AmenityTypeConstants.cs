namespace AppBackend.BusinessObjects.Constants
{
    public static class AmenityTypeConstants
    {
        public const string Common = "Common";           // Tiện ích cơ bản - tự động hiển thị cho tất cả phòng
        public const string Additional = "Additional";   // Tiện ích bổ sung - có thể thêm vào phòng cụ thể

        public static readonly string[] AllTypes = { Common, Additional };
    }
}
