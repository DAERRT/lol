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

                var student1Email = "olegoviz.2006@gmail.com";
                var student1password = "20062018-Egor";
                var student1 = await userManager.FindByEmailAsync(student1Email);
                var student2Email = "student2@test.com";
                var student2password = "20022018-Egor";
                var student2 = await userManager.FindByEmailAsync(student2Email);
                var customerEmail = "customer@test.com";
                var customerPassword = "20022018-Egor";
                var customer = await userManager.FindByEmailAsync(customerEmail);
                var expert1Email = "expert1@test.com";
                var expert1Password = "20022018-Egor";
                var expert1 = await userManager.FindByEmailAsync(expert1Email);
                var expert2Email = "expert2@test.com";
                var expert2Password = "20022018-Egor";
                var expert2 = await userManager.FindByEmailAsync(expert2Email);
                var expert3Email = "expert3@test.com";
                var expert3Password = "20022018-Egor";
                var expert3 = await userManager.FindByEmailAsync(expert3Email);

                if (student1 == null)
                {
                    student1 = new ApplicationUser
                    {
                        UserName = student1Email,
                        Email = student1Email,
                        EmailConfirmed = true,
                        FirstName = "Егор",
                        LastName = "Олегович",
                        Group = "ПИ-221",
                        PhoneNumber = "+79001234567"
                    };
                    var result = await userManager.CreateAsync(student1, student1password);
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Студент 1 успешно создан");
                        await userManager.AddToRoleAsync(student1, "Студент");
                    }
                }

                if (student2 == null)
                {
                    student2 = new ApplicationUser
                    {
                        UserName = student2Email,
                        Email = student2Email,
                        EmailConfirmed = true,
                        FirstName = "Егор",
                        LastName = "Олегович",
                        Group = "ПИ-221",
                        PhoneNumber = "+79001234567"
                    };
                    var result = await userManager.CreateAsync(student2, student2password);
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Студент 2 успешно создан");
                        await userManager.AddToRoleAsync(student2, "Студент");
                    }
                }

                if (customer == null)
                {
                    customer = new ApplicationUser
                    {
                        UserName = customerEmail,
                        Email = customerEmail,
                        EmailConfirmed = true,
                        FirstName = "Заказчик",
                        LastName = "Проекта",
                        Group = "Заказчики",
                        PhoneNumber = "+79001234567"
                    };
                    var result = await userManager.CreateAsync(customer, customerPassword);
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Заказчик успешно создан");
                        await userManager.AddToRoleAsync(customer, "Заказчик");
                    }
                }

                if (expert1 == null)
                {
                    expert1 = new ApplicationUser
                    {
                        UserName = expert1Email,
                        Email = expert1Email,
                        EmailConfirmed = true,
                        FirstName = "Эксперт",
                        LastName = "Первый",
                        Group = "Эксперты",
                        PhoneNumber = "+79001234567"
                    };
                    var result = await userManager.CreateAsync(expert1, expert1Password);
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Эксперт 1 успешно создан");
                        await userManager.AddToRoleAsync(expert1, "Эксперт");
                    }
                }

                if (expert2 == null)
                {
                    expert2 = new ApplicationUser
                    {
                        UserName = expert2Email,
                        Email = expert2Email,
                        EmailConfirmed = true,
                        FirstName = "Эксперт",
                        LastName = "Второй",
                        Group = "Эксперты",
                        PhoneNumber = "+79001234567"
                    };
                    var result = await userManager.CreateAsync(expert2, expert2Password);
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Эксперт 2 успешно создан");
                        await userManager.AddToRoleAsync(expert2, "Эксперт");
                    }
                }

                if (expert3 == null)
                {
                    expert3 = new ApplicationUser
                    {
                        UserName = expert3Email,
                        Email = expert3Email,
                        EmailConfirmed = true,
                        FirstName = "Эксперт",
                        LastName = "Третий",
                        Group = "Эксперты",
                        PhoneNumber = "+79001234567"
                    };
                    var result = await userManager.CreateAsync(expert3, expert3Password);
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Эксперт 3 успешно создан");
                        await userManager.AddToRoleAsync(expert3, "Эксперт");
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