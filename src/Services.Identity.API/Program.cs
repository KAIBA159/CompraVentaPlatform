using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Services.Identity.API.Data;
using Services.Identity.API.DTOs;
using Services.Identity.API.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. BASE DE DATOS (SQL Server Express local / Azure SQL Serverless en producción)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            // Clave para Azure SQL Serverless (Free Tier): reintenta si la BD está despertando de autopausa
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(15),
                errorNumbersToAdd: null);
        }));

// 2. CONFIGURACIONES CENTRALIZADAS Y CONTROL GLOBAL DE HILOS SAP
builder.Services.Configure<SapSettings>(builder.Configuration.GetSection("SapSettings"));
builder.Services.AddSingleton<SapConcurrencyService>();

// 3. AUTENTICACIÓN JWT
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? "ClaveSecretaSuperSeguraPorDefectoParaDesarrolloLocal_2026";

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

builder.Services.AddAuthorization();

// 4. CORS (Desarrollo local + Producción en Azure)
const string AllowLocalhostClient = "_allowLocalhostClient";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: AllowLocalhostClient, policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:5174",
                "http://localhost:5175",
                "https://tu-frontend-react.azurewebsites.net"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// 5. INYECCIÓN DE DEPENDENCIAS Y CONTROLADORES
builder.Services.AddControllers(options =>
{
    // Evita que [ApiController] rechace con HTTP 400 campos string vacíos ("") provenientes del Excel
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Si SapServiceLayerAuth guarda la cookie en un campo de instancia (no static), cámbialo a AddSingleton
builder.Services.AddTransient<SapServiceLayerAuth>();
builder.Services.AddTransient<SapArticleService>();
builder.Services.AddTransient<SapSocioNegocioService>();

var app = builder.Build();

// 6. PIPELINE DE MIDDLEWARE (Orden estricto)
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