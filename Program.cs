using ConsumoAPI2.Api.Data;
using ConsumoAPI2.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// 🔥 AGREGAR ESTO PRIMERO - MANEJO DE ERRORES EN STARTUP
try
{
    Console.WriteLine("🚀 INICIANDO APLICACIÓN...");

    // Configurar HttpClient para The Dog API
    builder.Services.AddHttpClient("DogAPI", client =>
    {
        client.BaseAddress = new Uri("https://api.thedogapi.com/v1/");
        client.DefaultRequestHeaders.Add("x-api-key", "live_IO5ZjVjwigVhC3SrfgvEMNe2fB22sSL5H998b9RAtEBkXIkPfRhEDlZQuWbKAcYz");
    });

    // Configurar Entity Framework con PostgreSQL - CON MANEJO DE ERRORES
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    Console.WriteLine($"🔗 Connection String: {!string.IsNullOrEmpty(connectionString)}");
    
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseNpgsql(connectionString);
    });

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
    });

    var app = builder.Build();
    
    app.UseCors("AllowAll");

    // 🔥 ENDPOINT RAÍZ CRÍTICO - SIEMPRE DEBE EXISTIR
    app.MapGet("/", () => "🐕 API de Perros funcionando! Usa /api/dogs para ver razas.");

    // 🔥 ENDPOINT DE HEALTH CHECK
    app.MapGet("/health", () => new { 
        status = "Healthy", 
        timestamp = DateTime.UtcNow,
        database = !string.IsNullOrEmpty(connectionString)
    });

    // 🔥 ENDPOINTS CON THE DOG API
    app.MapGet("/api/dogs", async (IHttpClientFactory httpClientFactory) =>
    {
        try
        {
            var client = httpClientFactory.CreateClient("DogAPI");
            var response = await client.GetAsync("breeds");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Results.Ok(content);
            }
            return Results.StatusCode((int)response.StatusCode);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error: {ex.Message}");
        }
    });

    // GET - Obtener una raza específica por ID
    app.MapGet("/api/dogs/{id}", async (int id, IHttpClientFactory httpClientFactory) =>
    {
        try
        {
            var client = httpClientFactory.CreateClient("DogAPI");
            var response = await client.GetAsync($"breeds/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Results.Ok(content);
            }
            return Results.NotFound();
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error: {ex.Message}");
        }
    });

    // GET - Buscar razas por nombre
    app.MapGet("/api/dogs/search/{name}", async (string name, IHttpClientFactory httpClientFactory) =>
    {
        try
        {
            var client = httpClientFactory.CreateClient("DogAPI");
            var response = await client.GetAsync($"breeds/search?q={name}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Results.Ok(content);
            }
            return Results.NotFound();
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error: {ex.Message}");
        }
    });

    // 🔥 ENDPOINTS PARA FAVORITOS - CON MANEJO DE ERRORES
    app.MapPost("/api/favorites", async (DogFavorite favorite, AppDbContext db) =>
    {
        try
        {
            db.DogFavorites.Add(favorite);
            await db.SaveChangesAsync();
            return Results.Created($"/api/favorites/{favorite.Id}", favorite);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error guardando favorito: {ex.Message}");
        }
    });

    app.MapGet("/api/favorites", async (AppDbContext db) =>
    {
        try
        {
            return Results.Ok(await db.DogFavorites.ToListAsync());
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error obteniendo favoritos: {ex.Message}");
        }
    });

    app.MapDelete("/api/favorites/{id}", async (int id, AppDbContext db) =>
    {
        try
        {
            var favorite = await db.DogFavorites.FindAsync(id);
            if (favorite == null) return Results.NotFound();
            
            db.DogFavorites.Remove(favorite);
            await db.SaveChangesAsync();
            return Results.Ok();
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error eliminando favorito: {ex.Message}");
        }
    });

    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    app.Urls.Add($"http://0.0.0.0:{port}");

    Console.WriteLine("🎉 APLICACIÓN INICIADA CORRECTAMENTE");
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"💥 ERROR CRÍTICO AL INICIAR: {ex.Message}");
    Console.WriteLine($"📄 StackTrace: {ex.StackTrace}");
    throw;
}
