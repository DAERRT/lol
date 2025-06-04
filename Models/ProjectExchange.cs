using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace lol.Models
{
    public class ProjectExchange
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Название биржи")]
        [StringLength(200)]
        public string Name { get; set; }

        [Display(Name = "Дата начала")]
        public DateTime StartDate { get; set; }

        [Display(Name = "Дата окончания")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Активная")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Проекты")]
        public ICollection<Project> Projects { get; set; } = new List<Project>();
    }
} 