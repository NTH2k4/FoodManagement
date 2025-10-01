using FoodManagement.Contracts;
using FoodManagement.HostedServices;
using FoodManagement.Models;
using FoodManagement.Presenters;
using FoodManagement.Presenters.Adapters;
using FoodManagement.Repositories;
using FoodManagement.Security;
using FoodManagement.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login/Login");
});

builder.Services.AddHttpContextAccessor();

// Repositories & realtime repositories
builder.Services.AddSingleton<FirebaseFoodRepository>();
builder.Services.AddSingleton<IRepository<FoodDto>>(sp => sp.GetRequiredService<FirebaseFoodRepository>());
builder.Services.AddSingleton<IRealtimeRepository<FoodDto>>(sp => sp.GetRequiredService<FirebaseFoodRepository>());

builder.Services.AddSingleton<FirebaseUserRepository>();
builder.Services.AddSingleton<IRepository<UserDto>>(sp => sp.GetRequiredService<FirebaseUserRepository>());
builder.Services.AddSingleton<IRealtimeRepository<UserDto>>(sp => sp.GetRequiredService<FirebaseUserRepository>());

builder.Services.AddSingleton<FirebaseAdminRepository>();
builder.Services.AddSingleton<IRepository<AdminDto>>(sp => sp.GetRequiredService<FirebaseAdminRepository>());
builder.Services.AddSingleton<IRealtimeRepository<AdminDto>>(sp => sp.GetRequiredService<FirebaseAdminRepository>());

builder.Services.AddSingleton<FirebaseBookingRepository>();
builder.Services.AddSingleton<IRepository<BookingDto>>(sp => sp.GetRequiredService<FirebaseBookingRepository>());
builder.Services.AddSingleton<IRealtimeRepository<BookingDto>>(sp => sp.GetRequiredService<FirebaseBookingRepository>());

builder.Services.AddSingleton<FirebaseFeedbackRepository>();
builder.Services.AddSingleton<IRepository<FeedbackDto>>(sp => sp.GetRequiredService<FirebaseFeedbackRepository>());
builder.Services.AddSingleton<IRealtimeRepository<FeedbackDto>>(sp => sp.GetRequiredService<FirebaseFeedbackRepository>());

// Services (IService<T>)
builder.Services.AddScoped<IService<FoodDto>, FoodService>();
builder.Services.AddScoped<IService<UserDto>, UserService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<IAdminService>(sp => sp.GetRequiredService<AdminService>());
builder.Services.AddScoped<IService<AdminDto>>(sp => sp.GetRequiredService<AdminService>());
builder.Services.AddScoped<IService<BookingDto>, BookingService>();
builder.Services.AddScoped<IService<FeedbackDto>, FeedbackService>();

// Statistics & Dashboard
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
builder.Services.AddSingleton<IDashboardService, DashboardService>();

// Hosted services for realtime sync
builder.Services.AddHostedService<FirebaseFoodHostedService>();
builder.Services.AddHostedService<FirebaseUserHostedService>();
builder.Services.AddHostedService<FirebaseAdminHostedService>();
builder.Services.AddHostedService<FirebaseBookingHostedService>();
builder.Services.AddHostedService<FirebaseFeedbackHostedService>();

// Security / Auth
builder.Services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddScoped<IAuthService, CookieAuthService>();
builder.Services.AddScoped<IAuditService, FirebaseAuditService>();
builder.Services.AddScoped<IAdminRepository>(sp => sp.GetRequiredService<FirebaseAdminRepository>());

// Presenter factories for page models (presenter instances are created per-request via factory)
builder.Services.AddScoped<Func<IListView<AdminDto>, AdminPresenter>>(sp =>
{
    var svc = sp.GetRequiredService<IAdminService>();
    return view => new AdminPresenter(svc, view);
});
builder.Services.AddScoped<Func<IListView<UserDto>, IPresenter<UserDto>>>(sp => view => new UserPresenter(sp.GetRequiredService<IService<UserDto>>(), view));
builder.Services.AddScoped<Func<IListView<BookingDto>, IPresenter<BookingDto>>>(sp => view => new BookingPresenter(sp.GetRequiredService<IService<BookingDto>>(), view));
builder.Services.AddScoped<Func<IListView<FeedbackDto>, IPresenter<FeedbackDto>>>(sp => view => new FeedbackPresenter(sp.GetRequiredService<IService<FeedbackDto>>(), view));
builder.Services.AddScoped<Func<IListView<FoodDto>, IPresenter<FoodDto>>>(sp => view => new FoodPresenter(sp.GetRequiredService<IService<FoodDto>>(), view));

// Factories typed to concrete presenter when PageModel expects that specific presenter
builder.Services.AddScoped<Func<IListView<AdminDto>, IPresenter<AdminDto>>>(sp =>
{
    var factory = sp.GetRequiredService<Func<IListView<AdminDto>, AdminPresenter>>();
    return view => (IPresenter<AdminDto>)factory(view);
});
builder.Services.AddScoped<Func<IListView<UserDto>, UserPresenter>>(sp => view => new UserPresenter(sp.GetRequiredService<IService<UserDto>>(), view));
builder.Services.AddPresenterAdapters();

// Dashboard/Statistics presenter factories
builder.Services.AddScoped<Func<IDashboardView, DashboardPresenter>>(sp => view => new DashboardPresenter(sp.GetRequiredService<IDashboardService>(), view, sp.GetService<ILogger<DashboardPresenter>>()));
builder.Services.AddScoped<Func<IStatisticsView, StatisticsPresenter>>(sp =>
{
    var svc = sp.GetRequiredService<IStatisticsService>();
    var logger = sp.GetService<ILogger<StatisticsPresenter>>();
    return view => new StatisticsPresenter(svc, view, logger);
});


// SignalR
builder.Services.AddSignalR();

// Authentication & Authorization
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

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
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
