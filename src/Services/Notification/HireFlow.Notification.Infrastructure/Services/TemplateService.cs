using HireFlow.Notification.Application.Common;
using HireFlow.Notification.Application.DTOs;
using HireFlow.Notification.Application.Interfaces;
using HireFlow.Notification.Domain.Entities;
using HireFlow.Notification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HireFlow.Notification.Infrastructure.Services;

public class TemplateService : ITemplateService
{
    private readonly NotificationDbContext _context;

    public TemplateService(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<TemplateDto>> CreateTemplateAsync(CreateTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedKey = request.TemplateKey.Trim();
        var exists = await _context.Templates
            .AnyAsync(t => t.TemplateKey.ToLower() == normalizedKey.ToLower(), cancellationToken);

        if (exists)
        {
            return Result<TemplateDto>.Failure($"A template with key '{normalizedKey}' already exists.", 409);
        }

        var template = new EmailTemplate
        {
            TemplateKey = normalizedKey,
            SubjectTemplate = request.SubjectTemplate.Trim(),
            BodyTemplate = request.BodyTemplate.Trim(),
            Locale = string.IsNullOrWhiteSpace(request.Locale) ? "en-US" : request.Locale.Trim(),
            Version = 1,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        _context.Templates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<TemplateDto>.Success(MapToDto(template), 201);
    }

    public async Task<Result<PagedResult<TemplateDto>>> GetTemplatesPagedAsync(
        int page,
        int pageSize,
        string? search,
        bool? activeOnly,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _context.Templates.AsNoTracking().AsQueryable();

        if (activeOnly.HasValue && activeOnly.Value)
        {
            query = query.Where(t => t.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchNorm = search.Trim().ToLower();
            query = query.Where(t => t.TemplateKey.ToLower().Contains(searchNorm) ||
                                     t.SubjectTemplate.ToLower().Contains(searchNorm));
        }

        query = query.OrderBy(t => t.TemplateKey);

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => MapToDto(t))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<TemplateDto>>.Success(PagedResult<TemplateDto>.Create(items, page, pageSize, totalItems));
    }

    public async Task<Result<TemplateDto>> GetTemplateByIdAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await _context.Templates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

        if (template == null)
        {
            return Result<TemplateDto>.Failure("Template not found.", 404);
        }

        return Result<TemplateDto>.Success(MapToDto(template));
    }

    public async Task<Result<TemplateDto>> UpdateTemplateAsync(
        Guid templateId,
        UpdateTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var template = await _context.Templates
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

        if (template == null)
        {
            return Result<TemplateDto>.Failure("Template not found.", 404);
        }

        template.SubjectTemplate = request.SubjectTemplate.Trim();
        template.BodyTemplate = request.BodyTemplate.Trim();
        template.IsActive = request.IsActive;
        template.Version += 1;
        template.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Result<TemplateDto>.Success(MapToDto(template));
    }

    public async Task<Result<bool>> DeactivateTemplateAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await _context.Templates
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

        if (template == null)
        {
            return Result<bool>.Failure("Template not found.", 404);
        }

        template.IsActive = false;
        template.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    private static TemplateDto MapToDto(EmailTemplate t)
    {
        return new TemplateDto(
            t.Id,
            t.TemplateKey,
            t.SubjectTemplate,
            t.BodyTemplate,
            t.Locale,
            t.Version,
            t.IsActive,
            t.CreatedAtUtc,
            t.UpdatedAtUtc
        );
    }
}
