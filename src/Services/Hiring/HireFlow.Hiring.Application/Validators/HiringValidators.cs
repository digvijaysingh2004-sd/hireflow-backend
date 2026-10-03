using FluentValidation;
using HireFlow.Hiring.Application.DTOs;

namespace HireFlow.Hiring.Application.Validators;

public class CreateCompanyRequestValidator : AbstractValidator<CreateCompanyRequest>
{
    public CreateCompanyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(150).WithMessage("Company name cannot exceed 150 characters.");

        RuleFor(x => x.Website)
            .MaximumLength(255).WithMessage("Website URL cannot exceed 255 characters.");
    }
}

public class UpdateCompanyRequestValidator : AbstractValidator<UpdateCompanyRequest>
{
    public UpdateCompanyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(150).WithMessage("Company name cannot exceed 150 characters.");

        RuleFor(x => x.Website)
            .MaximumLength(255).WithMessage("Website URL cannot exceed 255 characters.");
    }
}

public class CreateJobRequestValidator : AbstractValidator<CreateJobRequest>
{
    public CreateJobRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Job title is required.")
            .MaximumLength(160).WithMessage("Job title cannot exceed 160 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Job description is required.");

        RuleFor(x => x.EmploymentType)
            .NotEmpty().WithMessage("Employment type is required.")
            .MaximumLength(30);

        RuleFor(x => x.SalaryMax)
            .GreaterThanOrEqualTo(x => x.SalaryMin!.Value)
            .When(x => x.SalaryMin.HasValue && x.SalaryMax.HasValue)
            .WithMessage("Maximum salary must be greater than or equal to minimum salary.");

        RuleFor(x => x.ExperienceMaxYears)
            .GreaterThanOrEqualTo(x => x.ExperienceMinYears!.Value)
            .When(x => x.ExperienceMinYears.HasValue && x.ExperienceMaxYears.HasValue)
            .WithMessage("Maximum experience must be greater than or equal to minimum experience.");
    }
}

public class UpdateJobRequestValidator : AbstractValidator<UpdateJobRequest>
{
    public UpdateJobRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Job title is required.")
            .MaximumLength(160).WithMessage("Job title cannot exceed 160 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Job description is required.");

        RuleFor(x => x.EmploymentType)
            .NotEmpty().WithMessage("Employment type is required.")
            .MaximumLength(30);

        RuleFor(x => x.SalaryMax)
            .GreaterThanOrEqualTo(x => x.SalaryMin!.Value)
            .When(x => x.SalaryMin.HasValue && x.SalaryMax.HasValue)
            .WithMessage("Maximum salary must be greater than or equal to minimum salary.");
    }
}

public class ApplyJobRequestValidator : AbstractValidator<ApplyJobRequest>
{
    public ApplyJobRequestValidator()
    {
        RuleFor(x => x.ResumeUrl)
            .MaximumLength(500);

        RuleFor(x => x.CoverNote)
            .MaximumLength(2000);
    }
}

public class UpdateApplicationStatusRequestValidator : AbstractValidator<UpdateApplicationStatusRequest>
{
    private static readonly string[] ValidStatuses =
    [
        "Submitted", "UnderReview", "Shortlisted", "InterviewScheduled", "Offered", "Hired", "Rejected", "Withdrawn"
    ];

    public UpdateApplicationStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}");
    }
}

public class ScheduleInterviewRequestValidator : AbstractValidator<ScheduleInterviewRequest>
{
    public ScheduleInterviewRequestValidator()
    {
        RuleFor(x => x.StartsAtUtc)
            .GreaterThan(DateTimeOffset.UtcNow).WithMessage("Interview start time must be in the future.");

        RuleFor(x => x.EndsAtUtc)
            .GreaterThan(x => x.StartsAtUtc).WithMessage("Interview end time must be after start time.");
    }
}

public class RescheduleInterviewRequestValidator : AbstractValidator<RescheduleInterviewRequest>
{
    public RescheduleInterviewRequestValidator()
    {
        RuleFor(x => x.StartsAtUtc)
            .GreaterThan(DateTimeOffset.UtcNow).WithMessage("Interview start time must be in the future.");

        RuleFor(x => x.EndsAtUtc)
            .GreaterThan(x => x.StartsAtUtc).WithMessage("Interview end time must be after start time.");
    }
}
