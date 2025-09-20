## Optica API (.NET 8 + EF Core + SQL Server)

### Pasos
```bash
dotnet restore
dotnet build

# (opcional, para tener archivos de migración)
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate -p src/Optica.Infrastructure -s src/Optica.Api
dotnet ef database update -p src/Optica.Infrastructure -s src/Optica.Api

# Ejecutar
dotnet run --project src/Optica.Api
```
Ajusta la cadena de conexión en `src/Optica.Api/appsettings.json`.
Prueba en Swagger `/swagger` y endpoint `/api/inventory/search`.
Header de dev: `X-Sucursal-Id: 11111111-1111-1111-1111-111111111111`.
