using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using Optica.Api.Auth;
using Optica.Domain.Entities;
using Optica.Domain.Enums;
using Optica.Infrastructure;
using Optica.Infrastructure.Identity;
using Optica.Infrastructure.Persistence;

const string CorsLocal = "CorsLocal";
const string CorsProd = "CorsProd";

var builder = WebApplication.CreateBuilder(args);

// Infraestructura (registra AppDbContext con la connection string)
builder.Services.AddInfrastructure(builder.Configuration);

// Identity + Roles
builder.Services
    .AddIdentityCore<AppUser>(opt =>
    {
        opt.Password.RequiredLength = 6;
        opt.Password.RequireNonAlphanumeric = false;
        opt.Password.RequireUppercase = false;
        opt.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager<SignInManager<AppUser>>()
    .AddDefaultTokenProviders();

// Controllers
builder.Services.AddControllers();

// CORS: políticas separadas para dev y prod
builder.Services.AddCors(opts =>
{
    // Desarrollo: Angular local
    opts.AddPolicy(CorsLocal, b => b
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()
    // Si usaras cookies cross-site, descomenta:
    // .AllowCredentials()
    );

    // Producción: Angular en Azure Static Web Apps / dominio propio
    opts.AddPolicy(CorsProd, b => b
        .WithOrigins(
            "https://opticaapi20250919155555.azurewebsites.net",
            "https://<tu-dominio>" // opcional, si tienes dominio
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
    // .AllowCredentials() si vas a usar cookies cross-site
    );
});

// JWT
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwt = new JwtOptions();
builder.Configuration.GetSection(JwtOptions.SectionName).Bind(jwt);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options => { Policies.Add(options); });
builder.Services.AddScoped<JwtTokenService>();

// Swagger + JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Optica API", Version = "v1" });

    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme
        }
    };
    c.AddSecurityDefinition(jwtScheme.Reference.Id, jwtScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtScheme, Array.Empty<string>() }
    });
});

var app = builder.Build();

// Migraciones + datos demo
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await SeedData(db, sp);
}

// Pipeline
// ↓ CORS debe ir antes de Auth
app.UseCors(app.Environment.IsDevelopment() ? CorsLocal : CorsProd);

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static async Task SeedData(AppDbContext db, IServiceProvider sp)
{
    // Sucursales
    if (!await db.Sucursales.AnyAsync())
    {
        db.Sucursales.AddRange(
            new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Nombre = "Sucursal Centro" },
            new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Nombre = "Sucursal Norte" }
        );
    }

    // Productos demo
    if (!await db.Productos.AnyAsync())
    {
        db.Productos.AddRange(
            new() { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), Sku = "ARZ-001", Nombre = "Armazón Clásico Negro", Categoria = CategoriaProducto.Armazon },
            new() { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), Sku = "ACC-001", Nombre = "Estuche Rígido", Categoria = CategoriaProducto.Accesorio }
        );
    }

    // Inventarios demo
    if (!await db.Inventarios.AnyAsync())
    {
        db.Inventarios.AddRange(
            new() { Id = Guid.NewGuid(), ProductoId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), SucursalId = Guid.Parse("11111111-1111-1111-1111-111111111111"), Stock = 5, StockMin = 2 },
            new() { Id = Guid.NewGuid(), ProductoId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), SucursalId = Guid.Parse("22222222-2222-2222-2222-222222222222"), Stock = 3, StockMin = 2 },
            new() { Id = Guid.NewGuid(), ProductoId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), SucursalId = Guid.Parse("11111111-1111-1111-1111-111111111111"), Stock = 10, StockMin = 4 }
        );
    }

    await db.SaveChangesAsync();

    // Seed Identity (roles + admin)
    var userMgr = sp.GetRequiredService<UserManager<AppUser>>();
    var roleMgr = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    foreach (var role in new[] { "Admin", "Vendedor", "Optometrista" })
        if (!await roleMgr.RoleExistsAsync(role))
            await roleMgr.CreateAsync(new IdentityRole<Guid>(role));

    var adminEmail = "admin@optica.local";
    var admin = await userMgr.FindByEmailAsync(adminEmail);
    if (admin is null)
    {
        admin = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = "admin",
            Email = adminEmail,
            EmailConfirmed = true,
            FullName = "Administrador",
            SucursalId = Guid.Parse("11111111-1111-1111-1111-111111111111")
        };
        await userMgr.CreateAsync(admin, "Admin123!");
        await userMgr.AddToRoleAsync(admin, "Admin");
    }
}
