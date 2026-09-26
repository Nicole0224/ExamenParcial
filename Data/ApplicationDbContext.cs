using GestionCreditos.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GestionCreditos.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Incidencia> Incidencias => Set<Incidencia>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Incidencia>(entity =>
        {
            entity.Property(i => i.Estacion)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(i => i.Descripcion)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(i => i.Prioridad)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(10);

            entity.Property(i => i.Estado)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(10)
                .HasDefaultValue(EstadoIncidencia.Abierta);
        });
    }
}
