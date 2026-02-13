using F1Predictions.Services.Interfaces;

namespace F1Predictions.Services
{
    /// <summary>
    /// Background service that periodically checks for expired voting windows
    /// and auto-finalizes them.
    /// </summary>
    public class VotingFinalizerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<VotingFinalizerService> _logger;

        public VotingFinalizerService(IServiceProvider serviceProvider, ILogger<VotingFinalizerService> logger)
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
                    var votingService = scope.ServiceProvider.GetRequiredService<IVotingService>();
                    await votingService.FinalizeExpiredVotingWindows();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error finalizing expired voting windows.");
                }

                // Check every 5 minutes
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
