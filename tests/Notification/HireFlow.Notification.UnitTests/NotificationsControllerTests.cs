using FluentAssertions;
using HireFlow.Notification.Api.Controllers;
using HireFlow.Notification.Application.Common;
using HireFlow.Notification.Application.DTOs;
using HireFlow.Notification.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace HireFlow.Notification.UnitTests;

public class NotificationsControllerTests
{
    private readonly Mock<IEmailNotificationService> _serviceMock;
    private readonly NotificationsController _controller;

    public NotificationsControllerTests()
    {
        _serviceMock = new Mock<IEmailNotificationService>();
        _controller = new NotificationsController(_serviceMock.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetNotifications_ReturnsPagedDeliveryRecords()
    {
        var deliveries = new List<NotificationDeliveryDto>();
        var pagedResult = PagedResult<NotificationDeliveryDto>.Create(deliveries, 1, 10, 0);

        _serviceMock.Setup(s => s.GetNotificationsPagedAsync(1, 10, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<NotificationDeliveryDto>>.Success(pagedResult));

        var result = await _controller.GetNotifications(1, 10, null, null, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(pagedResult);
    }

    [Fact]
    public async Task GetNotificationById_Existing_ReturnsDeliveryRecord()
    {
        var id = Guid.NewGuid();
        var dto = new NotificationDeliveryDto(id, "WELCOME_EMAIL", "u***@example.com", "Sent", 1, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        _serviceMock.Setup(s => s.GetNotificationByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<NotificationDeliveryDto>.Success(dto));

        var result = await _controller.GetNotificationById(id, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }
}
