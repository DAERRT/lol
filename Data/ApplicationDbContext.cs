using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using lol.Models;

namespace lol.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Team> Teams { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<TeamRequest> TeamRequests { get; set; }
        public DbSet<ProjectApplication> ProjectApplications { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<ChatUser> ChatUsers { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ExpertReview> ExpertReviews { get; set; }
        public DbSet<ProjectExchange> ProjectExchanges { get; set; }
        public DbSet<MessageAttachment> MessageAttachments { get; set; }
        public DbSet<MessageRead> MessageReads { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Настройка связи многие-ко-многим между Team и ApplicationUser
            builder.Entity<Team>()
                .HasMany(t => t.Members)
                .WithMany(u => u.Teams)
                .UsingEntity(j => j.ToTable("TeamMembers"));

            // Настройка связи один-ко-многим для Creator
            builder.Entity<Team>()
                .HasOne(t => t.Creator)
                .WithMany(u => u.CreatedTeams)
                .HasForeignKey(t => t.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Настройка связи один-ко-многим для Leader
            builder.Entity<Team>()
                .HasOne(t => t.Leader)
                .WithMany(u => u.LedTeams)
                .HasForeignKey(t => t.LeaderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Настройка связей для TeamRequest
            builder.Entity<TeamRequest>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TeamRequest>()
                .HasOne(r => r.Team)
                .WithMany()
                .HasForeignKey(r => r.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            // Связь многие-ко-многим между Project и Team (исполнители)
            builder.Entity<Project>()
                .HasMany(p => p.ExecutorTeams)
                .WithMany(t => t.ExecutorProjects)
                .UsingEntity(j => j.ToTable("ProjectExecutors"));

            // Связь многие-ко-многим между Project и ProjectExchange (биржи проектов)
            builder.Entity<Project>()
                .HasMany(p => p.ProjectExchanges)
                .WithMany(e => e.Projects)
                .UsingEntity(j => j.ToTable("ProjectExchangeProjects"));

            // Настройка связи многие-ко-многим между ProjectExchange и Project
            builder.Entity<ProjectExchange>()
                .HasMany(pe => pe.Projects)
                .WithMany(p => p.ProjectExchanges)
                .UsingEntity(j => j.ToTable("ProjectExchangeProjects"));

            // Настройка связи MessageRead -> Message без каскадного удаления
            builder.Entity<MessageRead>()
                .HasOne(mr => mr.Message)
                .WithMany(m => m.Reads)
                .HasForeignKey(mr => mr.MessageId)
                .OnDelete(DeleteBehavior.Restrict);

            // Устанавливаем стоковую аватарку для всех пользователей, у кого она не задана
            builder.Entity<ApplicationUser>().Property(u => u.AvatarPath).HasDefaultValue("/images/avatars/default.png");
        }
    }
} 