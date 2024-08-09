﻿using eShop.EventBus.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using eShop.OrderProcessor.Events;
using Microsoft.Data.SqlClient;

namespace eShop.OrderProcessor.Services
{
    public class GracePeriodManagerService(
        IOptions<BackgroundTaskOptions> options,
        IEventBus eventBus,
        ILogger<GracePeriodManagerService> logger) : BackgroundService
    {
        private readonly BackgroundTaskOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var delayTime = TimeSpan.FromSeconds(_options.CheckUpdateTime);

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("GracePeriodManagerService is starting.");
                stoppingToken.Register(() => logger.LogDebug("GracePeriodManagerService background task is stopping."));
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("GracePeriodManagerService background task is doing background work.");
                }

                await CheckConfirmedGracePeriodOrders();

                await Task.Delay(delayTime, stoppingToken);
            }

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("GracePeriodManagerService background task is stopping.");
            }
        }

        private async Task CheckConfirmedGracePeriodOrders()
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Checking confirmed grace period orders");
            }

            var orderIds = await GetConfirmedGracePeriodOrders();

            foreach (var orderId in orderIds)
            {
                var confirmGracePeriodEvent = new GracePeriodConfirmedIntegrationEvent(orderId);

                logger.LogInformation("Publishing integration event: {IntegrationEventId} - ({@IntegrationEvent})", confirmGracePeriodEvent.Id, confirmGracePeriodEvent);

                await eventBus.PublishAsync(confirmGracePeriodEvent);
            }
        }

        private async ValueTask<List<int>> GetConfirmedGracePeriodOrders()
        {
            try
            {
                SqlConnectionStringBuilder sConnB = new SqlConnectionStringBuilder()
                {
                    DataSource = "DESKTOP-3D9O0KV",
                    InitialCatalog = "OrderingDB",
                    UserID = "sa",
                    Password = "Xzyk1234567"
                };

                SqlConnection conn = new SqlConnection(sConnB.ConnectionString);

                using var command = conn.CreateCommand();
                command.CommandText = """
                    SELECT "Id"
                    FROM ordering.orders
                    WHERE CURRENT_TIMESTAMP - "OrderDate" >= @GracePeriodTime AND "OrderStatus" = 'Submitted'
                    """;
                command.Parameters.AddWithValue("GracePeriodTime", TimeSpan.FromMinutes(_options.GracePeriodTime));

                List<int> ids = [];

                await conn.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    ids.Add(reader.GetInt32(0));
                }

                return ids;
            }
            catch (NpgsqlException exception)
            {
                logger.LogError(exception, "Fatal error establishing database connection");
            }

            return [];
        }
    }
}
