using Microsoft.EntityFrameworkCore;
using SchedulerApp.Api.Data;
using SchedulerApp.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Scheduler")
    ?? $"Data Source={Path.Combine(builder.Environment.ContentRootPath, "scheduler.db")}";

builder.Services.AddDbContext<SchedulerDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDevelopment", policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
{
    var database = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
    await database.Database.MigrateAsync();
}

app.UseCors("AngularDevelopment");
app.MapEmployeesEndpoints();
app.MapShiftsEndpoints();
app.Run();

public partial class Program;
