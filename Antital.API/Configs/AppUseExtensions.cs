using BuildingBlocks.Application.Jobs;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Antital.Infrastructure;
using System.Text;

namespace Antital.API.Configs;

public static class AppUseExtensions
{
    public static IApplicationBuilder AppUse(this IApplicationBuilder app, IConfiguration configuration)
    {
        app.MigratingDatabase();

        UsingJobs(configuration);

        UsingRabbitMQ(configuration);

        return app;
    }

    private static void UsingRabbitMQ(IConfiguration configuration)
    {
        // RabbitMQ is optional - only connect if settings are provided
        var hostName = configuration["RabbitMQSettings:HostName"];
        if (string.IsNullOrEmpty(hostName))
        {
            Console.WriteLine("RabbitMQ settings not found. Skipping RabbitMQ initialization.");
            return;
        }

        try
        {
            var factory = new ConnectionFactory()
            {
                HostName = hostName,
                UserName = configuration["RabbitMQSettings:UserName"],
                Password = configuration["RabbitMQSettings:Password"]
            };

            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            channel.QueueDeclare(queue: "TestModel_Notifications", durable: false, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($"Received Message: {message}");
            };

            channel.BasicConsume(queue: "TestModel_Notifications", autoAck: true, consumer: consumer);
            Console.WriteLine("RabbitMQ consumer initialized successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize RabbitMQ: {ex.Message}. Continuing without RabbitMQ.");
        }
    }

    private static void UsingJobs(IConfiguration configuration)
    {
        RecurringJob.AddOrUpdate<HealthCheckJob>("SampleJob", x => HealthCheckJob.CheckStatus(configuration["HealthCheck:Uri"]!), "* * * * *");
    }

    private static IApplicationBuilder MigratingDatabase(this IApplicationBuilder app)
    {
        using var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var context = serviceScope.ServiceProvider.GetService<AntitalDBContext>();
        context?.Database.Migrate();

        return app;
    }
}
