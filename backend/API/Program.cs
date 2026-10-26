using Microsoft.EntityFrameworkCore;
using Data.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<SBTi_Context>(options =>
{
    var connectionString =
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Connection string 'DefaultConnection' saknas.");

    options.UseNpgsql(connectionString);
});

var app = builder.Build();

// HTTP-pipelinen config
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }