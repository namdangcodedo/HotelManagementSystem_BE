namespace AppBackend.BusinessObjects.Enums
{
    public enum CommentStatus
    {
        Pending = 0,      // Đang chờ duyệt
        Approved = 1,     // Đã duyệt
        Rejected = 2,     // Bị từ chối
        Hidden = 3        // Đã ẩn
    }
}

