using Antital.Domain.Interfaces;
using Antital.Infrastructure.Repositories;
using Antital.Infrastructure;
using Antital.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using BuildingBlocks.Infrastructure.Implementations;
using Antital.Application;
using Antital.Application.Common.Security;
using FluentValidation;
using BuildingBlocks.Application.Behaviours;
using MediatR;
using Swashbuckle.AspNetCore.Filters;

namespace Antital.API.Configs;

public static class DependencyInjection
{
    public static IServiceCollection Register(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .RegisterRepositories()
            .RegisterDBContext(configuration)
            .RegisterAuthentication()
            .RegisterMediatR()
            .RegisterValidator()
            .RegisterSwagger()
            .RegisterServices(configuration);

        return services;
    }
    private static IServiceCollection RegisterMediatR(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining(typeof(SampleModelMapper));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        });

        return services;
    }

    private static IServiceCollection RegisterValidator(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining(typeof(SampleModelMapper));

        return services;
    }

    private static IServiceCollection RegisterRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IAntitalUnitOfWork), typeof(AntitalUnitOfWork));
        services.AddScoped(typeof(ISampleModelRepository), typeof(SampleModelRepository));
        services.AddScoped(typeof(IAnotherSampleModelRepository), typeof(AnotherSampleModelRepository));
        services.AddScoped(typeof(IUserRepository), typeof(UserRepository));

        return services;
    }

    private static IServiceCollection RegisterDBContext(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AntitalDBContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")),
            ServiceLifetime.Scoped);
        services.AddScoped<DBContext>(provider => provider.GetService<AntitalDBContext>()!);

        return services;
    }

    private static IServiceCollection RegisterAuthentication(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy("CanDeletePolicy", policy =>
            policy.RequireClaim("Permissions", "CanDelete"));

        return services;
    }

    private static IServiceCollection RegisterSwagger(this IServiceCollection services)
    {
        services.AddSwaggerExamplesFromAssemblyOf(typeof(SampleModelMapper));

        return services;
    }

    private static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register EmailSettings
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        
        // Register authentication services
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<ResetTokenProtector>();
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}
