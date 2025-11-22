using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DokuzTasOnlineTurnuva.Models;

namespace DokuzTasOnlineTurnuva.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<Match> Matches { get; set; }
        public DbSet<PlayerStatistic> PlayerStatistics { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<SystemSettings> SystemSettings { get; set; }
        
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            builder.Entity<Match>()
                .HasOne(m => m.Player1)
                .WithMany(u => u.MatchesAsPlayer1)
                .HasForeignKey(m => m.Player1Id)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.Entity<Match>()
                .HasOne(m => m.Player2)
                .WithMany(u => u.MatchesAsPlayer2)
                .HasForeignKey(m => m.Player2Id)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.Entity<PlayerStatistic>()
                .HasOne(ps => ps.User)
                .WithMany(u => u.Statistics)
                .HasForeignKey(ps => ps.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
