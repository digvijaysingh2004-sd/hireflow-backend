using System.Security.Claims;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using HireFlow.Hiring.Api.Controllers;
using HireFlow.Hiring.Application.Common;
using HireFlow.Hiring.Application.DTOs;
using HireFlow.Hiring.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace HireFlow.Hiring.UnitTests;

public class ApplicationsControllerTests
{
    private readonly Mock<IApplicationService> _applicationServiceMock;
    private readonly ApplicationsController _controller;
    private readonly Guid _testUserId;

    public ApplicationsControllerTests()
    {
        _applicationServiceMock = new Mock<IApplicationService>();
        _testUserId = Guid.NewGuid();

        _controller = new ApplicationsController(_applicationServiceMock.Object);

        var claims = new[] 
        { 
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString()),
            new Claim(ClaimTypes.Role, "Candidate")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task ApplyToJob_ValidRequest_Returns201Created()
    {
        var jobId = Guid.NewGuid();
        var request = new ApplyJobRequest("https://resume.url", "Cover Note");
        var applicationId = Guid.NewGuid();

        var validatorMock = new Mock<IValidator<ApplyJobRequest>>();
        validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _applicationServiceMock.Setup(s => s.ApplyJobAsync(jobId, request, _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(applicationId));

        var result = await _controller.ApplyToJob(jobId, request, validatorMock.Object, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task GetMyApplications_ReturnsPagedApplications()
    {
        var apps = new List<JobApplicationDto>();
        var pagedResult = PagedResult<JobApplicationDto>.Create(apps, 1, 10, 0);

        _applicationServiceMock.Setup(s => s.GetCandidateApplicationsAsync(_testUserId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<JobApplicationDto>>.Success(pagedResult));

        var result = await _controller.GetMyApplications(1, 10, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(pagedResult);
    }

    [Fact]
    public async Task GetApplicationById_Existing_ReturnsApplication()
    {
        var appId = Guid.NewGuid();
        var appDto = new JobApplicationDto(appId, Guid.NewGuid(), "Senior Dev", "TechCorp", _testUserId, "http://resume.pdf", "Cover Note", "Submitted", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        _applicationServiceMock.Setup(s => s.GetApplicationByIdAsync(appId, _testUserId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<JobApplicationDto>.Success(appDto));

        var result = await _controller.GetApplicationById(appId, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(appDto);
    }

    [Fact]
    public async Task WithdrawApplication_Valid_Returns200Ok()
    {
        var appId = Guid.NewGuid();

        _applicationServiceMock.Setup(s => s.WithdrawApplicationAsync(appId, _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.WithdrawApplication(appId, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }
}
