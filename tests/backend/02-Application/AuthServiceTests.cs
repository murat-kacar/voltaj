using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Voltflow.Application.Services;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Identity;
using System.Collections.Generic;

namespace Voltflow.Tests.Application;

public class AuthServiceTests
{
    private readonly Mock<IAppUserRepository> _usersMock;
    private readonly Mock<IRoleRepository> _rolesMock;
    private readonly Mock<IUserRoleRepository> _userRolesMock;
    private readonly Mock<IPasswordHasher<AppUser>> _passwordHasherMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IUserSessionRepository> _sessionsMock;
    private readonly Mock<IPasswordResetTokenRepository> _resetTokensMock;
    private readonly Mock<IHostEnvironment> _environmentMock;
    private readonly Mock<ICommandJournal> _commandJournalMock;
    private readonly Mock<IOperationContext> _operationContextMock;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _usersMock = new Mock<IAppUserRepository>();
        _rolesMock = new Mock<IRoleRepository>();
        _userRolesMock = new Mock<IUserRoleRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher<AppUser>>();
        _tokenServiceMock = new Mock<ITokenService>();
        _sessionsMock = new Mock<IUserSessionRepository>();
        _resetTokensMock = new Mock<IPasswordResetTokenRepository>();
        _environmentMock = new Mock<IHostEnvironment>();
        _environmentMock.Setup(x => x.EnvironmentName).Returns(Environments.Production);
        _commandJournalMock = new Mock<ICommandJournal>();
        _operationContextMock = new Mock<IOperationContext>();

        _sut = new AuthService(
            _usersMock.Object,
            _rolesMock.Object,
            _userRolesMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object,
            _sessionsMock.Object,
            _resetTokensMock.Object,
            _environmentMock.Object,
            _commandJournalMock.Object,
            _operationContextMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnSuccess_WhenValid()
    {
        var request = new RegisterUserRequest("Test User", "test@test.com", "Password123!");
        _usersMock.Setup(x => x.GetByEmailAsync("test@test.com", It.IsAny<CancellationToken>())).ReturnsAsync((AppUser?)null);
        _passwordHasherMock.Setup(x => x.HashPassword(It.IsAny<AppUser>(), "Password123!")).Returns("hashed");

        var result = await _sut.RegisterAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _usersMock.Verify(x => x.AddAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldFail_WhenEmailExists()
    {
        var request = new RegisterUserRequest("Test User", "test@test.com", "Password123!");
        _usersMock.Setup(x => x.GetByEmailAsync("test@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppUser("Existing", "test@test.com"));

        var result = await _sut.RegisterAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnToken_WhenCredentialsValid()
    {
        var request = new LoginRequest("test@test.com", "Password123!");
        var user = new AppUser("Test User", "test@test.com");
        user.SetPasswordHash("hashed");
        user.Approve();
        
        _usersMock.Setup(x => x.GetByEmailAsync("test@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyHashedPassword(user, "hashed", "Password123!"))
            .Returns(PasswordVerificationResult.Success);
        _rolesMock.Setup(x => x.GetNamesByUserAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new List<string> { "Admin" });
        _tokenServiceMock.Setup(x => x.CreateToken(user, It.IsAny<IReadOnlyCollection<string>>())).Returns("token123");

        var result = await _sut.LoginAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Token.Should().Be("token123");
        _sessionsMock.Verify(x => x.AddAsync(It.IsAny<UserSession>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_MasterOtp_ShouldNotAutoApprove_OutsideDevelopment()
    {
        _environmentMock.Setup(x => x.EnvironmentName).Returns(Environments.Production);
        var request = new RegisterUserRequest("Test User", "otp@test.com", "Password123!", "000000");
        _usersMock.Setup(x => x.GetByEmailAsync("otp@test.com", It.IsAny<CancellationToken>())).ReturnsAsync((AppUser?)null);
        _passwordHasherMock.Setup(x => x.HashPassword(It.IsAny<AppUser>(), "Password123!")).Returns("hashed");
        AppUser? added = null;
        _usersMock.Setup(x => x.AddAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
            .Callback<AppUser, CancellationToken>((u, _) => added = u)
            .Returns(Task.CompletedTask);

        var result = await _sut.RegisterAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        added.Should().NotBeNull();
        added!.IsApproved.Should().BeFalse();
        added.EmailVerified.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterAsync_MasterOtp_ShouldAutoApprove_InDevelopment()
    {
        _environmentMock.Setup(x => x.EnvironmentName).Returns(Environments.Development);
        var request = new RegisterUserRequest("Test User", "otp-dev@test.com", "Password123!", "000000");
        _usersMock.Setup(x => x.GetByEmailAsync("otp-dev@test.com", It.IsAny<CancellationToken>())).ReturnsAsync((AppUser?)null);
        _passwordHasherMock.Setup(x => x.HashPassword(It.IsAny<AppUser>(), "Password123!")).Returns("hashed");
        AppUser? added = null;
        _usersMock.Setup(x => x.AddAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
            .Callback<AppUser, CancellationToken>((u, _) => added = u)
            .Returns(Task.CompletedTask);

        var result = await _sut.RegisterAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        added.Should().NotBeNull();
        added!.IsApproved.Should().BeTrue();
        added.EmailVerified.Should().BeTrue();
    }

    [Fact]
    public async Task CompletePasswordResetAsync_MasterOtp_ShouldFail_OutsideDevelopment()
    {
        _environmentMock.Setup(x => x.EnvironmentName).Returns(Environments.Production);
        _resetTokensMock.Setup(x => x.GetAsync("000000", It.IsAny<CancellationToken>())).ReturnsAsync((PasswordResetToken?)null);

        var result = await _sut.CompletePasswordResetAsync("000000", "NewPassword123!", "victim@test.com", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("invalid or expired");
        _usersMock.Verify(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
