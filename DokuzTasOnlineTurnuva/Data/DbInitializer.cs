using Microsoft.AspNetCore.Identity;
using DokuzTasOnlineTurnuva.Models;

namespace DokuzTasOnlineTurnuva.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager, 
            RoleManager<IdentityRole> roleManager)
        {
            context.Database.EnsureCreated();
            
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }
            
            if (!await roleManager.RoleExistsAsync("Player"))
            {
                await roleManager.CreateAsync(new IdentityRole("Player"));
            }
            
            if (!context.Users.Any())
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin",
                    Email = "admin@dokuztas.com",
                    EmailConfirmed = true
                };
                
                await userManager.CreateAsync(admin, "admin123");
                await userManager.AddToRoleAsync(admin, "Admin");
            }
            
            if (!context.SystemSettings.Any())
            {
                context.SystemSettings.Add(new SystemSettings());
                await context.SaveChangesAsync();
            }
            
            if (!context.Questions.Any())
            {
                var questions = new List<Question>
                {
                    new Question { Text = "Türkiye'nin başkenti neresidir?", Category = "Sosyal Bilgiler", Option1 = "Ankara", Option2 = "İstanbul", Option3 = "İzmir", Option4 = "Bursa", CorrectAnswer = 0 },
                    new Question { Text = "5 + 7 = ?", Category = "Matematik", Option1 = "11", Option2 = "12", Option3 = "13", Option4 = "14", CorrectAnswer = 1 },
                    new Question { Text = "Güneş bir yıldız mıdır?", Category = "Fen Bilgisi", Option1 = "Evet", Option2 = "Hayır", CorrectAnswer = 0 },
                    new Question { Text = "Bir dakikada kaç saniye vardır?", Category = "Matematik", Option1 = "30", Option2 = "60", Option3 = "90", Option4 = "120", CorrectAnswer = 1 },
                    new Question { Text = "Dünyanın en büyük okyanusu hangisidir?", Category = "Sosyal Bilgiler", Option1 = "Atlas", Option2 = "Hint", Option3 = "Pasifik", Option4 = "Arktik", CorrectAnswer = 2 },
                };
                
                context.Questions.AddRange(questions);
                await context.SaveChangesAsync();
            }
        }
    }
}
