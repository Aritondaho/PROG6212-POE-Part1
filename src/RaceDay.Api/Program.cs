using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Security;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();

builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// PBKDF2 password hashing from ASP.NET Core Identity. The hasher is stateless, so a singleton is safe.
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

// Banner images are stored locally in Part 2. Part 3 swaps this for Azure Blob Storage.
builder.Services.AddSingleton<IBannerStorage, LocalBannerStorage>();

// Server-side session state. The session stores only the user's ID and role, keyed by an opaque cookie.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = SessionKeys.CookieName;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

var app = builder.Build();

app.UseExceptionHandler();

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseSession();
app.MapControllers();

// Create or update the schema from the migrations, then load sample data in Development.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    await db.Database.MigrateAsync();

    if (app.Environment.IsDevelopment())
    {
        await DbSeeder.SeedAsync(db);
    }
}

app.Run();

// Exposes the entry point to the test project's WebApplicationFactory.
public partial class Program;
