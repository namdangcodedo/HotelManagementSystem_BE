using AppBackend.Repositories.UnitOfWork;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppBackend.Services.Helpers;

namespace AppBackend.Services.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;
        private readonly BookingTokenHelper _bookingTokenHelper;

        public EmailService(IConfiguration configuration, IUnitOfWork unitOfWork, BookingTokenHelper bookingTokenHelper)
        {
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _bookingTokenHelper = bookingTokenHelper;
        }
        public async Task SendEmail(string email, string subject, string body)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("StayHub", _configuration["EmailSettings:SenderEmail"]));
            emailMessage.To.Add(new MailboxAddress("", email));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart("html") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(_configuration["EmailSettings:SmtpServer"],
                                      int.Parse(_configuration["EmailSettings:Port"]),
                                      SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_configuration["EmailSettings:SenderEmail"],
                                           _configuration["EmailSettings:Password"]);
            await client.SendAsync(emailMessage);
            await client.DisconnectAsync(true);
        }
        public async Task SendOtpEmail(string email, string otp)
        {
            var templatePath = "/Users/tungld/Documents/Code/sp/HotelManagement/Backend/HotelManagementSystem_BE/AppBackend.Services/TemplateEmail/OtpEmailTemplate.html";
            string template;
            using (var reader = new System.IO.StreamReader(templatePath))
            {
                template = await reader.ReadToEndAsync();
            }
            var body = template.Replace("{{OTP}}", otp);
            await SendEmail(email, "Your OTP Code", body);
        }

        public async Task SendBookingConfirmationEmailAsync(string email, string customerName, int bookingId,
            DateTime checkInDate, DateTime checkOutDate, List<string> roomNames,
            decimal totalAmount, decimal depositAmount)
        {
            var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                "TemplateEmail", "BookingConfirmationTemplate.html");
            
            string template;
            using (var reader = new System.IO.StreamReader(templatePath))
            {
                template = await reader.ReadToEndAsync();
            }

            var numberOfNights = (checkOutDate - checkInDate).Days;

            // Build room list HTML
            var roomListHtml = string.Join("", roomNames.Select((room, index) => 
                $"<div class=\"room-item\">✓ {room}</div>"));

            // Replace placeholders
            var body = template
                .Replace("{{CustomerName}}", customerName)
                .Replace("{{BookingId}}", bookingId.ToString())
                .Replace("{{CheckInDate}}", checkInDate.ToString("dd/MM/yyyy HH:mm"))
                .Replace("{{CheckOutDate}}", checkOutDate.ToString("dd/MM/yyyy HH:mm"))
                .Replace("{{NumberOfNights}}", numberOfNights.ToString())
                .Replace("{{RoomList}}", roomListHtml)
                .Replace("{{TotalAmount}}", totalAmount.ToString("N0"))
                .Replace("{{DepositAmount}}", depositAmount.ToString("N0"));

            await SendEmail(email, $"Xác nhận đặt phòng #{bookingId} - StayHub Hotel", body);
        }

        public async Task SendBookingConfirmationEmailAsync(int bookingId, string? newAccountPassword = null)
        {
            // 1. Lấy thông tin booking
            var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null)
            {
                throw new Exception($"Booking {bookingId} không tồn tại");
            }

            // 2. Lấy thông tin customer và account
            var customer = await _unitOfWork.Customers.GetByIdAsync(booking.CustomerId);
            if (customer == null)
            {
                throw new Exception($"Customer không tồn tại");
            }

            // 3. Lấy email từ account của customer
            string? customerEmail = null;
            if (customer.AccountId.HasValue)
            {
                var account = await _unitOfWork.Accounts.GetByIdAsync(customer.AccountId.Value);
                customerEmail = account?.Email;
            }

            // Nếu không có email thì không gửi được
            if (string.IsNullOrEmpty(customerEmail))
            {
                throw new Exception("Customer không có email để gửi");
            }

            // 4. Mã hóa bookingId thành token
            var bookingToken = _bookingTokenHelper.EncodeBookingId(bookingId);

            // 5. Lấy frontend base URL từ configuration
            var frontendBaseUrl = _configuration["FrontendSettings:BaseUrl"] ?? "http://localhost:3000";
            var bookingDetailUrl = $"{frontendBaseUrl}/booking/{bookingToken}";

            // 6. Lấy danh sách phòng
            var bookingRooms = await _unitOfWork.BookingRooms.FindAsync(br => br.BookingId == bookingId);
            var roomIds = bookingRooms.Select(br => br.RoomId).ToList();
            var rooms = await Task.WhenAll(roomIds.Select(id => _unitOfWork.Rooms.GetByIdAsync(id)));
            var roomNames = rooms.Where(r => r != null).Select(r => r!.RoomName).ToList();

            // 7. Đọc template
            var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                "TemplateEmail", "BookingConfirmationTemplate.html");
            
            string template;
            using (var reader = new StreamReader(templatePath))
            {
                template = await reader.ReadToEndAsync();
            }

            // 8. Calculate số đêm
            var numberOfNights = (booking.CheckOutDate - booking.CheckInDate).Days;

            // 9. Build account info section nếu có password mới
            var accountInfoHtml = "";
            if (!string.IsNullOrEmpty(newAccountPassword))
            {
                var account = await _unitOfWork.Accounts.GetByIdAsync(customer.AccountId!.Value);
                accountInfoHtml = $@"
                <div class=""info-box"" style=""background: #dcfce7; border-left-color: #16a34a;"">
                    <strong>🎉 Tài khoản đã được tạo thành công!</strong>
                    <div style=""margin-top: 10px;"">
                        <div><strong>Email/Username:</strong> {account?.Email}</div>
                        <div><strong>Mật khẩu:</strong> {newAccountPassword}</div>
                        <div style=""margin-top: 10px; color: #15803d; font-size: 13px;"">
                            ⚠️ Vui lòng đổi mật khẩu sau khi đăng nhập lần đầu để bảo mật tài khoản.
                        </div>
                    </div>
                </div>";
            }

            // 10. Replace placeholders
            var body = template
                .Replace("{{CustomerName}}", customer.FullName ?? "Quý khách")
                .Replace("{{CheckInDate}}", booking.CheckInDate.ToString("dd/MM/yyyy HH:mm"))
                .Replace("{{CheckOutDate}}", booking.CheckOutDate.ToString("dd/MM/yyyy HH:mm"))
                .Replace("{{NumberOfNights}}", numberOfNights.ToString())
                .Replace("{{AccountInfo}}", accountInfoHtml)
                .Replace("{{BookingDetailUrl}}", bookingDetailUrl);

            // 11. Gửi email
            await SendEmail(customerEmail, $"Xác nhận đặt phòng - StayHub Hotel", body);
        }
    }
}
