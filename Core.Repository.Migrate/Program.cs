using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Core.Repository.Migrate;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System.Data;



using IHost host = CreateHostBuilder(args).Build();
using var scope = host.Services.CreateScope();

var services = scope.ServiceProvider;

try
{
    services.GetRequiredService<MigrateSchemaService>().Migrate(args).Wait();
}
catch (Exception e)
{
    Console.WriteLine(e.Message);
}

IHostBuilder CreateHostBuilder(string[] strings)
{
    return Host.CreateDefaultBuilder()
        .ConfigureServices((_, services) =>
        {

         
            services.AddSingleton<MigrateSchemaService>();
        }).ConfigureAppConfiguration(app =>
        {
            app.AddJsonFile("appsettings.json");
        });
}