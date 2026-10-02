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

public class CompaniesControllerTests
{
    private readonly Mock<ICompanyService> _companyServiceMock;
    private readonly CompaniesController _controller;
    private readonly Guid _testUserId;

    public CompaniesControllerTests()
    {
        _companyServiceMock = new Mock<ICompanyService>();
        _testUserId = Guid.NewGuid();

        _controller = new CompaniesController(_companyServiceMock.Object);

        var claims = new[] 
        { 
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task GetCompanies_ReturnsPagedCompanies()
    {
        var companies = new List<CompanyDto>();
        var pagedResult = PagedResult<CompanyDto>.Create(companies, 1, 10, 0);

        _companyServiceMock.Setup(s => s.GetCompaniesPagedAsync(1, 10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<CompanyDto>>.Success(pagedResult));

        var result = await _controller.GetCompanies(1, 10, null, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(pagedResult);
    }

    [Fact]
    public async Task CreateCompany_ValidRequest_Returns201Created()
    {
        var request = new CreateCompanyRequest("TechCorp", "https://techcorp.com", "Leading Tech");
        var companyId = Guid.NewGuid();

        var validatorMock = new Mock<IValidator<CreateCompanyRequest>>();
        validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _companyServiceMock.Setup(s => s.CreateCompanyAsync(request, _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(companyId));

        var result = await _controller.CreateCompany(request, validatorMock.Object, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
    }
}
