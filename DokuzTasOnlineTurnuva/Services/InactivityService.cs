using DokuzTasOnlineTurnuva.Data;
using DokuzTasOnlineTurnuva.Models;
using Microsoft.EntityFrameworkCore;

namespace DokuzTasOnlineTurnuva.Services
{
    public class InactivityService
    {
        private readonly ApplicationDbContext _context;
        
        public InactivityService(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task UpdateActivity(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.LastActiveTime = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        
        public async Task<List<string>> GetInactiveUsers(int minutesThreshold)
        {
            var threshold = DateTime.UtcNow.AddMinutes(-minutesThreshold);
            
            return await _context.Users
                .Where(u => u.LastActiveTime.HasValue 
                    && u.LastActiveTime < threshold 
                    && !string.IsNullOrEmpty(u.CurrentConnectionId))
                .Select(u => u.Id)
                .ToListAsync();
        }
        
        public async Task DisconnectUser(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.CurrentConnectionId = null;
                user.LastActiveTime = null;
                await _context.SaveChangesAsync();
            }
        }
    }
}
