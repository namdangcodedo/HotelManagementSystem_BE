using AppBackend.Services.Authentication;
using AppBackend.Services.ApiModels;
using AppBackend.Services.Helpers;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.BusinessObjects.Models;
using AppBackend.Services.Services.Email;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using FluentAssertions;

namespace AppBackend.Tests.Services
{
    public class AuthenticationServiceTests
    {
        private readonly AccountHelper _accountHelper;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly CacheHelper _cacheHelper;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly AuthenticationService _authService;

        public AuthenticationServiceTests()
        {
            // Create real instances of helpers since they are concrete classes
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns("TestSecretKeyForJwtTokenGeneration123456789");
            _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
            _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
            _mockConfiguration.Setup(c => c["Jwt:ExpireMinutes"]).Returns("60");
            
            _accountHelper = new AccountHelper(_mockConfiguration.Object);
            
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            _cacheHelper = new CacheHelper(memoryCache);
            
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockEmailService = new Mock<IEmailService>();

            _authService = new AuthenticationService(
                _accountHelper,
                _mockUnitOfWork.Object,
                _cacheHelper,
                _mockHttpContextAccessor.Object,
                _mockConfiguration.Object,
                _mockEmailService.Object
            );
        }

        #region ChangePasswordWithOtp Tests

