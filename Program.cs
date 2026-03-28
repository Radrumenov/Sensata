using Sensata.Models;
using Sensata.Data;



// // using Sensata.Data;
// // using Microsoft.EntityFrameworkCore;

// // var builder = WebApplication.CreateBuilder(args);

// // // Регистрираме базата данни в системата
// // builder.Services.AddDbContext<AppDbContext>();

// // var app = builder.Build();


var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Тази магия казва на .NET: "Търси файлове в папка wwwroot"
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/machines", () => {
    // Това са примерни данни за твоите 31 машини (за тест)
    return Enumerable.Range(1, 31).Select(i => new {
        id = i,
        name = $"Machine {i:D2}",
        status = i % 5 == 0 ? "Red" : "Green", // На всеки 5 машини една е "счупена"
        lastUpdate = DateTime.Now.ToString("HH:mm")
    });
});

app.Run();

