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

public class JobsControllerTests
{
    private readonly Mock<IJobService> _jobServiceMock;
    private readonly Mock<IApplicationService> _applicationServiceMock;
    private readonly JobsController _controller;
    private readonly Guid _testUserId;

    public JobsControllerTests()
    {
        _jobServiceMock = new Mock<IJobService>();
        _applicationServiceMock = new Mock<IApplicationService>();
        _testUserId = Guid.NewGuid();

        _controller = new JobsController(_jobServiceMock.Object, _applicationServiceMock.Object);

        var claims = new[] 
        { 
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString()),
            new Claim(ClaimTypes.Role, "Recruiter")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task GetJobs_ReturnsPagedResult()
    {
        var jobs = new List<JobDto>();
        var pagedResult = PagedResult<JobDto>.Create(jobs, 1, 10, 0);

        _jobServiceMock.Setup(s => s.GetJobsPagedAsync(1, 10, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<JobDto>>.Success(pagedResult));

        var result = await _controller.GetJobs(1, 10, null, null, null, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(pagedResult);
    }

    [Fact]
    public async Task GetJobById_ExistingJob_ReturnsJob()
    {
        var jobId = Guid.NewGuid();
        var jobDto = new JobDto(jobId, Guid.NewGuid(), "TechCorp", "Senior Dev", "senior-dev", "Desc", "Reqs", "Remote", "FullTime", "Published", 3, 5, 100000, 150000, "USD", new[] { "C#" }, _testUserId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null);

        _jobServiceMock.Setup(s => s.GetJobByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<JobDto>.Success(jobDto));

        var result = await _controller.GetJobById(jobId, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(jobDto);
    }

    [Fact]
    public async Task CreateJob_ValidRequest_Returns201Created()
    {
        var request = new CreateJobRequest(Guid.NewGuid(), "Software Engineer", "Description", "Requirements", "Remote", "FullTime", 2, 5, 80000, 120000, "USD", new[] { ".NET" }, null);
        var jobId = Guid.NewGuid();

        var validatorMock = new Mock<IValidator<CreateJobRequest>>();
        validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _jobServiceMock.Setup(s => s.CreateJobAsync(request, _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(jobId));

        var result = await _controller.CreateJob(request, validatorMock.Object, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task PublishJob_ValidJob_Returns200Ok()
    {
        var jobId = Guid.NewGuid();
        _jobServiceMock.Setup(s => s.PublishJobAsync(jobId, _testUserId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.PublishJob(jobId, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task CloseJob_ValidJob_Returns200Ok()
    {
        var jobId = Guid.NewGuid();
        _jobServiceMock.Setup(s => s.CloseJobAsync(jobId, _testUserId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.CloseJob(jobId, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task DeleteJob_ValidJob_Returns200Ok()
    {
        var jobId = Guid.NewGuid();
        _jobServiceMock.Setup(s => s.DeleteJobAsync(jobId, _testUserId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.DeleteJob(jobId, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }
}
