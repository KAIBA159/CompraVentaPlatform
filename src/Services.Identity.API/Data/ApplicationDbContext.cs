
using Microsoft.EntityFrameworkCore;
using Services.Identity.API.Models;

namespace Services.Identity.API.Data;

public class ApplicationDbContext : DbContext
{
    // Este es el constructor. Recibe las opciones (como la dirección de la base de datos) 
    // y se las pasa a la clase base (DbContext) para que sepa a dónde conectarse.
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // Aquí le decimos a .NET: "Quiero que mi modelo User se convierta en una tabla llamada Users"
    public DbSet<User> Users { get; set; }
}