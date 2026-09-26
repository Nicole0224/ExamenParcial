using GestionCreditos.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestionCreditos.Data;

/// <summary>
/// Aplica migraciones pendientes y carga datos de prueba:
/// incidencias (abiertas y cerradas), rol Supervisor y un usuario supervisor.
/// </summary>
public static class DbInitializer
{
    public const string SupervisorEmail = "supervisor@demo.local";
    public const string SupervisorPassword = "Supervisor123*";
    public const string SupervisorRole = "Supervisor";

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

        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roleManager.RoleExistsAsync(SupervisorRole))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(SupervisorRole));
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "No se pudo crear el rol Supervisor: "
                    + string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            }
        }

        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(SupervisorEmail);
        if (user is null)
        {
            user = new ApplicationUser
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

        if (!await userManager.IsInRoleAsync(user, SupervisorRole))
        {
            var addResult = await userManager.AddToRoleAsync(user, SupervisorRole);
            if (!addResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "No se pudo asignar el rol Supervisor: "
                    + string.Join("; ", addResult.Errors.Select(e => e.Description)));
            }
        }
    }
}
