using DokuzTasOnlineTurnuva.Data;
using DokuzTasOnlineTurnuva.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace DokuzTasOnlineTurnuva.Services
{
    public class InactivityMonitorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InactivityMonitorService> _logger;
        
        public InactivityMonitorService(IServiceProvider serviceProvider, ILogger<InactivityMonitorService> logger)
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
                    await CheckInactiveUsers();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in inactivity monitor service");
                }
                
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        
        private async Task CheckInactiveUsers()
        {
            using var scope = _serviceProvider.CreateScope();
            var inactivityService = scope.ServiceProvider.GetRequiredService<InactivityService>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<GameHub>>();
            
            var settings = await context.SystemSettings.FirstOrDefaultAsync() ?? new Models.SystemSettings();
            var inactiveUsers = await inactivityService.GetInactiveUsers(settings.InactivityLimit);
            
            foreach (var userId in inactiveUsers)
            {
                var user = await context.Users.FindAsync(userId);
                if (user?.CurrentConnectionId != null)
                {
                    await hubContext.Clients.Client(user.CurrentConnectionId).SendAsync("ForceDisconnect", "Aktivite yok");
                    await inactivityService.DisconnectUser(userId);
                    _logger.LogInformation("User {UserId} disconnected due to inactivity", userId);
                }
            }
        }
    }
}
