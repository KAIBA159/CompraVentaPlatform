using Microsoft.EntityFrameworkCore;
using Services.Identity.API.Data;

// 1. Inicia el constructor de la aplicación
var builder = WebApplication.CreateBuilder(args);

// 2. CONFIGURACIÓN DE SERVICIOS (Conectar la Base de Datos)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Le avisamos a .NET que busque y prepare nuestros Controladores (como AuthController)
builder.Services.AddControllers();

// Herramientas para que funcione Swagger (la interfaz gráfica de pruebas)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. CONSTRUCCIÓN DE LA APP
var app = builder.Build();

// 4. CONFIGURACIÓN DEL ENTORNO WEB
// Si estamos desarrollando en nuestra máquina local, mostramos Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Activa el guardia de seguridad (para leer tokens más adelante)
app.UseAuthorization();

// Mapea las rutas (hace que las URLs coincidan con tus controladores)
app.MapControllers();

// 5. ¡Arranca el motor y se queda escuchando!
app.Run();