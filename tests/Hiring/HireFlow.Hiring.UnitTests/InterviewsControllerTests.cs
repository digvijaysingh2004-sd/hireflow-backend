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

public class InterviewsControllerTests
{
    private readonly Mock<IInterviewService> _interviewServiceMock;
    private readonly InterviewsController _controller;
    private readonly Guid _testUserId;

    public InterviewsControllerTests()
    {
        _interviewServiceMock = new Mock<IInterviewService>();
        _testUserId = Guid.NewGuid();

        _controller = new InterviewsController(_interviewServiceMock.Object);

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
    public async Task ScheduleInterview_ValidRequest_Returns201Created()
    {
        var appId = Guid.NewGuid();
        var request = new ScheduleInterviewRequest(DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(1).AddHours(1), "https://meet.link", "Notes");
        var interviewId = Guid.NewGuid();

        var validatorMock = new Mock<IValidator<ScheduleInterviewRequest>>();
        validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _interviewServiceMock.Setup(s => s.ScheduleInterviewAsync(appId, request, _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(interviewId));

        var result = await _controller.ScheduleInterview(appId, request, validatorMock.Object, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task GetInterviews_ReturnsPagedInterviews()
    {
        var interviews = new List<InterviewDto>();
        var pagedResult = PagedResult<InterviewDto>.Create(interviews, 1, 10, 0);

        _interviewServiceMock.Setup(s => s.GetInterviewsPagedAsync(1, 10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<InterviewDto>>.Success(pagedResult));

        var result = await _controller.GetInterviews(1, 10, null, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(pagedResult);
    }
}
