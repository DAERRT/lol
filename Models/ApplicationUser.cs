using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace lol.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Введите имя")]
        [Display(Name = "Имя")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Введите фамилию")]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Введите группу")]
        [Display(Name = "Группа")]
        public string Group { get; set; }

        // Команды, в которых пользователь является участником
        public ICollection<Team> Teams { get; set; }

        // Команды, где пользователь является создателем
        public ICollection<Team> CreatedTeams { get; set; }

        // Команды, где пользователь является лидером
        public ICollection<Team> LedTeams { get; set; }

        // Путь к аватарке
        public string AvatarPath { get; set; } = "/images/avatars/default.png";
    }
} 