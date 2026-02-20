using Common.Notifications.Function.Interfaces;
using Common.Notifications.Function.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Notifications.Function.Configuration;

public static class EmailServiceConfiguration
{
    public static IServiceCollection AddEmailServices(this IServiceCollection services)
    {
        services.AddScoped<IEmailService, AzureCommunicationEmailService>();
        services.AddSingleton<IEmailTemplateService, EmailTemplateService>();

        return services;
    }
}
