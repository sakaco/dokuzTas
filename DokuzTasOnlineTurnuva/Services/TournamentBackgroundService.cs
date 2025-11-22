using DokuzTasOnlineTurnuva.Data;
using Microsoft.EntityFrameworkCore;

namespace DokuzTasOnlineTurnuva.Services
{
    public class TournamentBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TournamentBackgroundService> _logger;
        
        public TournamentBackgroundService(IServiceProvider serviceProvider, ILogger<TournamentBackgroundService> logger)
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
                    await CheckDayTransition();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in tournament background service");
                }
                
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        
        private async Task CheckDayTransition()
        {
            var now = DateTime.Now;
            
            if (now.Hour == 0 && now.Minute == 1)
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                _logger.LogInformation("New day transition at {Time}", now);
            }
        }
    }
}
