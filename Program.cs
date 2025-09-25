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
app.MapHub<FoodManagement.Hubs.FeedbackHub>("/hubs/feedbacks");
app.MapHub<FoodManagement.Hubs.FoodHub>("/hubs/Foods");
app.MapHub<FoodManagement.Hubs.UserHub>("/hubs/Users");

app.MapRazorPages();

app.Run();