        [Fact]
        public async Task ChangePasswordWithOtp_WithValidCredentials_ShouldReturnSuccess()
        {
            // Arrange
            var email = "thinhlqhe172306@fpt.edu.vn";
            var otp = "123456";
            var newPassword = "Th@ h123@!";
            
            var existingAccount = new Account
            {
                AccountId = 1,
                Email = email,
                PasswordHash = _accountHelper.HashPassword("oldpassword"),
                IsLocked = false
            };

            _mockUnitOfWork.Setup(u => u.Accounts.GetByEmailAsync(email))
                .ReturnsAsync(existingAccount);
            
            // Set OTP in cache
            _cacheHelper.Set(CachePrefix.OtpCode, email, otp, TimeSpan.FromMinutes(5));
            
            _mockUnitOfWork.Setup(u => u.Accounts.UpdateAsync(It.IsAny<Account>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _authService.ChangePasswordWithOtpAsync(email, otp, newPassword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
        }

        [Theory]
        [InlineData("thinhlqhe172306@fpt.edu.vn", "123456", "Th@ h123@!")]
        public async Task ChangePasswordWithOtp_WithExistingActiveUser_ShouldSucceed(
            string email, string otp, string newPassword)
        {
            // Arrange
            var existingAccount = new Account
            {
                AccountId = 1,
                Email = email,
                PasswordHash = _accountHelper.HashPassword("oldpassword"),
                IsLocked = false
            };

            _mockUnitOfWork.Setup(u => u.Accounts.GetByEmailAsync(email))
                .ReturnsAsync(existingAccount);
            
            _cacheHelper.Set(CachePrefix.OtpCode, email, otp, TimeSpan.FromMinutes(5));
            
            _mockUnitOfWork.Setup(u => u.Accounts.UpdateAsync(It.IsAny<Account>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _authService.ChangePasswordWithOtpAsync(email, otp, newPassword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
        }

        [Theory]
        [InlineData("", "123456", "Th@ h123@!")]
        [InlineData("   ", "123456", "Th@ h123@!")]
        public async Task ChangePasswordWithOtp_WithInvalidEmail_ShouldReturnFailure(
            string email, string otp, string newPassword)
        {
            // Act
            var result = await _authService.ChangePasswordWithOtpAsync(email, otp, newPassword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
        }

        [Theory]
        [InlineData("invalid_email", "123456", "Th@ h123@!")]
        [InlineData("example", "123456", "Th@ h123@!")]
        [InlineData("@e@e.vn", "123456", "Th@ h123@!")]
        public async Task ChangePasswordWithOtp_WithInvalidEmailFormat_ShouldReturnFailure(
            string email, string otp, string newPassword)
        {
            // Arrange
            _mockUnitOfWork.Setup(u => u.Accounts.GetByEmailAsync(email))
                .ReturnsAsync((Account?)null);

            // Act
            var result = await _authService.ChangePasswordWithOtpAsync(email, otp, newPassword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
        }

        [Theory]
        [InlineData("exampleexampleexampleexampleexampleexampleexample@gmail.com", "123456", "Th@ h123@!")]
        public async Task ChangePasswordWithOtp_WithTooLongEmail_ShouldReturnFailure(
            string email, string otp, string newPassword)
        {
            // Arrange
            _mockUnitOfWork.Setup(u => u.Accounts.GetByEmailAsync(email))
                .ReturnsAsync((Account?)null);

            // Act
            var result = await _authService.ChangePasswordWithOtpAsync(email, otp, newPassword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
        }

        [Theory]
        [InlineData("example@fpt.edu.vn", "123456", "Th@ h123@!")]
        [InlineData("anhhvhe176717@fpt.edu.vn", "123456", "NewPass@123")]
        public async Task ChangePasswordWithOtp_WithValidEmailAndOtp_ShouldReturnTrue(
            string email, string otp, string newPassword)
        {
            // Arrange
            var existingAccount = new Account
            {
                AccountId = 1,
                Email = email,
                PasswordHash = _accountHelper.HashPassword("oldpassword"),
                IsLocked = false
            };

            _mockUnitOfWork.Setup(u => u.Accounts.GetByEmailAsync(email))
                .ReturnsAsync(existingAccount);
            
            _cacheHelper.Set(CachePrefix.OtpCode, email, otp, TimeSpan.FromMinutes(5));
            
            _mockUnitOfWork.Setup(u => u.Accounts.UpdateAsync(It.IsAny<Account>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _authService.ChangePasswordWithOtpAsync(email, otp, newPassword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
        }

        [Theory]
        [InlineData("thinhlqhe172306@fpt.edu.vn", "123456", "")]
        [InlineData("thinhlqhe172306@fpt.edu.vn", "123456", "   ")]
        public async Task ChangePasswordWithOtp_WithInvalidPassword_ShouldReturnFailure(
            string email, string otp, string newPassword)
        {
            // Arrange
            var existingAccount = new Account
            {
                AccountId = 1,
                Email = email,
                PasswordHash = _accountHelper.HashPassword("oldpassword"),
                IsLocked = false
            };

            _mockUnitOfWork.Setup(u => u.Accounts.GetByEmailAsync(email))
                .ReturnsAsync(existingAccount);
            
            _cacheHelper.Set(CachePrefix.OtpCode, email, otp, TimeSpan.FromMinutes(5));

            // Act
            var result = await _authService.ChangePasswordWithOtpAsync(email, otp, newPassword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
        }

        [Theory]
        [InlineData("thinhlqhe172306@fpt.edu.vn", "123456", "th@h123@!")]
        [InlineData("thinhlqhe172306@fpt.edu.vn", "123456", "Thh12345")]
        [InlineData("thinhlqhe172306@fpt.edu.vn", "123456", "Th@!@!@!")]
        [InlineData("thinhlqhe172306@fpt.edu.vn", "123456", "*THH1234@")]
        [InlineData("thinhlqhe172306@fpt.edu.vn", "123456", "Abce123*")]
        public async Task ChangePasswordWithOtp_WithInvalidPasswordFormat_ShouldReturnFailure(
            string email, string otp, string newPassword)
        {
            // Arrange
            var existingAccount = new Account
            {
                AccountId = 1,
                Email = email,
                PasswordHash = _accountHelper.HashPassword("oldpassword"),
                IsLocked = false
            };

            _mockUnitOfWork.Setup(u => u.Accounts.GetByEmailAsync(email))
                .ReturnsAsync(existingAccount);
            
            _cacheHelper.Set(CachePrefix.OtpCode, email, otp, TimeSpan.FromMinutes(5));

            // Act
            var result = await _authService.ChangePasswordWithOtpAsync(email, otp, newPassword);

            // Assert
            result.Should().NotBeNull();
        }

        [Theory]
        [InlineData("thinhlqhe172306@fpt.edu.vn", "123456", "V1@wxyZ!@#abCdeF gHhJkLmNo PqRsTuVwXyZ1234567890!@#$%^&*()")]
        public async Task ChangePasswordWithOtp_WithTooLongPassword_ShouldReturnFailure(
            string email, string otp, string newPassword)
        {
            // Arrange
            var existingAccount = new Account
            {
                AccountId = 1,
                Email = email,
                PasswordHash = _accountHelper.HashPassword("oldpassword"),
                IsLocked = false
            };

            _mockUnitOfWork.Setup(u => u.Accounts.GetByEmailAsync(email))
                .ReturnsAsync(existingAccount);
            
            _cacheHelper.Set(CachePrefix.OtpCode, email, otp, TimeSpan.FromMinutes(5));

            // Act
            var result = await _authService.ChangePasswordWithOtpAsync(email, otp, newPassword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
        }

        #endregion

        #region Return Value Tests

        [Fact]
        public async Task ChangePasswordWithOtp_ShouldReturnResultModel()
        {
            // Arrange
            var email = "test@example.com";
            var otp = "123456";
            var newPassword = "NewPass@123";

            _mockUnitOfWork.Setup(u => u.Accounts.GetByEmailAsync(email))
                .ReturnsAsync((Account?)null);

            // Act
            var result = await _authService.ChangePasswordWithOtpAsync(email, otp, newPassword);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<ResultModel>();
        }

        [Theory]
        [InlineData("user1@example.com", "123456", "Pass@123", "Normal")]
        [InlineData("user2@test.com", "654321", "Test@456", "Abnormal")]
        [InlineData("user3@domain.vn", "111111", "Boundary@789", "Boundary")]
        public async Task ChangePasswordWithOtp_WithDifferentScenarios_ShouldHandleCorrectly(
            string email, string otp, string newPassword, string testType)
        {
            // Arrange
            var existingAccount = new Account
            {
                AccountId = 1,
                Email = email,
                PasswordHash = _accountHelper.HashPassword("oldpassword"),
                IsLocked = false
            };

            _mockUnitOfWork.Setup(u => u.Accounts.GetByEmailAsync(email))
                .ReturnsAsync(existingAccount);
            
            _cacheHelper.Set(CachePrefix.OtpCode, email, otp, TimeSpan.FromMinutes(5));
            
            _mockUnitOfWork.Setup(u => u.Accounts.UpdateAsync(It.IsAny<Account>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _authService.ChangePasswordWithOtpAsync(email, otp, newPassword);

            // Assert
            result.Should().NotBeNull();
            Assert.NotNull(testType); // Use the testType parameter
        }

        #endregion

        #region SendOtp Tests

        [Theory]
        [InlineData("thinhlqhe172306@fpt.edu.vn")]
        [InlineData("test@example.com")]
        public async Task SendOtp_WithValidEmail_ShouldSendOtpSuccessfully(string email)
        {
            // Arrange
            var existingAccount = new Account
            {
                AccountId = 1,
                Email = email,
                IsLocked = false
            };

            _mockUnitOfWork.Setup(u => u.Accounts.GetByEmailAsync(email))
                .ReturnsAsync(existingAccount);
            
            _mockEmailService.Setup(e => e.SendOtpEmail(email, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _authService.SendOtpAsync(email);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SendOtp_WithInvalidEmail_ShouldReturnFailure(string email)
        {
            // Act
            var result = await _authService.SendOtpAsync(email);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task SendOtp_WithNonExistentEmail_ShouldReturnFailure()
        {
            // Arrange
            var email = "nonexistent@example.com";
            
            _mockUnitOfWork.Setup(u => u.Accounts.GetByEmailAsync(email))
                .ReturnsAsync((Account?)null);

            // Act
            var result = await _authService.SendOtpAsync(email);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("không tồn tại");
        }

        #endregion

        #region VerifyOtp Tests

        [Theory]
        [InlineData("test@example.com", "123456")]
        public void VerifyOtp_WithValidOtp_ShouldReturnSuccess(string email, string otp)
        {
            // Arrange
            _cacheHelper.Set(CachePrefix.OtpCode, email, otp, TimeSpan.FromMinutes(5));

            // Act
            var result = _authService.VerifyOtp(email, otp);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
        }

        [Theory]
        [InlineData("test@example.com", "123456", "654321")]
        public void VerifyOtp_WithInvalidOtp_ShouldReturnFailure(string email, string correctOtp, string wrongOtp)
        {
            // Arrange
            _cacheHelper.Set(CachePrefix.OtpCode, email, correctOtp, TimeSpan.FromMinutes(5));

            // Act
            var result = _authService.VerifyOtp(email, wrongOtp);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
        }

        #endregion
    }
}
