using ScaleManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace ScaleManager.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : base(options)
    {
    }
    public DbSet<UserAvailability> UserAvailabilities { get; set; }
    // Removendo GeneralAvailability, pois será substituído por ScaleDay
    // public DbSet<GeneralAvailability> GeneralAvailabilities { get; set; }
    public DbSet<Scale> Scale { get; set; }
    // Mudando para UserMinistry para representar a relação entre usuários e ministérios
    public DbSet<UserMinistry> UserMinistries { get; set; }
    // Adicionando DbSet para Ministry
    public DbSet<Ministry> Ministries { get; set; }
    // Adicionando DbSet para ScaleDay
    public DbSet<ScaleDay> ScaleDays { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserAvailability>()
            .HasIndex(ua => new { ua.UserId, ua.Date })
            .IsUnique();


        builder.Entity<Scale>()
            .HasIndex(s => new { s.Date, s.Team })
            .IsUnique();

        // Configurando a chave primária composta para UserMinistry
        builder.Entity<UserMinistry>()
            .HasKey(um => new { um.UserId, um.MinistryId });

        // Configurando o relacionamento entre Scale e ScaleDay
        builder.Entity<Scale>()
            .HasOne(s => s.ScaleDay)
            .WithMany(sd => sd.Scales)
            .HasForeignKey(s => s.ScaleDayId)
            .OnDelete(DeleteBehavior.Cascade); // Defina o comportamento de exclusão conforme necessário
    }

    // Adicionando propriedades DbSet que estavam faltando
    public DbSet<ApplicationUser> ApplicationUsers { get; set; } // Já herda de IdentityDbContext, mas pode ser útil ter explicitamente
}

