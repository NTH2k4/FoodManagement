using FoodManagement.Contracts;
using FoodManagement.HostedServices;
using FoodManagement.Models;
using FoodManagement.Presenters;
using FoodManagement.Repositories;
using FoodManagement.Security;
using FoodManagement.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/"); 
    options.Conventions.AllowAnonymousToPage("/Login/Login");
});

builder.Services.AddHttpContextAccessor();

// Food
builder.Services.AddSingleton<FirebaseFoodRepository>();
builder.Services.AddSingleton<IRepository<FoodDto>>(sp => sp.GetRequiredService<FirebaseFoodRepository>());
builder.Services.AddSingleton<IRealtimeRepository<FoodDto>>(sp => sp.GetRequiredService<FirebaseFoodRepository>());
builder.Services.AddScoped<IService<FoodDto>, FoodService>();
builder.Services.AddHostedService<FirebaseFoodHostedService>();

// User
builder.Services.AddSingleton<FirebaseUserRepository>();
builder.Services.AddSingleton<IRepository<UserDto>>(sp => sp.GetRequiredService<FirebaseUserRepository>());
builder.Services.AddSingleton<IRealtimeRepository<UserDto>>(sp => sp.GetRequiredService<FirebaseUserRepository>());
builder.Services.AddScoped<IService<UserDto>, UserService>();
builder.Services.AddHostedService<FirebaseUserHostedService>();

// Admin
builder.Services.AddSingleton<FirebaseAdminRepository>();
builder.Services.AddSingleton<IRepository<AdminDto>>(sp => sp.GetRequiredService<FirebaseAdminRepository>());
builder.Services.AddSingleton<IRealtimeRepository<AdminDto>>(sp => sp.GetRequiredService<FirebaseAdminRepository>());
builder.Services.AddScoped<IService<AdminDto>, AdminService>();
builder.Services.AddScoped<Func<IListView<AdminDto>, AdminPresenter>>(sp =>
    view => new AdminPresenter(sp.GetRequiredService<IService<AdminDto>>(), view));
builder.Services.AddHostedService<FirebaseAdminHostedService>();

// Booking
builder.Services.AddSingleton<FirebaseBookingRepository>();
builder.Services.AddSingleton<IRepository<BookingDto>>(sp => sp.GetRequiredService<FirebaseBookingRepository>());
builder.Services.AddSingleton<IRealtimeRepository<BookingDto>>(sp => sp.GetRequiredService<FirebaseBookingRepository>());
builder.Services.AddScoped<IService<BookingDto>, BookingService>();
builder.Services.AddHostedService<FirebaseBookingHostedService>();

// Feedback
builder.Services.AddSingleton<FirebaseFeedbackRepository>();
builder.Services.AddSingleton<IRepository<FeedbackDto>>(sp => sp.GetRequiredService<FirebaseFeedbackRepository>());
builder.Services.AddSingleton<IRealtimeRepository<FeedbackDto>>(sp => sp.GetRequiredService<FirebaseFeedbackRepository>());
builder.Services.AddScoped<IService<FeedbackDto>, FeedbackService>();
builder.Services.AddHostedService<FirebaseFeedbackHostedService>();

// Statistic
builder.Services.AddScoped<IStatisticsService, StatisticsService>();

// Dashboard
builder.Services.AddSingleton<IDashboardService, DashboardService>();

// Login
builder.Services.AddScoped<IAdminRepository, FirebaseAdminRepository>();
builder.Services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddScoped<IAuthService, CookieAuthService>();
builder.Services.AddScoped<IAuditService, FirebaseAuditService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Login";
        options.LogoutPath = "/Login/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapGet("/", context =>
{
    context.Response.Redirect("/Login/Login");
    return Task.CompletedTask;
});

app.MapHub<FoodManagement.Hubs.BookingHub>("/hubs/bookings");
app.MapHub<FoodManagement.Hubs.FeedbackHub>("/hubs/feedbacks");
app.MapHub<FoodManagement.Hubs.FoodHub>("/hubs/Foods");
app.MapHub<FoodManagement.Hubs.UserHub>("/hubs/Users");

app.MapRazorPages();

app.Run();
