using Sensata.Data;
using Sensata.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// База данни
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=Sensata.db"));

// CopilotService — чете GeminiSettings от appsettings.json
// ПРАВИЛНО
builder.Services.AddHttpClient<CopilotService>();
builder.Services.AddScoped<DashboardDataService>();

// DashboardDataService
builder.Services.AddScoped<DashboardDataService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();

app.Run();