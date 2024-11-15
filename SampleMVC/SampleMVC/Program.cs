using Core.Repository.Config;
using Core.Repository.Entity;
using Core.Repository.SqlGenerator;
using MySqlConnector;
using SampleMVC.Repository;
using SampleMVC.Services;
using System.Data;
using System.Data.SqlTypes;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration["ConnectionSettings:DefaultConnection"];


var settings = builder.Configuration.GetSection("EntityConfig").Get<CoreRepositoryConfig>();
RepositoryOrmConfig.Config = settings;


builder.Services.AddTransient<IDbConnection>(db => new MySqlConnection(connectionString));

builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<ISqlGenerator<User>, SqlGenerator<User>>();
builder.Services.AddScoped<IUserService, UserService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();



