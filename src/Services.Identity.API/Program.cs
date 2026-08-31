using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Services.Identity.API.Data;
using Services.Identity.API.DTOs; // O Configurations, según donde dejaste SapSettings
using Services.Identity.API.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. BASE DE DATOS (SQL Server Express / Azure SQL)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. CONFIGURACIONES CENTRALIZADAS
builder.Services.Configure<SapSettings>(builder.Configuration.GetSection("SapSettings"));

// 3. AUTENTICACIÓN JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ClaveSecretaSuperSeguraPorDefectoParaDesarrolloLocal_2026";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "MakitaPE",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "MakitaClients"
        };
    });

// 4. CORS (Desarrollo local + Preparado para Producción en Azure)
var AllowLocalhostClient = "_allowLocalhostClient";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: AllowLocalhostClient,
                      policy =>
                      {
                          policy.WithOrigins(
                              "http://localhost:5173",
                              "http://localhost:5174",
                              "http://localhost:5175",
                              "https://tu-frontend-react.azurewebsites.net" // <--- Agrega tu URL de Azure aquí cuando despliegues
                          )
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                      });
});

// 5. INYECCIÓN DE DEPENDENCIAS Y CONTROLADORES
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<SapServiceLayerAuth>();
builder.Services.AddTransient<SapArticleService>();

var app = builder.Build();

// 6. PIPELINE DE MIDDLEWARE (El orden es correcto y estricto)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(AllowLocalhostClient);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();