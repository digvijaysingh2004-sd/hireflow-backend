using FluentAssertions;
using HireFlow.Identity.Api.Controllers;
using HireFlow.Identity.Application.Common;
using HireFlow.Identity.Application.DTOs;
using HireFlow.Identity.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace HireFlow.Identity.UnitTests;

public class RolesControllerTests
{
    private readonly Mock<IRoleService> _roleServiceMock;
    private readonly RolesController _controller;

    public RolesControllerTests()
    {
        _roleServiceMock = new Mock<IRoleService>();
        _controller = new RolesController(_roleServiceMock.Object);
    }

    [Fact]
    public async Task GetRoles_ReturnsAllRoles()
    {
        var roles = new List<RoleDto>
        {
            new RoleDto(Guid.NewGuid(), "Admin", "Administrator role"),
            new RoleDto(Guid.NewGuid(), "Recruiter", "Recruiter role"),
            new RoleDto(Guid.NewGuid(), "HiringManager", "Hiring Manager role"),
            new RoleDto(Guid.NewGuid(), "Candidate", "Candidate role")
        };

        _roleServiceMock.Setup(s => s.GetRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<RoleDto>>.Success(roles));

        var result = await _controller.GetRoles(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(roles);
    }
}
