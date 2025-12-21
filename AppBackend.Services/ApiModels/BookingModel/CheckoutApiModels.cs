using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.BookingModel
{
    /// <summary>
    /// Request để checkout - thanh toán phòng và dịch vụ đã sử dụng
    /// </summary>
    public class CheckoutRequest
    {
        /// <summary>
        /// Booking ID cần checkout
        /// </summary>
        [Required]
        public int BookingId { get; set; }

        /// <summary>
        /// Payment Method ID (từ CommonCode: Cash, Card, QR, PayOS)
        /// </summary>
        [Required]
        public int PaymentMethodId { get; set; }

        /// <summary>
        /// Ghi chú thanh toán
        /// </summary>
        public string? PaymentNote { get; set; }

        /// <summary>
        /// Mã giao dịch tham chiếu (nếu thanh toán qua bank transfer)
        /// </summary>
        public string? TransactionReference { get; set; }
    }

    /// <summary>
    /// Response chi tiết checkout
    /// </summary>
    public class CheckoutResponse
    {
        public int BookingId { get; set; }
        public string BookingType { get; set; } = string.Empty; // Hiển thị: "Online", "Đặt tại quầy"
        public string BookingTypeCode { get; set; } = string.Empty; // Logic: "Online", "WalkIn"
        public string BookingStatus { get; set; } = string.Empty; // Hiển thị: "Đã xác nhận", "Hoàn thành"
        public string BookingStatusCode { get; set; } = string.Empty; // Logic: "Confirmed", "Completed"

        /// <summary>
        /// Thông tin khách hàng
        /// </summary>
        public CustomerCheckoutInfo Customer { get; set; } = new CustomerCheckoutInfo();

        /// <summary>
        /// Ngày checkin/checkout
        /// </summary>
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public DateTime ActualCheckOutDate { get; set; }
        public int TotalNights { get; set; }
        public int ActualNights { get; set; }

        /// <summary>
        /// Breakdown chi tiết tiền phòng
        /// </summary>
        public List<RoomChargeDetail> RoomCharges { get; set; } = new List<RoomChargeDetail>();
        public decimal TotalRoomCharges { get; set; }

        /// <summary>
        /// Breakdown chi tiết tiền dịch vụ
        /// </summary>
        public List<ServiceChargeDetail> ServiceCharges { get; set; } = new List<ServiceChargeDetail>();
        public decimal TotalServiceCharges { get; set; }

        /// <summary>
        /// Tổng hóa đơn
        /// </summary>
        public decimal SubTotal { get; set; } // Room + Services

        /// <summary>
        /// Tiền cọc đã trả (nếu booking online)
        /// </summary>
        public decimal DepositPaid { get; set; }

        /// <summary>
        /// Tổng cần thanh toán
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Số tiền còn phải trả (Total - Deposit)
        /// </summary>
        public decimal AmountDue { get; set; }

        /// <summary>
        /// Phương thức thanh toán
        /// </summary>
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>
        /// Transaction ID được tạo
        /// </summary>
        public int TransactionId { get; set; }

        /// <summary>
        /// Thời gian checkout
        /// </summary>
        public DateTime CheckoutProcessedAt { get; set; }

        /// <summary>
        /// Nhân viên xử lý checkout
        /// </summary>
        public string? ProcessedBy { get; set; }
    }

    /// <summary>
    /// Thông tin khách hàng trong checkout
    /// </summary>
    public class CustomerCheckoutInfo
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? IdentityCard { get; set; }
    }

    /// <summary>
    /// Chi tiết tính tiền từng phòng
    /// </summary>
    public class RoomChargeDetail
    {
        public int BookingRoomId { get; set; }
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string RoomTypeName { get; set; } = string.Empty; // Hiển thị: "Phòng Tiêu Chuẩn"
        public string RoomTypeCode { get; set; } = string.Empty; // Logic: "Standard"
        public decimal PricePerNight { get; set; }
        public int PlannedNights { get; set; }
        public int ActualNights { get; set; }
        public decimal SubTotal { get; set; } // PricePerNight × ActualNights
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
    }

    /// <summary>
    /// Chi tiết tính tiền dịch vụ
    /// </summary>
    public class ServiceChargeDetail
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty; // Hiển thị: "Giặt ủi"
        public string ServiceCode { get; set; } = string.Empty; // Logic: "Laundry"
        public decimal PricePerUnit { get; set; }
        public int Quantity { get; set; }
        public decimal SubTotal { get; set; } // PricePerUnit × Quantity
        public DateTime ServiceDate { get; set; }
        public string ServiceType { get; set; } = string.Empty; // "RoomService" or "BookingService"
        public string? RoomName { get; set; } // Nếu là dịch vụ theo phòng
    }

    /// <summary>
    /// Request xem trước hóa đơn checkout (preview) - không lưu DB
    /// </summary>
    public class PreviewCheckoutRequest
    {
        [Required]
        public int BookingId { get; set; }
    }

    /// <summary>
    /// Response preview checkout - giống CheckoutResponse nhưng chưa xử lý thanh toán
    /// </summary>
    public class PreviewCheckoutResponse
    {
        public int BookingId { get; set; }
        public string BookingType { get; set; } = string.Empty; // Hiển thị: "Online", "Đặt tại quầy"
        public string BookingTypeCode { get; set; } = string.Empty; // Logic: "Online", "WalkIn"
        public CustomerCheckoutInfo Customer { get; set; } = new CustomerCheckoutInfo();

        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int TotalNights { get; set; }

        public List<RoomChargeDetail> RoomCharges { get; set; } = new List<RoomChargeDetail>();
        public decimal TotalRoomCharges { get; set; }

        public List<ServiceChargeDetail> ServiceCharges { get; set; } = new List<ServiceChargeDetail>();
        public decimal TotalServiceCharges { get; set; }

        public decimal SubTotal { get; set; }
        public decimal DepositPaid { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountDue { get; set; }

        public string BookingStatus { get; set; } = string.Empty;
        public string BookingStatusCode { get; set; } = string.Empty;
    }
}
