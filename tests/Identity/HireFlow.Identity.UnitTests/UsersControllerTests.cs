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

public class UsersControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly UsersController _controller;
    private readonly Guid _testUserId;

    public UsersControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _tokenServiceMock = new Mock<ITokenService>();
        _testUserId = Guid.NewGuid();

        _controller = new UsersController(_userServiceMock.Object, _tokenServiceMock.Object);

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task GetMyProfile_ValidUser_Returns200WithProfile()
    {
        var userDto = new UserDto(_testUserId, "test@example.com", "John", "Doe", new List<string> { "Candidate" });
        _userServiceMock.Setup(s => s.GetProfileAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Success(userDto));

        var result = await _controller.GetMyProfile(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(userDto);
    }

    [Fact]
    public async Task UpdateMyProfile_ValidRequest_Returns200WithUpdatedProfile()
    {
        var request = new UpdateProfileRequest("Jane", "Smith");
        var userDto = new UserDto(_testUserId, "test@example.com", "Jane", "Smith", new List<string> { "Candidate" });

        var validatorMock = new Mock<IValidator<UpdateProfileRequest>>();
        validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _userServiceMock.Setup(s => s.UpdateProfileAsync(_testUserId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Success(userDto));

        var result = await _controller.UpdateMyProfile(request, validatorMock.Object, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(userDto);
    }

    [Fact]
    public async Task GetUsers_AdminCall_ReturnsPagedResult()
    {
        var userList = new List<UserDto>
        {
            new UserDto(_testUserId, "test@example.com", "John", "Doe", new List<string> { "Admin" })
        };
        var pagedResult = PagedResult<UserDto>.Create(userList, 1, 10, 1);

        _userServiceMock.Setup(s => s.GetUsersPagedAsync(1, 10, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<UserDto>>.Success(pagedResult));

        var result = await _controller.GetUsers(1, 10, null, null, null, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(pagedResult);
    }

    [Fact]
    public async Task GetUserById_ExistingUser_Returns200WithUser()
    {
        var targetUserId = Guid.NewGuid();
        var userDto = new UserDto(targetUserId, "target@example.com", "Target", "User", new List<string> { "Recruiter" });

        _userServiceMock.Setup(s => s.GetUserByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Success(userDto));

        var result = await _controller.GetUserById(targetUserId, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(userDto);
    }

    [Fact]
    public async Task UpdateUserStatus_Deactivate_Returns200()
    {
        var targetUserId = Guid.NewGuid();
        var request = new UpdateUserStatusRequest(false);

        _userServiceMock.Setup(s => s.UpdateUserStatusAsync(targetUserId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.UpdateUserStatus(targetUserId, request, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task DeleteUser_ExistingUser_Returns200()
    {
        var targetUserId = Guid.NewGuid();

        _userServiceMock.Setup(s => s.DeleteUserAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.DeleteUser(targetUserId, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task DeleteUser_SelfDelete_Returns400BadRequest()
    {
        var result = await _controller.DeleteUser(_testUserId, CancellationToken.None);

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }
}
