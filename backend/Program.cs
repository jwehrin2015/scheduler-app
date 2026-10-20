using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using SchedulerApp.Api.Data;
using SchedulerApp.Api.Endpoints;
using SchedulerApp.Api.Models;

var builder = WebApplication.CreateBuilder(args);
var databasePath = Path.Combine(builder.Environment.ContentRootPath, "scheduler.db");

builder.Services.AddDbContext<SchedulerDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDevelopment", policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();
app.UseCors("AngularDevelopment");
app.MapEmployeesEndpoints();
app.MapShiftsEndpoints();
app.Run();

public partial class Program;
