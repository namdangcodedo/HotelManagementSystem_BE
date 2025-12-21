using AppBackend.Repositories.UnitOfWork;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public EmailService(
            IConfiguration configuration, 
            IUnitOfWork unitOfWork, 
            BookingTokenHelper bookingTokenHelper, 
            AccountTokenHelper accountTokenHelper,
            IServiceScopeFactory serviceScopeFactory)
        {
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _bookingTokenHelper = bookingTokenHelper;
            _accountTokenHelper = accountTokenHelper;
            _serviceScopeFactory = serviceScopeFactory;
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
        
        /// <summary>
        /// Gửi email thông báo khách đã thanh toán - Manager cần xác nhận
        /// </summary>
        public Task SendPaymentConfirmationRequestEmailToManagerAsync(int bookingId)
        {
            // Fire-and-forget: chạy background, không block caller
            // QUAN TRỌNG: Phải tạo scope mới vì DbContext của request gốc sẽ bị dispose
            _ = Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine($"[EmailService] === START SendPaymentConfirmationRequestEmailToManagerAsync for booking {bookingId} ===");
                    
                    // Tạo scope mới để có DbContext mới, tránh lỗi "Cannot access a disposed context"
                    using var scope = _serviceScopeFactory.CreateScope();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    
                    // 1. Lấy thông tin booking
                    var booking = await unitOfWork.Bookings.GetByIdAsync(bookingId);
                    if (booking == null)
                    {
                        Console.WriteLine($"[EmailService] ERROR: Booking with ID {bookingId} not found");
                        return;
                    }
                    Console.WriteLine($"[EmailService] Step 1: Found booking {bookingId}, CustomerId: {booking.CustomerId}");

                    // 2. Lấy thông tin customer
                    var customer = await unitOfWork.Customers.GetByIdAsync(booking.CustomerId);
                    if (customer == null)
                    {
                        Console.WriteLine($"[EmailService] ERROR: Customer with ID {booking.CustomerId} not found");
                        return;
                    }
                    Console.WriteLine($"[EmailService] Step 2: Found customer: {customer.FullName}, AccountId: {customer.AccountId}");

                    // 3. Lấy email customer (nếu có)
                    string customerEmail = "N/A";
                    if (customer.AccountId.HasValue)
                    {
                        var customerAccount = await unitOfWork.Accounts.GetByIdAsync(customer.AccountId.Value);
                        customerEmail = customerAccount?.Email ?? "N/A";
                    }
                    Console.WriteLine($"[EmailService] Step 3: Customer email: {customerEmail}");

                    // 4. Lấy thông tin phòng
                    var bookingRooms = await unitOfWork.BookingRooms.FindAsync(br => br.BookingId == bookingId);
                    var roomIds = bookingRooms.Select(br => br.RoomId).ToList();
                    var rooms = await unitOfWork.Rooms.FindAsync(r => roomIds.Contains(r.RoomId));
                    var roomNames = string.Join(", ", rooms.Select(r => r.RoomName));
                    Console.WriteLine($"[EmailService] Step 4: Found {roomIds.Count} rooms: {roomNames}");

                    // 5. Lấy danh sách email Manager/Admin
                    var managerRoles = new[] { "Manager", "Admin", "Receptionist" };
                    var allRoles = await unitOfWork.Roles.GetAllAsync();
                    Console.WriteLine($"[EmailService] Step 5a: Total roles in DB: {allRoles.Count()}");
                    
                    var managerRoleIds = allRoles.Where(r => managerRoles.Contains(r.RoleValue)).Select(r => r.RoleId).ToList();
                    Console.WriteLine($"[EmailService] Step 5b: Manager role IDs: [{string.Join(", ", managerRoleIds)}]");

                    var managerAccountIds = await unitOfWork.Roles.GetAccountIdsByRoleIdsAsync(managerRoleIds);
                    Console.WriteLine($"[EmailService] Step 5c: Manager account IDs: [{string.Join(", ", managerAccountIds)}]");
                    
                    var allEmployees = await unitOfWork.Employees.GetAllAsync();
                    var employeeAccountIds = allEmployees.Select(e => e.AccountId).ToHashSet();
                    Console.WriteLine($"[EmailService] Step 5d: Employee account IDs: [{string.Join(", ", employeeAccountIds)}]");
                    
                    var managerAccounts = await unitOfWork.Accounts.FindAsync(a => 
                        managerAccountIds.Contains(a.AccountId) && 
                        employeeAccountIds.Contains(a.AccountId) && 
                        !a.IsLocked);
                    Console.WriteLine($"[EmailService] Step 5e: Found {managerAccounts.Count()} manager accounts to notify");

                    if (!managerAccounts.Any())
                    {
                        Console.WriteLine("[EmailService] WARNING: No manager accounts found to send notification");
                        return;
                    }

                    // 6. Đọc template
                    var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        "TemplateEmail", "PaymentConfirmationRequestTemplate.html");
                    Console.WriteLine($"[EmailService] Step 6a: Template path: {templatePath}");
                    
                    if (!File.Exists(templatePath))
                    {
                        Console.WriteLine($"[EmailService] ERROR: Template file NOT FOUND at {templatePath}");
                        return;
                    }

                    string template;
                    using (var reader = new StreamReader(templatePath))
                    {
                        template = await reader.ReadToEndAsync();
                    }
                    Console.WriteLine($"[EmailService] Step 6b: Template loaded, length: {template.Length} chars");

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
                    Console.WriteLine($"[EmailService] Step 8: Body prepared");

                    // 9. Gửi email đến tất cả managers
                    foreach (var manager in managerAccounts)
                    {
                        try
                        {
                            Console.WriteLine($"[EmailService] Step 9: Sending email to {manager.Email}...");
                            await SendEmail(manager.Email, $"✓ Thông báo thanh toán cọc - Booking #{bookingId}", body);
                            Console.WriteLine($"[EmailService] Step 9: SUCCESS - Email sent to {manager.Email}");
                        }
                        catch (Exception emailEx)
                        {
                            Console.WriteLine($"[EmailService] Step 9: FAILED to send to {manager.Email}: {emailEx.Message}");
                        }
                    }
                    
                    Console.WriteLine($"[EmailService] === END SendPaymentConfirmationRequestEmailToManagerAsync for booking {bookingId} ===");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EmailService] FATAL ERROR: {ex.Message}");
                    Console.WriteLine($"[EmailService] Stack trace: {ex.StackTrace}");
                }
            });

            // Return completed task immediately - không chờ email gửi xong
            return Task.CompletedTask;
        }
    }
}
