// Importante: Agregar el namespace de seguridad y configuración
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Services.Identity.API.Data;
using Services.Identity.API.Services;
using System.Text;

// 1. Inicia el constructor de la aplicación
var builder = WebApplication.CreateBuilder(args);

// 2. CONFIGURACIÓN DE SERVICIOS

// 2a. Conectar la Base de Datos (SQL Server Express / Azure SQL)
// Usamos la cadena de conexión definida en appsettings.json (o variables de entorno en Azure)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2b. Configuración de Autenticación (Requerido para el módulo de Login)
// Aquí definimos que usaremos JWT Bearer para proteger las rutas
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            // NOTA: Estas claves deben venir de un lugar seguro en producción (ej. Azure Key Vault)
            // Por ahora, para tu autenticación propia, se leerán desde el appsettings.json
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

// 2c. Configuración de CORS (Crucial para el entorno Web)
// Dado que tu Front-end en React (puerto 517X) y Back-end (puerto 5243) son aplicaciones separadas,
// el navegador bloqueará las peticiones si no habilitamos CORS explícitamente.
var AllowLocalhostClient = "_allowLocalhostClient";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: AllowLocalhostClient,
                      policy =>
                      {
                          // Habilitamos el puerto donde estará corriendo React localmente.
                          // En producción, esto se actualizará dinámicamente por App Service.
                          policy.WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:5175") // Agrega tus posibles puertos de Vite
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

// 2d. Servicios estándar de la API
builder.Services.AddControllers();
// Herramientas para que funcione Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<SapServiceLayerAuth>();
builder.Services.AddTransient<SapArticleService>();



// 3. CONSTRUCCIÓN DE LA APP
var app = builder.Build();

// 4. CONFIGURACIÓN DEL PIPELINE DE MIDDLEWARE (El orden importa)

// 4a. Entorno de Desarrollo: Swagger
// Se recomienda dejar esto solo en desarrollo para no exponer la API en producción
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 4b. Seguridad web obligatoria para Azure
app.UseHttpsRedirection();

// 4c. Aplicar la política de CORS (Debe ir ANTES de Authentication y Authorization)
app.UseCors(AllowLocalhostClient);

// 4d. Activar el sistema de Autenticación
app.UseAuthentication();

// 4e. Activar el guardia de seguridad (Autorización)
app.UseAuthorization();

// 4f. Mapea las rutas de tus controladores
app.MapControllers();

// 5. ¡Arranca el motor y se queda escuchando!
app.Run();