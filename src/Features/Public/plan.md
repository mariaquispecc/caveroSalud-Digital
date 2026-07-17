Feature: Public

Objetivo:
- Mostrar la información institucional, especialidades disponibles y pasos para agendar sin autenticación.
- Servir de entrada para nuevos pacientes y mejorar confianza pública.

Contexto técnico compartido:
- API: ASP.NET Core 8 Web API en C#.
- Persistencia: EF Core + Npgsql.
- Frontend: Blazor Server con páginas públicas estáticas/dinámicas.
- Desarrollo local: PostgreSQL en Docker Compose `postgres:16-alpine`.
- Producción: Azure App Service con base de datos externa.
- Configuración segura de conexiones y secretos.

Implementación propuesta:
- `PublicController` en `src/Features/Public/Controllers` con endpoints públicos para información institucional y especialidades.
- Entidades `PublicInfo` y `Speciality` en `Domain`.
- Páginas Blazor públicas en `wwwroot/public` o en rutas públicas del Blazor Server.
- El módulo debe permitir que el administrador actualice este contenido desde el panel privado.
- El contenido público debe reflejar inmediatamente los cambios administrativos y ser visible sin autenticación.

Conexión y despliegue:
- La información se carga desde la base de datos mediante `DefaultConnection`.
- En Azure App Service, definir App Settings para `ConnectionStrings:DefaultConnection`.
- Garantizar que la información pública no requiere autenticación.

CI/CD con GitHub Actions:
- `checkout`
- `setup-dotnet` .NET 8
- `dotnet restore`
- `dotnet build`
- `dotnet test --collect:"XPlat Code Coverage"`
- `dotnet publish`
- `azure/webapps-deploy@v3`

Pruebas:
- Unitarias: xUnit + Moq para servicios de contenido público.
- Integración: xUnit + WebApplicationFactory + Testcontainers con `postgres:16-alpine` para validar datos públicos.
- E2E: Playwright para flujo de un visitante: ver misión, especialidades y pasos para agendar.
- Cobertura mínima 95% en lógica de dominio/aplicación. Excluir `obj`, `bin`, `Migrations`, `*Designer.cs`, `*g.cs`, `Program.cs`, `Startup.cs`, `appsettings*.json`.

Criterios de aceptación:
- La página pública muestra información institucional actualizada.
- Las especialidades disponibles se listan correctamente.
- Los pasos para agendar son claros y visibles sin login.
