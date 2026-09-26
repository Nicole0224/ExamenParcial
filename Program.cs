using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GestionCreditos.Data;
using GestionCreditos.Models;
using GestionCreditos.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

// Búsqueda con Algolia (solo servidor; la API key nunca sale al navegador).
// Publicación de eventos en PieHost (solo servidor; el secreto nunca sale al navegador).
builder.Services.AddHttpClient();
builder.Services.AddScoped<IIncidenciaSearchService, AlgoliaIncidenciaSearchService>();
builder.Services.AddScoped<IPieHostEventPublisher, PieHostEventPublisher>();

// Caché distribuida: Redis si hay connection string configurada
// (Redis:ConnectionString / Redis__ConnectionString); si no, caché en
// memoria para desarrollo local. El proveedor activo queda en logs.
var redisConnection = builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
}
else
{
    builder.Services.AddDistributedMemoryCache();
}
builder.Services.AddScoped<IIncidenciaListaCacheService, IncidenciaListaCacheService>();

var app = builder.Build();

app.Logger.LogInformation(
    "Proveedor de caché distribuida: {Proveedor}.",
    string.IsNullOrWhiteSpace(redisConnection) ? "memoria (Redis no configurado)" : "Redis");

await DbInitializer.SeedAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapAreaControllerRoute(
    name: "operaciones",
    areaName: "Operaciones",
    pattern: "Operaciones/{controller=Incidencias}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
