using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using lol.Models;
using Microsoft.Extensions.Logging;

namespace lol.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("DbInitializer");
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                // Проверяем, что база данных создана
                await context.Database.EnsureCreatedAsync();
                logger.LogInformation("База данных создана или уже существует");

                // Список ролей
                string[] roleNames = { "Администратор", "Студент", "Заказчик", "Эксперт", "Преподаватель", "Новый пользователь", "Тимлид" };

                // Создаем роли, если они не существуют
                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                        if (result.Succeeded)
                        {
                            logger.LogInformation($"Роль {roleName} успешно создана");
                        }
                        else
                        {
                            logger.LogError($"Ошибка при создании роли {roleName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                        }
                    }
                }

                // Создаем администратора, если его нет
                var adminEmail = "admin@example.com";
                var adminUser = await userManager.FindByEmailAsync(adminEmail);

                if (adminUser == null)
                {
                    var admin = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true,
                        FirstName = "Администратор",
                        LastName = "Системы",
                        Group = "Администрация",
                        PhoneNumber = "+79001234567"
                    };

                    var result = await userManager.CreateAsync(admin, "Admin123!");

                    if (result.Succeeded)
                    {
                        logger.LogInformation("Администратор успешно создан");
                        var roleResult = await userManager.AddToRoleAsync(admin, "Администратор");
                        if (roleResult.Succeeded)
                        {
                            logger.LogInformation("Роль Администратор успешно назначена");
                        }
                        else
                        {
                            logger.LogError($"Ошибка при назначении роли Администратор: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                        }
                    }
                    else
                    {
                        logger.LogError($"Ошибка при создании администратора: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Произошла ошибка при инициализации базы данных");
                throw;
            }
        }
    }
} 