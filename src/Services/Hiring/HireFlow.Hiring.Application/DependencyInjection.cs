using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace HireFlow.Hiring.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddHiringApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}

