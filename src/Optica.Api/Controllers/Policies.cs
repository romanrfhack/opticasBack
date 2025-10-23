using Microsoft.AspNetCore.Authorization;

namespace Optica.Api.Auth;

public static class Policies
{
    public const string Inventario_Ver = "Inventario.Ver";
    public const string Inventario_Editar = "Inventario.Editar";
    public const string Recetas_Ver = "Recetas.Ver";
    public const string Recetas_Editar = "Recetas.Editar";
    public const string Ordenes_Crear = "Ordenes.Crear";
    public const string Ordenes_Editar = "Ordenes.Editar";
    public const string Usuarios_Admin = "Usuarios.Admin";
    public const string SucursalEncargadoOnly = "SucursalEncargadoOnly";

    public static void Add(AuthorizationOptions options)
    {
        options.AddPolicy(Inventario_Ver, p => p.RequireRole("Admin", "Vendedor", "Optometrista"));
        options.AddPolicy(Inventario_Editar, p => p.RequireRole("Admin", "Vendedor"));
        options.AddPolicy(Recetas_Ver, p => p.RequireRole("Admin", "Optometrista"));
        options.AddPolicy(Recetas_Editar, p => p.RequireRole("Admin", "Optometrista"));
        options.AddPolicy(Ordenes_Crear, p => p.RequireRole("Admin", "Vendedor", "Optometrista"));
        options.AddPolicy(Ordenes_Editar, p => p.RequireRole("Admin", "Vendedor", "Optometrista"));
        options.AddPolicy(Usuarios_Admin, p => p.RequireRole("Admin"));
        options.AddPolicy(SucursalEncargadoOnly, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole("EncargadoSucursal");
            policy.RequireClaim("sucursalId"); 
        });
    }
}
