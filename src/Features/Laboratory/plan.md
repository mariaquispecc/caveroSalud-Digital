Feature: Laboratory

Objetivo:
- Procesar órdenes de laboratorio, registrar resultados y mantener inventario.
- Validar resultados antes de publicación.

Contexto técnico compartido:
- API: ASP.NET Core 8 Web API en C#.
- Persistencia: EF Core + `Npgsql.EntityFrameworkCore.PostgreSQL`.
- Autenticación: Identity + JWT.
- Roles: `Laboratorista`, `Médico`, `Administrador`, `Paciente`, `Farmacéutico`.
- Frontend: Blazor Server.
- Local: Docker Compose con `postgres:16-alpine`.
- Producción: PostgreSQL externo y Azure App Service.

Implementación propuesta:
- Endpoints en `src/Features/Laboratory/Controllers/LaboratoryController.cs` para listar órdenes, registrar resultados y validar.
- Entidades `LabOrder`, `LabResult`, `InventoryItem`, `LabTest`.
- Reglas de negocio:
  - sólo laboratoristas pueden cambiar órdenes a `Validated`.
  - resultados deben existir antes de validar.
  - el inventario reporta bajo stock.
- Blazor Server: páginas para ver órdenes pendientes, registrar resultados y administrar inventario.

Conexión y despliegue:
- `DefaultConnection` desde configuración/env vars.
- App Service App Settings para cadena de conexión y JWT.
- Asegurar acceso desde Azure a la base de datos externa.

CI/CD:
- `checkout`
- `setup-dotnet`
- `dotnet restore`
- `dotnet build`
- `dotnet test --collect:"XPlat Code Coverage"`
- `dotnet publish`
- `azure/webapps-deploy@v3`

Pruebas:
- Unitarias: xUnit + Moq para reglas de validación de resultados e inventario.
- Integración: xUnit + WebApplicationFactory + Testcontainers con `postgres:16-alpine`.
- E2E: Playwright para flujo laboratorista: login, procesar orden, registrar resultado y validar.
- Cobertura: coverlet 95% mínimo. Excluir:
  - `**/obj/**`, `**/bin/**`, `**/Migrations/**`, `**/*Designer.cs`, `**/*g.cs`, `Program.cs`, `Startup.cs`, `appsettings*.json`.

Criterios de aceptación:
- El laboratorista puede validar resultados solo con datos completos.
- El inventario de laboratorio refleja cantidades y alertas de bajo stock.
- El pipeline CI ejecuta pruebas y despliega a Azure.
