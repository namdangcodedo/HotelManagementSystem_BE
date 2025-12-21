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
        private readonly AccountTokenHelper _accountTokenHelper;

        public EmailService(IConfiguration configuration, IUnitOfWork unitOfWork, BookingTokenHelper bookingTokenHelper, AccountTokenHelper accountTokenHelper)
        {
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _bookingTokenHelper = bookingTokenHelper;
            _accountTokenHelper = accountTokenHelper;
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
            var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                "TemplateEmail", "OtpEmailTemplate.html");
            
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

        public async Task SendAccountActivationEmailAsync(int accountId)
        {
            // 1. Lấy thông tin account
            var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);
            if (account == null)
            {
                throw new Exception($"Account {accountId} không tồn tại");
            }

            // 2. Lấy thông tin customer để lấy FullName
            var customers = await _unitOfWork.Customers.FindAsync(c => c.AccountId == accountId);
            var customer = customers.FirstOrDefault();
            var customerName = customer?.FullName ?? account.Username;

            // 3. Mã hóa accountId thành token
            var activationToken = _accountTokenHelper.EncodeAccountId(accountId);

            // 4. Lấy frontend base URL từ configuration
            var frontendBaseUrl = _configuration["FrontendSettings:BaseUrl"] ?? "http://localhost:3000";
            var activationUrl = $"{frontendBaseUrl}/activate-account/{activationToken}";

            // 5. Đọc template
            var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                "TemplateEmail", "AccountActivationTemplate.html");
            
            string template;
            using (var reader = new StreamReader(templatePath))
            {
                template = await reader.ReadToEndAsync();
            }

            // 6. Replace placeholders
            var body = template
                .Replace("{{CustomerName}}", customerName)
                .Replace("{{Email}}", account.Email)
                .Replace("{{Username}}", account.Username)
                .Replace("{{ActivationUrl}}", activationUrl);

            // 7. Gửi email
            await SendEmail(account.Email, "Kích hoạt tài khoản - StayHub Hotel", body);
        }

        public async Task SendPaymentNotificationToStaffAsync(int bookingId, string orderCode)
        {
            try
            {
                // 1. Lấy thông tin booking
                var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    throw new Exception($"Booking with ID {bookingId} not found");
                }

                // 2. Lấy thông tin customer
                var customer = await _unitOfWork.Customers.GetByIdAsync(booking.CustomerId);
                if (customer == null)
                {
                    throw new Exception($"Customer with ID {booking.CustomerId} not found");
                }

                var customerAccount = await _unitOfWork.Accounts.GetByIdAsync(customer.AccountId ?? 0);

                // 3. Lấy thông tin phòng
                var bookingRooms = await _unitOfWork.BookingRooms.FindAsync(br => br.BookingId == bookingId);
                var roomIds = bookingRooms.Select(br => br.RoomId).ToList();
                var rooms = await _unitOfWork.Rooms.FindAsync(r => roomIds.Contains(r.RoomId));
                
                var roomTypeIds = rooms.Select(r => r.RoomTypeId).Distinct().ToList();
                var roomTypes = await _unitOfWork.RoomTypes.FindAsync(rt => roomTypeIds.Contains(rt.RoomTypeId));
                var roomTypesStr = string.Join(", ", roomTypes.Select(rt => rt.TypeName));

                // 4. Lấy thông tin transaction
                var transactions = await _unitOfWork.Transactions.FindAsync(t => 
                    t.BookingId == bookingId && t.OrderCode == orderCode);
                var transaction = transactions.FirstOrDefault();

                if (transaction == null)
                {
                    throw new Exception($"Transaction with OrderCode {orderCode} not found");
                }

                var paymentMethod = await _unitOfWork.CommonCodes.GetByIdAsync(transaction.PaymentMethodId);

                // 5. Lấy danh sách email staff (Receptionist, Manager, Admin)
                var staffRoles = new[] { "Receptionist", "Manager", "Admin" };
                var allRoles = await _unitOfWork.Roles.GetAllAsync();
                var staffRoleIds = allRoles.Where(r => staffRoles.Contains(r.RoleName)).Select(r => r.RoleId).ToList();

                // Lấy staff accounts thông qua AccountRole table (many-to-many relationship)
                var staffAccountIds = await _unitOfWork.Roles.GetAccountIdsByRoleIdsAsync(staffRoleIds);
                
                // Lọc các account là Employee và không bị khóa
                var allEmployees = await _unitOfWork.Employees.GetAllAsync();
                var employeeAccountIds = allEmployees.Select(e => e.AccountId).ToHashSet();
                
                var staffAccounts = await _unitOfWork.Accounts.FindAsync(a => 
                    staffAccountIds.Contains(a.AccountId) && 
                    employeeAccountIds.Contains(a.AccountId) && 
                    !a.IsLocked);

                if (!staffAccounts.Any())
                {
                    throw new Exception("No staff accounts found to send notification");
                }

                // 6. Đọc template
                var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "TemplateEmail", "PaymentNotificationTemplate.html");

                string template;
                using (var reader = new StreamReader(templatePath))
                {
                    template = await reader.ReadToEndAsync();
                }

                // 7. Tạo verify link
                var frontendUrl = _configuration["FrontendSettings:BaseUrl"] ?? "http://localhost:5173";
                var verifyLink = $"{frontendUrl}/admin/transactions?bookingId={bookingId}&highlight={transaction.TransactionId}";

                // 8. Replace placeholders
                var body = template
                    .Replace("{{CustomerName}}", customer.FullName ?? "N/A")
                    .Replace("{{CustomerEmail}}", customerAccount?.Email ?? "N/A")
                    .Replace("{{CustomerPhone}}", customer.PhoneNumber ?? "N/A")
                    .Replace("{{BookingId}}", bookingId.ToString())
                    .Replace("{{CheckInDate}}", booking.CheckInDate.ToString("dd/MM/yyyy HH:mm"))
                    .Replace("{{CheckOutDate}}", booking.CheckOutDate.ToString("dd/MM/yyyy HH:mm"))
                    .Replace("{{RoomTypes}}", roomTypesStr)
                    .Replace("{{OrderCode}}", orderCode)
                    .Replace("{{Amount}}", transaction.TotalAmount.ToString("N0"))
                    .Replace("{{PaymentMethod}}", paymentMethod?.CodeValue ?? "Chuyển khoản ngân hàng")
                    .Replace("{{ConfirmTime}}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"))
                    .Replace("{{VerifyLink}}", verifyLink);

                // 9. Gửi email đến tất cả staff
                var emailTasks = staffAccounts.Select(staff => 
                    SendEmail(staff.Email, 
                        $"🔔 Xác nhận thanh toán - Booking #{bookingId}", 
                        body)
                );

                await Task.WhenAll(emailTasks);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending payment notification: {ex.Message}", ex);
            }
        }

        public async Task SendDepositConfirmationEmailAsync(int bookingId)
        {
            try
            {
                var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    throw new Exception($"Booking {bookingId} không tồn tại");
                }

                var customer = await _unitOfWork.Customers.GetByIdAsync(booking.CustomerId);
                if (customer == null)
                {
                    throw new Exception("Customer không tồn tại");
                }

                string? customerEmail = null;
                if (customer.AccountId.HasValue)
                {
                    var account = await _unitOfWork.Accounts.GetByIdAsync(customer.AccountId.Value);
                    customerEmail = account?.Email;
                }

                if (string.IsNullOrEmpty(customerEmail))
                {
                    throw new Exception("Customer không có email để gửi");
                }

                var bookingToken = _bookingTokenHelper.EncodeBookingId(bookingId);
                var frontendBaseUrl = _configuration["FrontendSettings:BaseUrl"] ?? "http://localhost:3000";
                var bookingDetailUrl = $"{frontendBaseUrl}/booking/{bookingToken}";

                var bookingRooms = await _unitOfWork.BookingRooms.FindAsync(br => br.BookingId == bookingId);
                var roomIds = bookingRooms.Select(br => br.RoomId).ToList();
                var rooms = await Task.WhenAll(roomIds.Select(id => _unitOfWork.Rooms.GetByIdAsync(id)));
                var roomNames = rooms.Where(r => r != null).Select(r => r!.RoomName).ToList();

                var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                    "TemplateEmail", "BookingConfirmationTemplate.html");
                
                string template;
                using (var reader = new StreamReader(templatePath))
                {
                    template = await reader.ReadToEndAsync();
                }

                var numberOfNights = (booking.CheckOutDate - booking.CheckInDate).Days;

                var successBadgeHtml = @"
                    <div class=""success-badge"" style=""background: #10b981; color: white; display: inline-block; padding: 8px 20px; border-radius: 20px; font-size: 14px; font-weight: 600; margin-bottom: 20px;"">
                        ✓ Thanh toán cọc thành công
                    </div>";

                var body = template
                    .Replace("{{CustomerName}}", customer.FullName ?? "Quý khách")
                    .Replace("{{CheckInDate}}", booking.CheckInDate.ToString("dd/MM/yyyy HH:mm"))
                    .Replace("{{CheckOutDate}}", booking.CheckOutDate.ToString("dd/MM/yyyy HH:mm"))
                    .Replace("{{NumberOfNights}}", numberOfNights.ToString())
                    .Replace("{{BookingDetailUrl}}", bookingDetailUrl)
                    .Replace("{{AccountInfo}}", "")
                    .Replace("<div class=\"success-badge\">✓ Đặt phòng thành công</div>", successBadgeHtml);

                await SendEmail(customerEmail, $"Xác nhận thanh toán cọc - Booking #{bookingId} - StayHub Hotel", body);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending deposit confirmation email: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gửi email thông báo hủy booking đến quản lý/manager
        /// </summary>
        public async Task SendBookingCancellationEmailToManagerAsync(int bookingId, string reason = "")
        {
            try
            {
                // 1. Lấy thông tin booking
                var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    throw new Exception($"Booking with ID {bookingId} not found");
                }

                // 2. Lấy thông tin customer
                var customer = await _unitOfWork.Customers.GetByIdAsync(booking.CustomerId);
                if (customer == null)
                {
                    throw new Exception($"Customer with ID {booking.CustomerId} not found");
                }

                // 3. Lấy thông tin phòng
                var bookingRooms = await _unitOfWork.BookingRooms.FindAsync(br => br.BookingId == bookingId);
                var roomIds = bookingRooms.Select(br => br.RoomId).ToList();
                var rooms = await _unitOfWork.Rooms.FindAsync(r => roomIds.Contains(r.RoomId));
                var roomNames = string.Join(", ", rooms.Select(r => r.RoomName));

                // 4. Lấy status code
                var statusCode = booking.StatusId.HasValue
                    ? await _unitOfWork.CommonCodes.GetByIdAsync(booking.StatusId.Value)
                    : null;

                // 5. Lấy danh sách email Manager/Admin
                var managerRoles = new[] { "Manager", "Admin" };
                var allRoles = await _unitOfWork.Roles.GetAllAsync();
                var managerRoleIds = allRoles.Where(r => managerRoles.Contains(r.RoleValue)).Select(r => r.RoleId).ToList();

                // Lấy manager accounts
                var managerAccountIds = await _unitOfWork.Roles.GetAccountIdsByRoleIdsAsync(managerRoleIds);
                
                var allEmployees = await _unitOfWork.Employees.GetAllAsync();
                var employeeAccountIds = allEmployees.Select(e => e.AccountId).ToHashSet();
                
                var managerAccounts = await _unitOfWork.Accounts.FindAsync(a => 
                    managerAccountIds.Contains(a.AccountId) && 
                    employeeAccountIds.Contains(a.AccountId) && 
                    !a.IsLocked);

                if (!managerAccounts.Any())
                {
                    throw new Exception("No manager accounts found to send notification");
                }

                // 6. Tạo nội dung email
                var numberOfNights = (booking.CheckOutDate - booking.CheckInDate).Days;
                var cancellationTime = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss");

                var emailBody = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: #dc2626; color: white; padding: 20px; border-radius: 5px 5px 0 0; }}
                        .content {{ background: #f9fafb; padding: 20px; }}
                        .footer {{ background: #f3f4f6; padding: 10px; border-radius: 0 0 5px 5px; font-size: 12px; color: #666; }}
                        .info-row {{ margin: 10px 0; }}
                        .label {{ font-weight: 600; color: #374151; }}
                        .value {{ color: #6b7280; }}
                        .alert {{ background: #fee2e2; border-left: 4px solid #dc2626; padding: 15px; margin: 15px 0; border-radius: 3px; }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <div class=""header"">
                            <h2>⚠️ THÔNG BÁO HỦY ĐẶT PHÒNG</h2>
                        </div>
                        <div class=""content"">
                            <p>Xin chào,</p>
                            <p>Có một booking vừa bị hủy. Vui lòng xem chi tiết dưới đây:</p>
                            
                            <div class=""alert"">
                                <strong>Booking #{bookingId} đã bị HỦY</strong>
                            </div>

                            <div class=""info-row"">
                                <span class=""label"">Khách hàng:</span>
                                <span class=""value"">{customer.FullName ?? "N/A"}</span>
                            </div>

                            <div class=""info-row"">
                                <span class=""label"">Số điện thoại:</span>
                                <span class=""value"">{customer.PhoneNumber ?? "N/A"}</span>
                            </div>

                            <div class=""info-row"">
                                <span class=""label"">Email khách:</span>
                                <span class=""value""><a href=""mailto:{customer.FullName}"">{customer.FullName}</a></span>
                            </div>

                            <div class=""info-row"">
                                <span class=""label"">Phòng đã đặt:</span>
                                <span class=""value"">{roomNames}</span>
                            </div>

                            <div class=""info-row"">
                                <span class=""label"">Check-in:</span>
                                <span class=""value"">{booking.CheckInDate:dd/MM/yyyy HH:mm}</span>
                            </div>

                            <div class=""info-row"">
                                <span class=""label"">Check-out:</span>
                                <span class=""value"">{booking.CheckOutDate:dd/MM/yyyy HH:mm}</span>
                            </div>

                            <div class=""info-row"">
                                <span class=""label"">Số đêm:</span>
                                <span class=""value"">{numberOfNights}</span>
                            </div>

                            <div class=""info-row"">
                                <span class=""label"">Tổng tiền:</span>
                                <span class=""value"">{booking.TotalAmount:N0} VND</span>
                            </div>

                            <div class=""info-row"">
                                <span class=""label"">Tiền cọc:</span>
                                <span class=""value"">{booking.DepositAmount:N0} VND</span>
                            </div>

                            <div class=""info-row"">
                                <span class=""label"">Trạng thái:</span>
                                <span class=""value"">{statusCode?.CodeValue ?? "N/A"}</span>
                            </div>

                            <div class=""info-row"">
                                <span class=""label"">Thời gian hủy:</span>
                                <span class=""value"">{cancellationTime}</span>
                            </div>

                            {(string.IsNullOrEmpty(reason) ? "" : $@"
                            <div class=""info-row"">
                                <span class=""label"">Lý do hủy:</span>
                                <span class=""value"">{reason}</span>
                            </div>")}

                            <p style=""margin-top: 20px; padding-top: 20px; border-top: 1px solid #e5e7eb;"">
                                Vui lòng kiểm tra hệ thống để cập nhật trạng thái booking và liên hệ khách hàng nếu cần thiết.
                            </p>
                        </div>
                        <div class=""footer"">
                            <p>StayHub Hotel Management System</p>
                            <p>Đây là email tự động, vui lòng không trả lời email này.</p>
                        </div>
                    </div>
                </body>
                </html>";

                // 7. Gửi email đến tất cả managers
                foreach (var manager in managerAccounts)
                {
                    await SendEmail(manager.Email, $"⚠️ Thông báo hủy booking #{bookingId}", emailBody);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending booking cancellation email to manager: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gửi email thông báo khách đã thanh toán - Manager cần xác nhận
        /// </summary>
        public Task SendPaymentConfirmationRequestEmailToManagerAsync(int bookingId)
        {
            // Fire-and-forget: chạy background, không block caller
            _ = Task.Run(async () =>
            {
                try
                {
                    // 1. Lấy thông tin booking
                    var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
                    if (booking == null)
                    {
                        Console.WriteLine($"[EmailService] Booking with ID {bookingId} not found");
                        return;
                    }

                    // 2. Lấy thông tin customer
                    var customer = await _unitOfWork.Customers.GetByIdAsync(booking.CustomerId);
                    if (customer == null)
                    {
                        Console.WriteLine($"[EmailService] Customer with ID {booking.CustomerId} not found");
                        return;
                    }

                    // 3. Lấy email customer (nếu có)
                    string customerEmail = "N/A";
                    if (customer.AccountId.HasValue)
                    {
                        var customerAccount = await _unitOfWork.Accounts.GetByIdAsync(customer.AccountId.Value);
                        customerEmail = customerAccount?.Email ?? "N/A";
                    }

                    // 4. Lấy thông tin phòng
                    var bookingRooms = await _unitOfWork.BookingRooms.FindAsync(br => br.BookingId == bookingId);
                    var roomIds = bookingRooms.Select(br => br.RoomId).ToList();
                    var rooms = await _unitOfWork.Rooms.FindAsync(r => roomIds.Contains(r.RoomId));
                    var roomNames = string.Join(", ", rooms.Select(r => r.RoomName));

                    // 5. Lấy danh sách email Manager/Admin
                    var managerRoles = new[] { "Manager", "Admin","Receptionist" };
                    var allRoles = await _unitOfWork.Roles.GetAllAsync();
                    var managerRoleIds = allRoles.Where(r => managerRoles.Contains(r.RoleValue)).Select(r => r.RoleId).ToList();

                    var managerAccountIds = await _unitOfWork.Roles.GetAccountIdsByRoleIdsAsync(managerRoleIds);
                    
                    var allEmployees = await _unitOfWork.Employees.GetAllAsync();
                    var employeeAccountIds = allEmployees.Select(e => e.AccountId).ToHashSet();
                    
                    var managerAccounts = await _unitOfWork.Accounts.FindAsync(a => 
                        managerAccountIds.Contains(a.AccountId) && 
                        employeeAccountIds.Contains(a.AccountId) && 
                        !a.IsLocked);

                    if (!managerAccounts.Any())
                    {
                        Console.WriteLine("[EmailService] No manager accounts found to send notification");
                        return;
                    }

                    // 6. Đọc template
                    var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        "TemplateEmail", "PaymentConfirmationRequestTemplate.html");

                    string template;
                    using (var reader = new StreamReader(templatePath))
                    {
                        template = await reader.ReadToEndAsync();
                    }

                    // 7. Tính toán và format dữ liệu
                    var numberOfNights = (booking.CheckOutDate - booking.CheckInDate).Days;
                    var confirmationTime = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss");

                    // 8. Replace placeholders
                    var body = template
                        .Replace("{{BookingId}}", bookingId.ToString())
                        .Replace("{{CustomerName}}", customer.FullName ?? "N/A")
                        .Replace("{{CustomerPhone}}", customer.PhoneNumber ?? "N/A")
                        .Replace("{{CustomerEmail}}", customerEmail)
                        .Replace("{{RoomNames}}", roomNames)
                        .Replace("{{CheckInDate}}", booking.CheckInDate.ToString("dd/MM/yyyy HH:mm"))
                        .Replace("{{CheckOutDate}}", booking.CheckOutDate.ToString("dd/MM/yyyy HH:mm"))
                        .Replace("{{NumberOfNights}}", numberOfNights.ToString())
                        .Replace("{{TotalAmount}}", booking.TotalAmount.ToString("N0"))
                        .Replace("{{DepositAmount}}", booking.DepositAmount.ToString("N0"))
                        .Replace("{{ConfirmationTime}}", confirmationTime);

                    // 9. Gửi email đến tất cả managers
                    foreach (var manager in managerAccounts)
                    {
                        await SendEmail(manager.Email, $"✓ Thông báo thanh toán cọc - Booking #{bookingId}", body);
                    }
                    
                    Console.WriteLine($"[EmailService] Payment confirmation request sent to manager for booking {bookingId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EmailService] Error sending payment confirmation request email: {ex.Message}");
                }
            });

            // Return completed task immediately - không chờ email gửi xong
            return Task.CompletedTask;
        }
    }
}
