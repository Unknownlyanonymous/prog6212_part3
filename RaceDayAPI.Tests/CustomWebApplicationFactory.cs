using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using RaceDayAPI.Data;

namespace RaceDayAPI.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
              d => d.ServiceType == typeof(DbContextOptions<RaceDayContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<RaceDayContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }
}