using AppBackend.BusinessObjects.Enums;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels.BookingModel;
using AppBackend.Services.Helpers;
using AppBackend.Services.MessageQueue;
using AppBackend.Services.Services.BookingServices;
using AppBackend.Services.Services.Email;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Net.payOS;
using Net.payOS.Types;
using Xunit;

namespace AppBackend.Tests.Services;

/// <summary>
/// Unit tests cho BookingService - tập trung vào chức năng tạo account guest và webhook email
/// </summary>
public class BookingServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<CacheHelper> _cacheHelperMock;
    private readonly Mock<IBookingQueueService> _queueServiceMock;
    private readonly Mock<PayOS> _payOSMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<AccountHelper> _accountHelperMock;
    private readonly BookingService _bookingService;

    public BookingServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cacheHelperMock = new Mock<CacheHelper>();
        _queueServiceMock = new Mock<IBookingQueueService>();
        _payOSMock = new Mock<PayOS>("clientId", "apiKey", "checksumKey");
        _configurationMock = new Mock<IConfiguration>();
        _emailServiceMock = new Mock<IEmailService>();
        _accountHelperMock = new Mock<AccountHelper>(new Mock<IConfiguration>().Object);

        _bookingService = new BookingService(
            _unitOfWorkMock.Object,
            _cacheHelperMock.Object,
            _queueServiceMock.Object,
            _payOSMock.Object,
            _configurationMock.Object,
            _emailServiceMock.Object,
            _accountHelperMock.Object
        );
    }

    #region CreateGuestBookingAsync Tests

    [Fact]
    public async Task CreateGuestBookingAsync_NewGuest_ShouldCreateAccountWithDefaultPassword()
    {
        // Arrange
        var request = new CreateGuestBookingRequest
        {
            FullName = "Nguyen Van A",
            Email = "newguest@example.com",
            PhoneNumber = "0901234567",
            RoomTypes = new List<RoomTypeQuantityRequest>
            {
                new() { RoomTypeId = 1, Quantity = 1 }
            },
            CheckInDate = DateTime.UtcNow.AddDays(1),
            CheckOutDate = DateTime.UtcNow.AddDays(3)
        };

        // Mock: Email chưa tồn tại
        _unitOfWorkMock.Setup(u => u.Accounts.GetByEmailAsync(request.Email))
            .ReturnsAsync((Account?)null);

        // Mock: Hash password
        _accountHelperMock.Setup(a => a.HashPassword("123456"))
            .Returns("hashed_password_123456");

        // Mock: User role
        var userRole = new Role { RoleId = 1, RoleName = "User", RoleValue = "User" };
        _unitOfWorkMock.Setup(u => u.Roles.GetRoleByRoleValueAsync(RoleEnums.User.ToString()))
            .ReturnsAsync(userRole);

        // Mock: Add account
        var newAccount = new Account();
        _unitOfWorkMock.Setup(u => u.Accounts.AddAsync(It.IsAny<Account>()))
            .Callback<Account>(acc =>
            {
                acc.AccountId = 100;
                newAccount = acc;
            })
            .Returns(Task.CompletedTask);

        // Mock: Room Type
        var roomType = new RoomType { RoomTypeId = 1, TypeName = "Deluxe", BasePriceNight = 1000000 };
        _unitOfWorkMock.Setup(u => u.RoomTypes.GetByIdAsync(1))
            .ReturnsAsync(roomType);

        // Mock: Available rooms
        var room = new Room { RoomId = 1, RoomName = "D101", RoomTypeId = 1 };
        _unitOfWorkMock.Setup(u => u.Rooms.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Room, bool>>>()))
            .ReturnsAsync(new List<Room> { room });

        // Mock: Booking rooms - empty
        _unitOfWorkMock.Setup(u => u.BookingRooms.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BookingRoom, bool>>>()))
            .ReturnsAsync(new List<BookingRoom>());

        // Mock: Cache lock
        _cacheHelperMock.Setup(c => c.TryAcquireLock(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        // Mock: Common codes
        var unpaidStatus = new CommonCode { CodeId = 1, CodeName = "Unpaid" };
        var onlineType = new CommonCode { CodeId = 2, CodeName = "Online" };
        _unitOfWorkMock.Setup(u => u.CommonCodes.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CommonCode, bool>>>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<CommonCode, bool>> predicate) =>
            {
                // Simulate finding status codes
                return new List<CommonCode> { unpaidStatus, onlineType };
            });

        // Mock: PayOS
        var paymentLink = new CreatePaymentResult { checkoutUrl = "https://payos.vn/checkout/123" };
        _payOSMock.Setup(p => p.createPaymentLink(It.IsAny<PaymentData>()))
            .ReturnsAsync(paymentLink);

        // Mock: Configuration
        _configurationMock.Setup(c => c["PayOS:ReturnUrl"]).Returns("http://localhost:3000/payment/callback");
        _configurationMock.Setup(c => c["PayOS:CancelUrl"]).Returns("http://localhost:3000/payment/cancel");

        // Act
        var result = await _bookingService.CreateGuestBookingAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(StatusCodes.Status201Created);

        // Verify account được tạo với password "123456" đã hash
        _accountHelperMock.Verify(a => a.HashPassword("123456"), Times.Once);
        _unitOfWorkMock.Verify(u => u.Accounts.AddAsync(It.Is<Account>(acc =>
            acc.Email == request.Email &&
            acc.Username == request.Email &&
            acc.PasswordHash == "hashed_password_123456" &&
            acc.IsLocked == false
        )), Times.Once);

        // Verify role được assign
        _unitOfWorkMock.Verify(u => u.Accounts.AddAccountRoleAsync(It.Is<AccountRole>(ar =>
            ar.RoleId == userRole.RoleId
        )), Times.Once);

        // Verify customer được tạo với AccountId
        _unitOfWorkMock.Verify(u => u.Customers.AddAsync(It.Is<Customer>(c =>
            c.AccountId == 100 &&
            c.FullName == request.FullName &&
            c.PhoneNumber == request.PhoneNumber
        )), Times.Once);

        // Verify password được lưu vào cache
        _cacheHelperMock.Verify(c => c.Set(
            CachePrefix.BookingPayment,
            It.IsAny<string>(),
            It.Is<object>(obj => obj.ToString()!.Contains("NewAccountPassword"))
        ), Times.Once);
    }

    [Fact]
    public async Task CreateGuestBookingAsync_ExistingEmail_ShouldUseExistingAccount()
    {
        // Arrange
        var request = new CreateGuestBookingRequest
        {
            FullName = "Nguyen Van B",
            Email = "existing@example.com",
            PhoneNumber = "0907654321",
            RoomTypes = new List<RoomTypeQuantityRequest>
            {
                new() { RoomTypeId = 1, Quantity = 1 }
            },
            CheckInDate = DateTime.UtcNow.AddDays(1),
            CheckOutDate = DateTime.UtcNow.AddDays(3)
        };

        // Mock: Email đã tồn tại
        var existingAccount = new Account
        {
            AccountId = 50,
            Email = request.Email,
            Username = request.Email,
            PasswordHash = "existing_hashed_password"
        };
        _unitOfWorkMock.Setup(u => u.Accounts.GetByEmailAsync(request.Email))
            .ReturnsAsync(existingAccount);

        // Mock: Customer đã tồn tại
        var existingCustomer = new Customer
        {
            CustomerId = 25,
            AccountId = 50,
            FullName = "Old Name",
            PhoneNumber = "0900000000"
        };
        _unitOfWorkMock.Setup(u => u.Customers.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Customer, bool>>>()))
            .ReturnsAsync(new List<Customer> { existingCustomer });

        // Mock: Room Type và các dependencies khác (giống test trên)
        SetupCommonMocks();

        // Act
        var result = await _bookingService.CreateGuestBookingAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify KHÔNG tạo account mới
        _accountHelperMock.Verify(a => a.HashPassword(It.IsAny<string>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.Accounts.AddAsync(It.IsAny<Account>()), Times.Never);

        // Verify customer được update thông tin
        _unitOfWorkMock.Verify(u => u.Customers.UpdateAsync(It.Is<Customer>(c =>
            c.CustomerId == existingCustomer.CustomerId &&
            c.FullName == request.FullName &&
            c.PhoneNumber == request.PhoneNumber
        )), Times.Once);

        // Verify password KHÔNG được lưu vào cache (vì account đã tồn tại)
        _cacheHelperMock.Verify(c => c.Set(
            CachePrefix.BookingPayment,
            It.IsAny<string>(),
            It.Is<object>(obj => !obj.ToString()!.Contains("NewAccountPassword") || obj.ToString()!.Contains("null"))
        ), Times.Once);
    }

    [Fact]
    public async Task CreateGuestBookingAsync_InvalidInput_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateGuestBookingRequest
        {
            FullName = "", // Empty name
            Email = "test@example.com",
            PhoneNumber = "0901234567",
            RoomTypes = new List<RoomTypeQuantityRequest>(),
            CheckInDate = DateTime.UtcNow.AddDays(1),
            CheckOutDate = DateTime.UtcNow.AddDays(3)
        };

        // Act
        var result = await _bookingService.CreateGuestBookingAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        result.Message.Should().Contain("đầy đủ");
    }

    #endregion

    #region HandlePayOSWebhookAsync Tests

    [Fact]
    public async Task HandlePayOSWebhookAsync_ValidWebhook_ShouldSendEmailWithPassword()
    {
        // Arrange
        var bookingId = 123;
        var orderCode = 250101120000L;
        var newPassword = "123456";

        var webhookRequest = new PayOSWebhookRequest
        {
            Code = "00",
            Desc = "success",
            Success = true,
            Signature = "valid_signature",
            Data = new PayOSWebhookData
            {
                OrderCode = orderCode.ToString(),
                Amount = 3000000,
                Description = "Booking payment",
                Reference = "REF123",
                TransactionDateTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                AccountNumber = "0399609015",
                Code = "00",
                Desc = "success"
            }
        };

        // Mock: Verify webhook data
        var webhookData = new WebhookData(
            orderCode: orderCode,
            amount: 3000000,
            description: "Booking payment",
            accountNumber: "0399609015",
            reference: "REF123",
            transactionDateTime: DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            currency: "VND",
            paymentLinkId: "link123",
            code: "00",
            desc: "success",
            counterAccountBankId: null,
            counterAccountBankName: null,
            counterAccountName: null,
            counterAccountNumber: null,
            virtualAccountName: null,
            virtualAccountNumber: null
        );

        _payOSMock.Setup(p => p.verifyPaymentWebhookData(It.IsAny<WebhookType>()))
            .Returns(webhookData);

        // Mock: Find booking by order code
        var transaction = new BusinessObjects.Models.Transaction
        {
            TransactionId = 1,
            BookingId = bookingId,
            OrderCode = orderCode.ToString()
        };
        _unitOfWorkMock.Setup(u => u.Transactions.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BusinessObjects.Models.Transaction, bool>>>()))
            .ReturnsAsync(new List<BusinessObjects.Models.Transaction> { transaction });

        // Mock: Booking
        var booking = new Booking
        {
            BookingId = bookingId,
            CustomerId = 1,
            TotalAmount = 10000000,
            DepositAmount = 3000000,
            CheckInDate = DateTime.UtcNow.AddDays(1),
            CheckOutDate = DateTime.UtcNow.AddDays(3)
        };
        _unitOfWorkMock.Setup(u => u.Bookings.GetByIdAsync(bookingId))
            .ReturnsAsync(booking);

        // Mock: Common codes
        var paidStatus = new CommonCode { CodeId = 10, CodeName = "Paid" };
        _unitOfWorkMock.Setup(u => u.CommonCodes.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CommonCode, bool>>>()))
            .ReturnsAsync(new List<CommonCode> { paidStatus });

        // Mock: Cache - lưu password
        var paymentInfo = new
        {
            BookingId = bookingId,
            LockId = "lock123",
            RoomIds = new[] { 1, 2 },
            CheckInDate = DateTime.UtcNow.AddDays(1),
            CheckOutDate = DateTime.UtcNow.AddDays(3),
            NewAccountPassword = newPassword
        };
        _cacheHelperMock.Setup(c => c.Get<dynamic>(CachePrefix.BookingPayment, bookingId.ToString()))
            .Returns(paymentInfo);

        // Act
        var result = await _bookingService.HandlePayOSWebhookAsync(webhookRequest);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(StatusCodes.Status200OK);

        // Verify email được gửi với password
        _emailServiceMock.Verify(e => e.SendBookingConfirmationEmailAsync(
            bookingId,
            newPassword // Password phải được truyền vào
        ), Times.Once);

        // Verify payment status được update
        _unitOfWorkMock.Verify(u => u.Bookings.UpdateAsync(It.Is<Booking>(b =>
            b.BookingId == bookingId &&
            b.DepositStatusId == paidStatus.CodeId
        )), Times.Once);

        // Verify cache được xóa
        _cacheHelperMock.Verify(c => c.Remove(CachePrefix.BookingPayment, bookingId.ToString()), Times.Once);
    }

    [Fact]
    public async Task HandlePayOSWebhookAsync_NoPassword_ShouldSendEmailWithoutPassword()
    {
        // Arrange
        var bookingId = 456;
        var orderCode = 250101130000L;

        var webhookRequest = new PayOSWebhookRequest
        {
            Code = "00",
            Desc = "success",
            Success = true,
            Signature = "valid_signature",
            Data = new PayOSWebhookData
            {
                OrderCode = orderCode.ToString(),
                Amount = 2000000,
                Code = "00"
            }
        };

        SetupWebhookMocks(bookingId, orderCode, newPassword: null);

        // Act
        var result = await _bookingService.HandlePayOSWebhookAsync(webhookRequest);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify email được gửi KHÔNG có password
        _emailServiceMock.Verify(e => e.SendBookingConfirmationEmailAsync(
            bookingId,
            null // Không có password
        ), Times.Once);
    }

    #endregion

    #region Helper Methods

    private void SetupCommonMocks()
    {
        // Room Type
        var roomType = new RoomType { RoomTypeId = 1, TypeName = "Deluxe", BasePriceNight = 1000000 };
        _unitOfWorkMock.Setup(u => u.RoomTypes.GetByIdAsync(1)).ReturnsAsync(roomType);

        // Available rooms
        var room = new Room { RoomId = 1, RoomName = "D101", RoomTypeId = 1 };
        _unitOfWorkMock.Setup(u => u.Rooms.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Room, bool>>>()))
            .ReturnsAsync(new List<Room> { room });

        // Empty booking rooms
        _unitOfWorkMock.Setup(u => u.BookingRooms.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BookingRoom, bool>>>()))
            .ReturnsAsync(new List<BookingRoom>());

        // Cache lock
        _cacheHelperMock.Setup(c => c.TryAcquireLock(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        // Common codes
        var unpaidStatus = new CommonCode { CodeId = 1, CodeName = "Unpaid" };
        var onlineType = new CommonCode { CodeId = 2, CodeName = "Online" };
        _unitOfWorkMock.Setup(u => u.CommonCodes.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CommonCode, bool>>>()))
            .ReturnsAsync(new List<CommonCode> { unpaidStatus, onlineType });

        // PayOS
        var paymentLink = new CreatePaymentResult { checkoutUrl = "https://payos.vn/checkout/123" };
        _payOSMock.Setup(p => p.createPaymentLink(It.IsAny<PaymentData>())).ReturnsAsync(paymentLink);

        // Configuration
        _configurationMock.Setup(c => c["PayOS:ReturnUrl"]).Returns("http://localhost:3000/payment/callback");
        _configurationMock.Setup(c => c["PayOS:CancelUrl"]).Returns("http://localhost:3000/payment/cancel");
    }

    private void SetupWebhookMocks(int bookingId, long orderCode, string? newPassword)
    {
        // Webhook data
        var webhookData = new WebhookData(
            orderCode: orderCode,
            amount: 2000000,
            description: "Payment",
            accountNumber: "123",
            reference: "REF",
            transactionDateTime: DateTime.UtcNow.ToString(),
            currency: "VND",
            paymentLinkId: "link",
            code: "00",
            desc: "success",
            counterAccountBankId: null,
            counterAccountBankName: null,
            counterAccountName: null,
            counterAccountNumber: null,
            virtualAccountName: null,
            virtualAccountNumber: null
        );
        _payOSMock.Setup(p => p.verifyPaymentWebhookData(It.IsAny<WebhookType>())).Returns(webhookData);

        // Transaction
        var transaction = new BusinessObjects.Models.Transaction { BookingId = bookingId, OrderCode = orderCode.ToString() };
        _unitOfWorkMock.Setup(u => u.Transactions.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BusinessObjects.Models.Transaction, bool>>>()))
            .ReturnsAsync(new List<BusinessObjects.Models.Transaction> { transaction });

        // Booking
        var booking = new Booking { BookingId = bookingId, CustomerId = 1, TotalAmount = 5000000, DepositAmount = 2000000 };
        _unitOfWorkMock.Setup(u => u.Bookings.GetByIdAsync(bookingId)).ReturnsAsync(booking);

        // Common codes
        var paidStatus = new CommonCode { CodeId = 10, CodeName = "Paid" };
        _unitOfWorkMock.Setup(u => u.CommonCodes.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CommonCode, bool>>>()))
            .ReturnsAsync(new List<CommonCode> { paidStatus });

        // Cache
        var paymentInfo = new { BookingId = bookingId, LockId = "lock", RoomIds = new[] { 1 }, NewAccountPassword = newPassword };
        _cacheHelperMock.Setup(c => c.Get<dynamic>(CachePrefix.BookingPayment, bookingId.ToString())).Returns(paymentInfo);
    }

    #endregion
}

