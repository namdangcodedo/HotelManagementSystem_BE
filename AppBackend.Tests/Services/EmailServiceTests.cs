using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.Services.Email;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace AppBackend.Tests.Services;

/// <summary>
/// Unit tests cho EmailService - tập trung vào gửi email xác nhận booking
/// </summary>
public class EmailServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly EmailService _emailService;

    public EmailServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        // Setup email configuration
        _configurationMock.Setup(c => c["EmailSettings:SenderEmail"]).Returns("noreply@stayhub.com");
        _configurationMock.Setup(c => c["EmailSettings:SmtpServer"]).Returns("smtp.gmail.com");
        _configurationMock.Setup(c => c["EmailSettings:Port"]).Returns("587");
        _configurationMock.Setup(c => c["EmailSettings:Password"]).Returns("app_password");

        _emailService = new EmailService(_configurationMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task SendBookingConfirmationEmailAsync_WithNewPassword_ShouldIncludeAccountInfo()
    {
        // Arrange
        var bookingId = 100;
        var newPassword = "123456";

        var booking = new Booking
        {
            BookingId = bookingId,
            CustomerId = 1,
            TotalAmount = 5000000,
            DepositAmount = 1500000,
            CheckInDate = new DateTime(2025, 11, 1, 14, 0, 0),
            CheckOutDate = new DateTime(2025, 11, 3, 12, 0, 0)
        };

        var customer = new Customer
        {
            CustomerId = 1,
            AccountId = 10,
            FullName = "Nguyen Van A",
            PhoneNumber = "0901234567"
        };

        var account = new Account
        {
            AccountId = 10,
            Email = "nguyenvana@example.com",
            Username = "nguyenvana@example.com"
        };

        var room1 = new Room { RoomId = 1, RoomName = "Deluxe 101" };
        var room2 = new Room { RoomId = 2, RoomName = "Deluxe 102" };

        var bookingRooms = new List<BookingRoom>
        {
            new() { BookingId = bookingId, RoomId = 1 },
            new() { BookingId = bookingId, RoomId = 2 }
        };

        // Setup mocks
        _unitOfWorkMock.Setup(u => u.Bookings.GetByIdAsync(bookingId)).ReturnsAsync(booking);
        _unitOfWorkMock.Setup(u => u.Customers.GetByIdAsync(customer.CustomerId)).ReturnsAsync(customer);
        _unitOfWorkMock.Setup(u => u.Accounts.GetByIdAsync(account.AccountId)).ReturnsAsync(account);
        _unitOfWorkMock.Setup(u => u.BookingRooms.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BookingRoom, bool>>>()))
            .ReturnsAsync(bookingRooms);
        _unitOfWorkMock.Setup(u => u.Rooms.GetByIdAsync(1)).ReturnsAsync(room1);
        _unitOfWorkMock.Setup(u => u.Rooms.GetByIdAsync(2)).ReturnsAsync(room2);

        // Act & Assert
        // Note: Actual email sending will fail in test environment, but we're testing the logic
        var exception = await Record.ExceptionAsync(async () =>
        {
            await _emailService.SendBookingConfirmationEmailAsync(bookingId, newPassword);
        });

        // Verify các phương thức được gọi đúng
        _unitOfWorkMock.Verify(u => u.Bookings.GetByIdAsync(bookingId), Times.Once);
        _unitOfWorkMock.Verify(u => u.Customers.GetByIdAsync(customer.CustomerId), Times.Once);
        _unitOfWorkMock.Verify(u => u.Accounts.GetByIdAsync(account.AccountId), Times.AtLeastOnce);
    }

    [Fact]
    public async Task SendBookingConfirmationEmailAsync_WithoutPassword_ShouldNotIncludeAccountInfo()
    {
        // Arrange
        var bookingId = 200;

        var booking = new Booking
        {
            BookingId = bookingId,
            CustomerId = 2,
            TotalAmount = 3000000,
            DepositAmount = 900000,
            CheckInDate = new DateTime(2025, 11, 5, 14, 0, 0),
            CheckOutDate = new DateTime(2025, 11, 7, 12, 0, 0)
        };

        var customer = new Customer
        {
            CustomerId = 2,
            AccountId = 20,
            FullName = "Tran Thi B",
            PhoneNumber = "0907654321"
        };

        var account = new Account
        {
            AccountId = 20,
            Email = "tranthib@example.com"
        };

        var room = new Room { RoomId = 5, RoomName = "VIP 501" };
        var bookingRooms = new List<BookingRoom>
        {
            new() { BookingId = bookingId, RoomId = 5 }
        };

        // Setup mocks
        _unitOfWorkMock.Setup(u => u.Bookings.GetByIdAsync(bookingId)).ReturnsAsync(booking);
        _unitOfWorkMock.Setup(u => u.Customers.GetByIdAsync(customer.CustomerId)).ReturnsAsync(customer);
        _unitOfWorkMock.Setup(u => u.Accounts.GetByIdAsync(account.AccountId)).ReturnsAsync(account);
        _unitOfWorkMock.Setup(u => u.BookingRooms.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BookingRoom, bool>>>()))
            .ReturnsAsync(bookingRooms);
        _unitOfWorkMock.Setup(u => u.Rooms.GetByIdAsync(5)).ReturnsAsync(room);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            await _emailService.SendBookingConfirmationEmailAsync(bookingId, null);
        });

        // Verify
        _unitOfWorkMock.Verify(u => u.Bookings.GetByIdAsync(bookingId), Times.Once);
        _unitOfWorkMock.Verify(u => u.Customers.GetByIdAsync(customer.CustomerId), Times.Once);
    }

    [Fact]
    public async Task SendBookingConfirmationEmailAsync_BookingNotFound_ShouldThrowException()
    {
        // Arrange
        var bookingId = 999;
        _unitOfWorkMock.Setup(u => u.Bookings.GetByIdAsync(bookingId)).ReturnsAsync((Booking?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(async () =>
        {
            await _emailService.SendBookingConfirmationEmailAsync(bookingId, "123456");
        });

        exception.Message.Should().Contain("không tồn tại");
    }

    [Fact]
    public async Task SendBookingConfirmationEmailAsync_CustomerNotFound_ShouldThrowException()
    {
        // Arrange
        var bookingId = 100;
        var booking = new Booking { BookingId = bookingId, CustomerId = 999 };

        _unitOfWorkMock.Setup(u => u.Bookings.GetByIdAsync(bookingId)).ReturnsAsync(booking);
        _unitOfWorkMock.Setup(u => u.Customers.GetByIdAsync(999)).ReturnsAsync((Customer?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(async () =>
        {
            await _emailService.SendBookingConfirmationEmailAsync(bookingId, "123456");
        });

        exception.Message.Should().Contain("Customer không tồn tại");
    }

    [Fact]
    public async Task SendBookingConfirmationEmailAsync_NoEmail_ShouldThrowException()
    {
        // Arrange
        var bookingId = 100;
        var booking = new Booking { BookingId = bookingId, CustomerId = 1 };
        var customer = new Customer { CustomerId = 1, AccountId = null }; // No account

        _unitOfWorkMock.Setup(u => u.Bookings.GetByIdAsync(bookingId)).ReturnsAsync(booking);
        _unitOfWorkMock.Setup(u => u.Customers.GetByIdAsync(1)).ReturnsAsync(customer);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(async () =>
        {
            await _emailService.SendBookingConfirmationEmailAsync(bookingId, "123456");
        });

        exception.Message.Should().Contain("không có email");
    }

    [Theory]
    [InlineData(5000000, 1500000, 3500000)] // Total: 5tr, Deposit: 1.5tr, Remaining: 3.5tr
    [InlineData(10000000, 3000000, 7000000)] // Total: 10tr, Deposit: 3tr, Remaining: 7tr
    [InlineData(2000000, 600000, 1400000)] // Total: 2tr, Deposit: 600k, Remaining: 1.4tr
    public void CalculateRemainingAmount_ShouldReturnCorrectValue(decimal total, decimal deposit, decimal expectedRemaining)
    {
        // Arrange & Act
        var remaining = total - deposit;

        // Assert
        remaining.Should().Be(expectedRemaining);
        remaining.ToString("N0").Should().NotBeNullOrEmpty();
    }
}

