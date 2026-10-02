using System.Text.RegularExpressions;
using HireFlow.Hiring.Application.Common;
using HireFlow.Hiring.Application.DTOs;
using HireFlow.Hiring.Application.Interfaces;
using HireFlow.Hiring.Domain.Entities;
using HireFlow.Hiring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HireFlow.Hiring.Infrastructure.Services;

public class CompanyService : ICompanyService
{
    private readonly HiringDbContext _dbContext;

    public CompanyService(HiringDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<Guid>> CreateCompanyAsync(CreateCompanyRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var slug = GenerateSlug(request.Name);
        var baseSlug = slug;
        var counter = 1;
        while (await _dbContext.Companies.AnyAsync(c => c.Slug == slug, cancellationToken))
        {
            slug = $"{baseSlug}-{counter++}";
        }

        var company = new Company
        {
            Name = request.Name.Trim(),
            Slug = slug,
            Website = request.Website?.Trim(),
            Description = request.Description?.Trim(),
            CreatedByUserId = currentUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.Companies.Add(company);
        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUserId,
            Action = "CompanyCreated",
            EntityType = "Company",
            EntityId = company.Id.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(company.Id, 201);
    }

    public async Task<Result<PagedResult<CompanyDto>>> GetCompaniesPagedAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Companies
            .Include(c => c.Jobs)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(s) || c.Slug.ToLower().Contains(s));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CompanyDto(
                c.Id,
                c.Name,
                c.Slug,
                c.Website,
                c.Description,
                c.CreatedByUserId,
                c.CreatedAtUtc,
                c.Jobs.Count(j => j.Status == "Published")
            ))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<CompanyDto>>.Success(PagedResult<CompanyDto>.Create(items, page, pageSize, totalCount));
    }

    public async Task<Result<CompanyDto>> GetCompanyByIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var company = await _dbContext.Companies
            .Include(c => c.Jobs)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);

        if (company == null)
        {
            return Result<CompanyDto>.Failure("Company not found.", 404);
        }

        var dto = new CompanyDto(
            company.Id,
            company.Name,
            company.Slug,
            company.Website,
            company.Description,
            company.CreatedByUserId,
            company.CreatedAtUtc,
            company.Jobs.Count(j => j.Status == "Published")
        );

        return Result<CompanyDto>.Success(dto);
    }

    public async Task<Result<CompanyDto>> UpdateCompanyAsync(Guid companyId, UpdateCompanyRequest request, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var company = await _dbContext.Companies
            .Include(c => c.Jobs)
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);

        if (company == null)
        {
            return Result<CompanyDto>.Failure("Company not found.", 404);
        }

        if (company.CreatedByUserId != currentUserId && !isAdmin)
        {
            return Result<CompanyDto>.Failure("You do not have permission to update this company.", 403);
        }

        company.Name = request.Name.Trim();
        company.Website = request.Website?.Trim();
        company.Description = request.Description?.Trim();
        company.UpdatedAtUtc = DateTimeOffset.UtcNow;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUserId,
            Action = "CompanyUpdated",
            EntityType = "Company",
            EntityId = company.Id.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = new CompanyDto(
            company.Id,
            company.Name,
            company.Slug,
            company.Website,
            company.Description,
            company.CreatedByUserId,
            company.CreatedAtUtc,
            company.Jobs.Count(j => j.Status == "Published")
        );

        return Result<CompanyDto>.Success(dto);
    }

    public async Task<Result<bool>> DeleteCompanyAsync(Guid companyId, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var company = await _dbContext.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);

        if (company == null)
        {
            return Result<bool>.Failure("Company not found.", 404);
        }

        if (company.CreatedByUserId != currentUserId && !isAdmin)
        {
            return Result<bool>.Failure("You do not have permission to delete this company.", 403);
        }

        _dbContext.Companies.Remove(company);
        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUserId,
            Action = "CompanyDeleted",
            EntityType = "Company",
            EntityId = companyId.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    private static string GenerateSlug(string text)
    {
        var clean = Regex.Replace(text.ToLowerInvariant(), @"[^a-z0-9\s-]", "");
        return Regex.Replace(clean, @"\s+", "-").Trim('-');
    }
}
