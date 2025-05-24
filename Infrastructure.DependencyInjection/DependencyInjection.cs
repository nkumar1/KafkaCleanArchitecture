using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Infrastructure.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(
            configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => sqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)),
        ServiceLifetime.Scoped); // Explicitly set as Scoped

            services.AddScoped<IRepository<VehicleLocation>, VehicleLocationRepository>();

            return services;
        }
    }
}
