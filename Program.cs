using FoodManagement.Contracts;
using FoodManagement.HostedServices;
using FoodManagement.Models;
using FoodManagement.Repositories;
using FoodManagement.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddHttpContextAccessor();

// Food
builder.Services.AddScoped<IRepository<FoodDto>, FirebaseFoodRepository>(IConfiguration => new FirebaseFoodRepository(IConfiguration.GetRequiredService<IConfiguration>()));
builder.Services.AddScoped<IService<FoodDto>, FoodService>();

// User
builder.Services.AddScoped<IRepository<UserDto>, FirebaseUserRepository>(IConfiguration => new FirebaseUserRepository(IConfiguration.GetRequiredService<IConfiguration>()));
builder.Services.AddScoped<IService<UserDto>, UserService>();

// Booking
builder.Services.AddSingleton<FirebaseBookingRepository>();
builder.Services.AddSingleton<IRepository<BookingDto>>(sp => sp.GetRequiredService<FirebaseBookingRepository>());
builder.Services.AddSingleton<IRealtimeRepository<BookingDto>>(sp => sp.GetRequiredService<FirebaseBookingRepository>());
builder.Services.AddScoped<IService<BookingDto>, BookingService>();
builder.Services.AddHostedService<FirebaseBookingHostedService>();

// Feedback
builder.Services.AddScoped<IRepository<FeedbackDto>, FirebaseFeedbackRepository>(IConfiguration => new FirebaseFeedbackRepository(IConfiguration.GetRequiredService<IConfiguration>()));
builder.Services.AddScoped<IService<FeedbackDto>, FeedbackService>();

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

app.UseAuthorization();

app.MapHub<FoodManagement.Hubs.BookingHub>("/hubs/bookings");

app.MapRazorPages();

app.Run();
