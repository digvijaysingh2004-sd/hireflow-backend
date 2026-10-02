using System.Security.Claims;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using HireFlow.Identity.Api.Controllers;
using HireFlow.Identity.Application.Common;
using HireFlow.Identity.Application.DTOs;
using HireFlow.Identity.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace HireFlow.Identity.UnitTests;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IValidator<RegisterRequest>> _registerValidatorMock;
    private readonly Mock<IValidator<VerifyEmailRequest>> _verifyEmailValidatorMock;
    private readonly Mock<IValidator<LoginRequest>> _loginValidatorMock;
    private readonly Mock<IValidator<RefreshTokenRequest>> _refreshValidatorMock;
    private readonly Mock<IValidator<ForgotPasswordRequest>> _forgotPasswordValidatorMock;
    private readonly Mock<IValidator<ResetPasswordRequest>> _resetPasswordValidatorMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _registerValidatorMock = new Mock<IValidator<RegisterRequest>>();
        _verifyEmailValidatorMock = new Mock<IValidator<VerifyEmailRequest>>();
        _loginValidatorMock = new Mock<IValidator<LoginRequest>>();
        _refreshValidatorMock = new Mock<IValidator<RefreshTokenRequest>>();
        _forgotPasswordValidatorMock = new Mock<IValidator<ForgotPasswordRequest>>();
        _resetPasswordValidatorMock = new Mock<IValidator<ResetPasswordRequest>>();

        _registerValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _verifyEmailValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<VerifyEmailRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _loginValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _refreshValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<RefreshTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _forgotPasswordValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ForgotPasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _resetPasswordValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ResetPasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _controller = new AuthController(
            _authServiceMock.Object,
            _registerValidatorMock.Object,
            _verifyEmailValidatorMock.Object,
            _loginValidatorMock.Object,
            _refreshValidatorMock.Object,
            _forgotPasswordValidatorMock.Object,
            _resetPasswordValidatorMock.Object);

        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task Register_ValidRequest_Returns201Created()
    {
        var request = new RegisterRequest("candidate@example.com", "Password123!", "John", "Doe", "Candidate");
        var userId = Guid.NewGuid();
        _authServiceMock.Setup(s => s.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(userId));

        var result = await _controller.Register(request, CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task VerifyEmail_ValidOtp_Returns200Ok()
    {
        var request = new VerifyEmailRequest("candidate@example.com", "123456");
        _authServiceMock.Setup(s => s.VerifyEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.VerifyEmail(request, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithAuthResponse()
    {
        var request = new LoginRequest("candidate@example.com", "Password123!");
        var response = new AuthResponse("access_token", 900, "refresh_token", new UserDto(Guid.NewGuid(), "candidate@example.com", "John", "Doe", new List<string> { "Candidate" }));
        
        _authServiceMock.Setup(s => s.LoginAsync(request, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponse>.Success(response));

        var result = await _controller.Login(request, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task RefreshToken_ValidToken_Returns200WithNewTokens()
    {
        var request = new RefreshTokenRequest("valid_refresh_token");
        var response = new AuthResponse("new_access_token", 900, "new_refresh_token", new UserDto(Guid.NewGuid(), "candidate@example.com", "John", "Doe", new List<string> { "Candidate" }));

        _authServiceMock.Setup(s => s.RefreshTokenAsync(request.RefreshToken, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponse>.Success(response));

        var result = await _controller.RefreshToken(request, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task Logout_ValidToken_Returns200Ok()
    {
        var request = new RefreshTokenRequest("valid_refresh_token");
        _authServiceMock.Setup(s => s.LogoutAsync(request.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.Logout(request, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ForgotPassword_ValidEmail_Returns200Ok()
    {
        var request = new ForgotPasswordRequest("candidate@example.com");
        _authServiceMock.Setup(s => s.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.ForgotPassword(request, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ResetPassword_ValidOtp_Returns200Ok()
    {
        var request = new ResetPasswordRequest("candidate@example.com", "123456", "NewPassword123!");
        _authServiceMock.Setup(s => s.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.ResetPassword(request, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetCurrentUser_AuthenticatedUser_Returns200WithProfile()
    {
        var userId = Guid.NewGuid();
        var userDto = new UserDto(userId, "candidate@example.com", "John", "Doe", new List<string> { "Candidate" });

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext.HttpContext.User = claimsPrincipal;

        _authServiceMock.Setup(s => s.GetCurrentUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Success(userDto));

        var result = await _controller.GetCurrentUser(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(userDto);
    }
}
