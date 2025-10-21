using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyChat.Models;
using MyChat.Services;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

builder.Services.AddDbContext<MyChatContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<User, IdentityRole<int>>(opt =>
    {
        opt.Password.RequiredLength = 6;
        opt.Password.RequireDigit = true;
        opt.Password.RequireLowercase = true;
        opt.Password.RequireUppercase = true;
        opt.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<MyChatContext>()
    .AddErrorDescriber<RuIdentityErrors>()
    .AddDefaultTokenProviders();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

var app = builder.Build();

var supportedCultures = new[]
{
    new CultureInfo("en"),
    new CultureInfo("ru")
};

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("ru"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    await AdminInitializer.Seed(sp);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Chat}/{action=Index}/{id?}");

app.Run();