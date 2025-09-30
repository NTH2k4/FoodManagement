using FoodManagement.Contracts;
using FoodManagement.Models;
using FoodManagement.Pages.Login;
using FoodManagement.Presenters;
using Microsoft.Extensions.DependencyInjection;

namespace FoodManagement.Presenters.Adapters
{
    public static class PresenterAdapterDIExtensions
    {
        public static IServiceCollection AddPresenterAdapters(this IServiceCollection services)
        {
            services.AddScoped<Func<ICreateView, IPresenter<UserDto>>>(sp =>
            {
                return view =>
                {
                    var svc = sp.GetRequiredService<IService<UserDto>>();
                    if (view is IListView<UserDto> listView) return new UserPresenter(svc, listView);
                    return new CreateAdapterPresenter<UserDto>(svc, view);
                };
            });

            services.AddScoped<Func<IEditView<UserDto>, IPresenter<UserDto>>>(sp =>
            {
                return view =>
                {
                    var svc = sp.GetRequiredService<IService<UserDto>>();
                    if (view is IListView<UserDto> listView) return new UserPresenter(svc, listView);
                    return new EditAdapterPresenter<UserDto>(svc, view);
                };
            });

            services.AddScoped<Func<ICreateView, IPresenter<FoodDto>>>(sp =>
            {
                return view =>
                {
                    var svc = sp.GetRequiredService<IService<FoodDto>>();
                    if (view is IListView<FoodDto> listView) return new FoodPresenter(svc, listView);
                    return new CreateAdapterPresenter<FoodDto>(svc, view);
                };
            });

            services.AddScoped<Func<IEditView<FoodDto>, IPresenter<FoodDto>>>(sp =>
            {
                return view =>
                {
                    var svc = sp.GetRequiredService<IService<FoodDto>>();
                    if (view is IListView<FoodDto> listView) return new FoodPresenter(svc, listView);
                    return new EditAdapterPresenter<FoodDto>(svc, view);
                };
            });

            services.AddScoped<Func<ICreateView, IPresenter<BookingDto>>>(sp =>
            {
                return view =>
                {
                    var svc = sp.GetRequiredService<IService<BookingDto>>();
                    if (view is IListView<BookingDto> listView) return new BookingPresenter(svc, listView);
                    return new CreateAdapterPresenter<BookingDto>(svc, view);
                };
            });

            services.AddScoped<Func<IEditView<BookingDto>, IPresenter<BookingDto>>>(sp =>
            {
                return view =>
                {
                    var svc = sp.GetRequiredService<IService<BookingDto>>();
                    if (view is IListView<BookingDto> listView) return new BookingPresenter(svc, listView);
                    return new EditAdapterPresenter<BookingDto>(svc, view);
                };
            });

            services.AddScoped<Func<ICreateView, IPresenter<FeedbackDto>>>(sp =>
            {
                return view =>
                {
                    var svc = sp.GetRequiredService<IService<FeedbackDto>>();
                    if (view is IListView<FeedbackDto> listView) return new FeedbackPresenter(svc, listView);
                    return new CreateAdapterPresenter<FeedbackDto>(svc, view);
                };
            });

            services.AddScoped<Func<IEditView<FeedbackDto>, IPresenter<FeedbackDto>>>(sp =>
            {
                return view =>
                {
                    var svc = sp.GetRequiredService<IService<FeedbackDto>>();
                    if (view is IListView<FeedbackDto> listView) return new FeedbackPresenter(svc, listView);
                    return new EditAdapterPresenter<FeedbackDto>(svc, view);
                };
            });

            services.AddScoped<Func<ICreateView, IPresenter<AdminDto>>>(sp =>
            {
                return view =>
                {
                    var svc = sp.GetRequiredService<IAdminService>();
                    if (view is IListView<AdminDto> listView) return new AdminPresenter(svc, listView);
                    return new CreateAdapterPresenter<AdminDto>(svc, view);
                };
            });

            services.AddScoped<Func<IEditView<AdminDto>, IPresenter<AdminDto>>>(sp =>
            {
                return view =>
                {
                    var svc = sp.GetRequiredService<IAdminService>();
                    if (view is IListView<AdminDto> listView) return new AdminPresenter(svc, listView);
                    return new EditAdapterPresenter<AdminDto>(svc, view);
                };
            });
            services.AddScoped<Func<IEditView<AdminDto>, IPresenter<AdminDto>>>(sp =>
            {
                return view =>
                {
                    var svc = sp.GetRequiredService<IAdminService>();
                    if (view is ChangePasswordModel) return new AdminPresenter(svc, view as IListView<AdminDto>);
                    return new EditAdapterPresenter<AdminDto>(svc, view);
                };
            });

            return services;
        }
    }
}
