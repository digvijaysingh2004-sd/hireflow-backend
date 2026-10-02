using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using HireFlow.Notification.Api.Controllers;
using HireFlow.Notification.Application.Common;
using HireFlow.Notification.Application.DTOs;
using HireFlow.Notification.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace HireFlow.Notification.UnitTests;

public class TemplatesControllerTests
{
    private readonly Mock<ITemplateService> _templateServiceMock;
    private readonly TemplatesController _controller;

    public TemplatesControllerTests()
    {
        _templateServiceMock = new Mock<ITemplateService>();
        _controller = new TemplatesController(_templateServiceMock.Object);
    }

    [Fact]
    public async Task CreateTemplate_ValidRequest_Returns201Created()
    {
        var request = new CreateTemplateRequest("WELCOME_EMAIL", "Welcome {{Name}}", "Hello {{Name}}", "en-US");
        var templateDto = new TemplateDto(Guid.NewGuid(), "WELCOME_EMAIL", "Welcome {{Name}}", "Hello {{Name}}", "en-US", 1, true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        var validatorMock = new Mock<IValidator<CreateTemplateRequest>>();
        validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _templateServiceMock.Setup(s => s.CreateTemplateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TemplateDto>.Success(templateDto));

        var result = await _controller.CreateTemplate(request, validatorMock.Object, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task GetTemplates_ReturnsPagedTemplates()
    {
        var templates = new List<TemplateDto>();
        var pagedResult = PagedResult<TemplateDto>.Create(templates, 1, 10, 0);

        _templateServiceMock.Setup(s => s.GetTemplatesPagedAsync(1, 10, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<TemplateDto>>.Success(pagedResult));

        var result = await _controller.GetTemplates(1, 10, null, null, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(pagedResult);
    }
}
