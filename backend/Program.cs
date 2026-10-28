using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using SeconSbtiBackend.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(dbConnectionString));

var app = builder.Build();

// Exit right away with a readable explanation if the database can't be reached
var dbConnectionError = await DatabaseConnectionCheck.GetConnectionErrorAsync(dbConnectionString, app.Environment.EnvironmentName);

if (dbConnectionError is not null)
{
    Console.Error.WriteLine(dbConnectionError);
    return 1;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

return 0;
