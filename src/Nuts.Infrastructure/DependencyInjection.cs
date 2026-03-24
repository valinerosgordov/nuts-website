using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nuts.Application.Account;
using Nuts.Application.Common;
using Nuts.Application.Contacts;
using Nuts.Application.Media;
using Nuts.Application.Products;
using Nuts.Infrastructure.Persistence;
using Nuts.Infrastructure.Persistence.Repositories;
using Nuts.Infrastructure.Services;

namespace Nuts.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=nuts.db";

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IMediaMentionRepository, MediaMentionRepository>();
        services.AddScoped<IContactRequestRepository, ContactRequestRepository>();
        services.AddScoped<IPageRepository, PageRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IProductExcelService, ProductExcelService>();

        return services;
    }
}
