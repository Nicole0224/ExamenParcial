using GestionCreditos.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestionCreditos.Data;

/// <summary>
/// Aplica migraciones pendientes y carga datos de prueba:
/// incidencias (abiertas y cerradas) y un usuario para entrar al sistema.
/// </summary>
public static class DbInitializer
{
    public const string SupervisorEmail = "supervisor@demo.local";
    public const string SupervisorPassword = "Supervisor123*";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var provider = scope.ServiceProvider;

        var db = provider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        if (!await db.Incidencias.AnyAsync())
        {
            db.Incidencias.AddRange(
                new Incidencia
                {
                    Id = 1,
                    Estacion = "Central",
                    Descripcion = "Freno delantero defectuoso",
                    Prioridad = PrioridadIncidencia.Alta,
                    Estado = EstadoIncidencia.Abierta
                },
                new Incidencia
                {
                    Id = 2,
                    Estacion = "Norte",
                    Descripcion = "Pinchazo en rueda trasera",
                    Prioridad = PrioridadIncidencia.Media,
                    Estado = EstadoIncidencia.Abierta
                },
                new Incidencia
                {
                    Id = 3,
                    Estacion = "Central",
                    Descripcion = "Sillín roto",
                    Prioridad = PrioridadIncidencia.Baja,
                    Estado = EstadoIncidencia.Abierta
                },
                new Incidencia
                {
                    Id = 4,
                    Estacion = "Sur",
                    Descripcion = "Cadena oxidada",
                    Prioridad = PrioridadIncidencia.Media,
                    Estado = EstadoIncidencia.Cerrada
                },
                new Incidencia
                {
                    Id = 5,
                    Estacion = "Este",
                    Descripcion = "Luz delantera no enciende",
                    Prioridad = PrioridadIncidencia.Baja,
                    Estado = EstadoIncidencia.Abierta
                });
            await db.SaveChangesAsync();
        }

        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(SupervisorEmail) is null)
        {
            var user = new ApplicationUser
            {
                UserName = SupervisorEmail,
                Email = SupervisorEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, SupervisorPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "No se pudo crear el usuario de prueba: "
                    + string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
