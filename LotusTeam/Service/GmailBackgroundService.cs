using Microsoft.Extensions.Hosting;

namespace LotusTeam.Services
{
    public class GmailBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GmailBackgroundService> _logger;

        public GmailBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<GmailBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var gmailService =
                        scope.ServiceProvider.GetRequiredService<GmailService>();

                    await gmailService.CheckUnreadEmailsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background Gmail service crashed");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}