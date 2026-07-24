using System.Net.Http.Headers;
using Antital.Domain.Configuration;
using Antital.Application.Features.Investments;
using Antital.Application.Features.Investments.Checkout;
using Antital.Application.Features.Investments.ConfirmInvestmentOrder;
using Antital.Application.Features.Investments.ProcessPaystackWebhook;
using Antital.Infrastructure.Integrations.Cloudinary;
using Antital.Infrastructure.Integrations.Paystack;
using Antital.Application.Features.Investors;
using Antital.Application.Features.Fundraisers;
using Antital.Application.Features.Onboarding;
using Antital.Application.Features.Onboarding.ApplicationFeePayment;
using Antital.Application.Services;
using Antital.Domain.Interfaces;
using Antital.Infrastructure.Repositories;
using Antital.Infrastructure;
using Antital.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using BuildingBlocks.Infrastructure.Implementations;
using Antital.Application.Common.Security;
using Antital.Application.DTOs;
using FluentValidation;
using BuildingBlocks.Application.Behaviours;
using MediatR;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;

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
            cfg.RegisterServicesFromAssemblyContaining(typeof(UserDto));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        });

        return services;
    }

    private static IServiceCollection RegisterValidator(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining(typeof(UserDto));

        return services;
    }

    private static IServiceCollection RegisterRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IAntitalUnitOfWork), typeof(AntitalUnitOfWork));
        services.AddScoped(typeof(IUserRepository), typeof(UserRepository));
        services.AddScoped(typeof(IUserOnboardingRepository), typeof(UserOnboardingRepository));
        services.AddScoped(typeof(IUserInvestmentProfileRepository), typeof(UserInvestmentProfileRepository));
        services.AddScoped(typeof(IUserKycRepository), typeof(UserKycRepository));
        services.AddScoped(typeof(IInvestmentOfferingRepository), typeof(InvestmentOfferingRepository));
        services.AddScoped(typeof(IInvestorDashboardRepository), typeof(InvestorDashboardRepository));
        services.AddScoped(typeof(IFundraiserDashboardRepository), typeof(FundraiserDashboardRepository));
        services.AddScoped(typeof(IFundraiserCampaignUpdatesRepository), typeof(FundraiserCampaignUpdatesRepository));
        services.AddScoped(typeof(IFundraiserInvestorMessagesRepository), typeof(FundraiserInvestorMessagesRepository));
        services.AddScoped(typeof(IFundraiserQiiParticipationRepository), typeof(FundraiserQiiParticipationRepository));
        services.AddScoped(typeof(IFundraiserAnalyticsRepository), typeof(FundraiserAnalyticsRepository));
        services.AddScoped(typeof(IFundraiserDocumentsRepository), typeof(FundraiserDocumentsRepository));
        services.AddScoped(typeof(IInvestmentOrderRepository), typeof(InvestmentOrderRepository));
        services.AddScoped(typeof(IInvestorPaymentMethodRepository), typeof(InvestorPaymentMethodRepository));
        services.AddScoped(typeof(IInvestorWatchlistRepository), typeof(InvestorWatchlistRepository));
        services.AddScoped(typeof(IFundraiserNotificationPreferencesRepository), typeof(FundraiserNotificationPreferencesRepository));

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
        services.AddSwaggerExamplesFromAssemblyOf(typeof(UserDto));
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, InvestmentCheckoutSwaggerOptions>();

        return services;
    }

    private static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register EmailSettings
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.Configure<PaystackSettings>(configuration.GetSection(PaystackSettings.SectionName));
        services.Configure<CloudinarySettings>(opts => BindCloudinarySettings(opts, configuration));
        services.AddHttpClient(EmailService.MailgunHttpClientName);

        // Register authentication services
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<ResetTokenProtector>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAntitalCurrentUser, AntitalCurrentUser>();
        services.AddScoped<IKycVerificationService, PassThroughKycVerificationService>();
        services.AddScoped<IOnboardingUserAccess, OnboardingUserAccess>();
        services.AddScoped<IInvestorUserAccess, InvestorUserAccess>();
        services.AddScoped<IFundraiserUserAccess, FundraiserUserAccess>();
        services.AddScoped<IInvestmentCheckoutAccess, InvestmentCheckoutAccess>();
        services.AddScoped<IInvestmentPaymentConfirmationService, InvestmentPaymentConfirmationService>();
        services.AddScoped<IApplicationFeePaymentConfirmationService, ApplicationFeePaymentConfirmationService>();
        services.AddScoped<IConfirmInvestmentOrderService, ConfirmInvestmentOrderService>();
        services.AddScoped<PaystackSignatureValidator>();
        services.AddScoped<InvestmentOfferingAccess>();
        services.AddScoped<IFileUploadService, CloudinaryFileUploadService>();

        services.AddHttpClient<IPaystackClient, PaystackClient>((serviceProvider, client) =>
        {
            client.BaseAddress = new Uri("https://api.paystack.co/");

            var secretKey = serviceProvider.GetRequiredService<IOptions<PaystackSettings>>().Value.SecretKey;
            if (!string.IsNullOrWhiteSpace(secretKey))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);
            }
        });

        return services;
    }

    /// <summary>
    /// Binds nested Cloudinary:* and flat Cloudinary_* env aliases used in local secrets.
    /// </summary>
    private static void BindCloudinarySettings(CloudinarySettings opts, IConfiguration configuration)
    {
        configuration.GetSection(CloudinarySettings.SectionName).Bind(opts);

        opts.CloudName = FirstNonEmpty(opts.CloudName, configuration["Cloudinary_Cloud_Name"]);
        opts.ApiKey = FirstNonEmpty(opts.ApiKey, configuration["Cloudinary_API_Key"]);
        opts.ApiSecret = FirstNonEmpty(opts.ApiSecret, configuration["Cloudinary_API_Secret"]);
        opts.FolderName = FirstNonEmpty(opts.FolderName, configuration["Cloudinary_FolderName"], "antital");
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }
}
