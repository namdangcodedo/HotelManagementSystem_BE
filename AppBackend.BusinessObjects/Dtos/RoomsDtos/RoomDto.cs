using System;
using System.Collections.Generic;

namespace AppBackend.BusinessObjects.Dtos
{
    public class RoomDto
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public int RoomTypeId { get; set; }
        public decimal BasePriceNight { get; set; }
        public decimal BasePriceHour { get; set; }
        public int StatusId { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }
}

