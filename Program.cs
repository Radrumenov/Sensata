using Sensata.Models;
using Sensata.Data;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// Добавя поддръжка за контролери
builder.Services.AddControllers(); 

// Ако ползваш Swagger (за тестване на API-то)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// --- ДОБАВИ ТОЗИ РЕД ---
// Това казва на приложението как да създава AppDbContext и да го дава на контролерите
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=Sensata.db")); 
// -----------------------
var app = builder.Build();

// Казва на приложението да ползва маршрутите на контролерите
app.MapControllers(); 


// --- ДОБАВИ ТЕЗИ ДВА РЕДА ---
app.UseDefaultFiles(); // Казва на сървъра: "Ако някой отвори localhost, зареди му index.html"
app.UseStaticFiles();  // Казва на сървъра: "Разрешавам да се четат файлове от папката wwwroot"
// ----------------------------

// Тези вероятно вече си ги имаш
app.UseAuthorization();
app.MapControllers();




app.Run();
// // using Sensata.Data;
// // using Microsoft.EntityFrameworkCore;

// // var builder = WebApplication.CreateBuilder(args);

// // // Регистрираме базата данни в системата
// // builder.Services.AddDbContext<AppDbContext>();

// // var app = builder.Build();


// var builder = WebApplication.CreateBuilder(args);
// var app = builder.Build();

// // Тази магия казва на .NET: "Търси файлове в папка wwwroot"
// app.UseDefaultFiles();
// app.UseStaticFiles();

// app.MapGet("/api/machines", () => {
//     // Това са примерни данни за твоите 31 машини (за тест)
//     return Enumerable.Range(1, 31).Select(i => new {
//         id = i,
//         name = $"Machine {i:D2}",
//         status = i % 5 == 0 ? "Red" : "Green", // На всеки 5 машини една е "счупена"
//         lastUpdate = DateTime.Now.ToString("HH:mm")
//     });
// });

// app.Run();

