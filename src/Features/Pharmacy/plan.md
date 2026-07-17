Feature: Pharmacy

Objetivo:
- Gestionar stock, recetas y dispensación de medicamentos.
- Asegurar trazabilidad de entregas y control de inventario.

Contexto técnico compartido:
- API: ASP.NET Core 8 Web API en C#.
- Persistencia: EF Core + PostgreSQL Npgsql.
- Autenticación: Identity + JWT.
- Roles: `Farmacéutico`, `Médico`, `Paciente`, `Administrador`.
- Frontend: Blazor Server.
- Local: Docker Compose con `postgres:16-alpine`.
- Producción: PostgreSQL externo y Azure App Service.

Implementación propuesta:
- Endpoints en `src/Features/Pharmacy/Controllers/PharmacyController.cs` para recetas, entrega y stock.
- Entidades `Prescription`, `PrescriptionItem`, `InventoryItem`, `Medication`.
- Reglas de negocio:
  - sólo farmacéuticos dispensan recetas.
  - la entrega descuenta inventario.
  - no se permite dispensar si no hay stock suficiente.
- Blazor Server: interfaz de farmacia para ver recetas pendientes y actualizar inventario.

Conexión y despliegue:
- Configuración de DB a través de `DefaultConnection` y variables de entorno.
- App Service usa App Settings para la cadena de conexión y JWT.
- El proveedor de PostgreSQL externo debe permitir el acceso desde Azure.

CI/CD:
- `checkout`
- `setup-dotnet`
- `dotnet restore`
- `dotnet build`
- `dotnet test --collect:"XPlat Code Coverage"`
- `dotnet publish`
- `azure/webapps-deploy@v3`

Pruebas:
- Unitarias: xUnit + Moq para reglas de inventario, validación de entregas y cálculos de stock.
- Integración: xUnit + WebApplicationFactory + Testcontainers usando `postgres:16-alpine`.
- E2E: Playwright para flujo farmacéutico: login, ver receta pendiente, entregar y verificar stock.
- Cobertura: coverlet 95% mínimo. Excluir:
  - `**/obj/**`, `**/bin/**`, `**/Migrations/**`, `**/*Designer.cs`, `**/*g.cs`, `Program.cs`, `Startup.cs`, `appsettings*.json`.

Criterios de aceptación:
- El farmacéutico puede dispensar recetas y el inventario se actualiza.
- Se bloquean entregas con stock insuficiente.
- Los endpoints funcionan con PostgreSQL real en integración.
