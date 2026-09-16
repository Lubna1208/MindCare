namespace MindCare.Services;

public sealed class AppointmentChatNotificationWorker(IServiceScopeFactory scopeFactory, ILogger<AppointmentChatNotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await ProcessNotificationsAsync(stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ProcessNotificationsAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug("Appointment chat notification worker stopped because the application is shutting down.");
        }
    }

    private async Task ProcessNotificationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
            await notificationService.EnsureChatLifecycleNotificationsAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unable to process appointment chat lifecycle notifications.");
        }
    }
}
