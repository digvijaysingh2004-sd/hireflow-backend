using FluentValidation;
using HireFlow.Notification.Application.DTOs;

namespace HireFlow.Notification.Application.Validators;

public class SendEmailNotificationRequestValidator : AbstractValidator<SendEmailNotificationRequest>
{
    public SendEmailNotificationRequestValidator()
    {
        RuleFor(x => x.TemplateKey)
            .NotEmpty().WithMessage("Template key is required.")
            .MaximumLength(100);

        RuleFor(x => x.RecipientEmail)
            .NotEmpty().WithMessage("Recipient email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(255);
    }
}

public class SendOtpNotificationRequestValidator : AbstractValidator<SendOtpNotificationRequest>
{
    public SendOtpNotificationRequestValidator()
    {
        RuleFor(x => x.RecipientEmail)
            .NotEmpty().WithMessage("Recipient email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(255);

        RuleFor(x => x.Purpose)
            .NotEmpty().WithMessage("Purpose is required.")
            .MaximumLength(100);

        RuleFor(x => x.Otp)
            .NotEmpty().WithMessage("OTP is required.")
            .Length(6).WithMessage("OTP must be 6 characters.");
    }
}

public class CreateTemplateRequestValidator : AbstractValidator<CreateTemplateRequest>
{
    public CreateTemplateRequestValidator()
    {
        RuleFor(x => x.TemplateKey)
            .NotEmpty().WithMessage("Template key is required.")
            .MaximumLength(100);

        RuleFor(x => x.SubjectTemplate)
            .NotEmpty().WithMessage("Subject template is required.")
            .MaximumLength(255);

        RuleFor(x => x.BodyTemplate)
            .NotEmpty().WithMessage("Body template is required.")
            .MaximumLength(10000);
    }
}

public class UpdateTemplateRequestValidator : AbstractValidator<UpdateTemplateRequest>
{
    public UpdateTemplateRequestValidator()
    {
        RuleFor(x => x.SubjectTemplate)
            .NotEmpty().WithMessage("Subject template is required.")
            .MaximumLength(255);

        RuleFor(x => x.BodyTemplate)
            .NotEmpty().WithMessage("Body template is required.")
            .MaximumLength(10000);
    }
}
