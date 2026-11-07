using Microsoft.Extensions.Options;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// The MVC app's own session holds the signed-in user's ID, role and name, plus the API session cookie.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = SessionKeys.CookieName;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    // Matches the API's idle timeout, so both sessions expire together.
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});
builder.Services.AddHttpContextAccessor();

// Typed HttpClient for the Part 2 API. Cookies are handled by ApiSessionCookieHandler per user,
// so the shared handler must not keep its own cookie container.
builder.Services.Configure<RaceDayApiOptions>(builder.Configuration.GetSection(RaceDayApiOptions.SectionName));
builder.Services.AddTransient<ApiSessionCookieHandler>();
builder.Services.AddHttpClient<RaceDayApiClient>((services, client) =>
    {
        var options = services.GetRequiredService<IOptions<RaceDayApiOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false })
    .AddHttpMessageHandler<ApiSessionCookieHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
