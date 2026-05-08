using ControlPyme.Api.Data;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);



// Add services to the container.
// DB
/*
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
*/

var envConnection = Environment.GetEnvironmentVariable("DB_CONNECTION");
var configConnection = builder.Configuration.GetConnectionString("DefaultConnection");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

Console.WriteLine("ENV:");
Console.WriteLine(envConnection);

Console.WriteLine("CONFIG:");
Console.WriteLine(configConnection);

//var connectionString = envConnection ?? configConnection;

Console.WriteLine("FINAL:");
Console.WriteLine(connectionString);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("¡La cadena de conexión no se encontró en los secretos!");
}

/*
var connectionString =
    Environment.GetEnvironmentVariable("DB_CONNECTION")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

Console.WriteLine(connectionString);
*/
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var port = Environment.GetEnvironmentVariable("PORT");

if (!string.IsNullOrEmpty(port))
{
    app.Urls.Add($"http://*:{port}");
}

/* 
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000"; 
app.Urls.Add($"http://*:{port}");
*/


// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
