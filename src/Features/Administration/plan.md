Feature: Administration

Objetivo:
- Gestionar usuarios, roles y políticas de acceso.
- Administrar especialidades, horarios, información pública y estados de citas.
- Gestionar bloques de disponibilidad y horarios de médicos como referencia para la programación de citas.

Contexto técnico compartido:
- API: ASP.NET Core 8 Web API en C#.
- Persistencia: EF Core con `Npgsql.EntityFrameworkCore.PostgreSQL`.
- Autenticación: ASP.NET Core Identity con `ApplicationUser : IdentityUser<Guid>`.
- Autorización: JWT con roles `Paciente`, `Médico`, `Laboratorista`, `Farmacéutico`, `Administrador`.
- Frontend: Blazor Server, consistente con el resto del sistema.
- Desarrollo local: PostgreSQL en Docker Compose usando `postgres:16-alpine`.
- Producción: PostgreSQL externo (Neon o Supabase) y Azure App Service para API y frontend.
- Cadena de conexión leída desde configuración/variables de entorno, nunca hardcodeada.

Implementación propuesta:
- `AdministrationController` en `src/Features/Administration/Controllers` con endpoints REST para CRUD de usuarios, aprobación/reprogramación de citas, CRUD de especialidades y contenido público.
- Servicios y casos de uso en la capa `Application` para separar lógica de dominio y evitar acoplamientos.
- `CaveroDbContext` en `Infrastructure` con DbSet para `Speciality`, `PublicInfo`, `Appointment`, `ApplicationUser` y demás entidades.
- Configuración de JWT y Identity en `Program.cs`/`Startup.cs` con `AddIdentityCore`, `AddRoles<IdentityRole<Guid>>()`, `AddAuthentication().AddJwtBearer(...)`.
- Blazor Server: páginas protegidas bajo `/admin`, resueltas por rol `Administrador`.

Conexión a datos y despliegue:
- Desarrollo local usa `docker-compose.yml` con servicio `db` basado en `postgres:16-alpine`.
- En `appsettings.Development.json` se puede usar un `DefaultConnection` con la URL de Docker, pero la aplicación siempre resuelve la cadena vía `Configuration.GetConnectionString("DefaultConnection")` y variables de entorno.
- Para Azure App Service, agregar App Settings:
  - `ConnectionStrings:DefaultConnection` = `<cadena postgres SSL/TrustServerCertificate=True>`
  - `JWT__Issuer`, `JWT__Audience`, `JWT__Secret`
  - `ASPNETCORE_ENVIRONMENT=Production`
- La base de datos externa debe permitir el acceso desde Azure App Service (IP pública o túnel seguro) y usar SSL según el proveedor.

Pipeline CI/CD simple con GitHub Actions:
- `checkout`
- `setup-dotnet` para .NET 8
- `dotnet restore`
- `dotnet build --configuration Release`
- `dotnet test --configuration Release --collect:"XPlat Code Coverage"`
- `dotnet publish src/CaveroSalud.Api/CaveroSalud.Api.csproj --configuration Release --output ./publish`
- `azure/webapps-deploy@v3` para desplegar a App Service de la API
- Si el frontend Blazor Server está empaquetado junto a la API, desplegar el mismo artefacto; si está separado, agregar segundo despliegue.
- Secrets requeridos: `AZURE_WEBAPP_PUBLISH_PROFILE`, `AZURE_APP_SERVICE_NAME`, `AZURE_RESOURCE_GROUP` o `AZURE_CREDENTIALS`.

Estrategia de pruebas:
- Unitarias (xUnit + Moq): validar servicios de aplicación, reglas de negocio y validaciones de roles.
- Integración (xUnit + WebApplicationFactory + Testcontainers): levantar una instancia real de PostgreSQL `postgres:16-alpine`, ejecutar migraciones y probar endpoints del controlador.
- End-to-end (Playwright): flujo principal del administrador, por ejemplo: login, crear usuario, asignar rol, crear especialidad y actualizar información pública.
- Cobertura: coverlet con mínimo 95% sobre la lógica de dominio y aplicación. Excluir:
  - `**/obj/**`
  - `**/bin/**`
  - `**/Migrations/**`
  - `**/*Designer.cs`
  - `**/*g.cs`
  - `Program.cs`, `Startup.cs`, `appsettings*.json`
  - pruebas mismas (`**/*Tests.cs` si se usa para test only)

Criterios de aceptación del módulo Administration:
- El administrador puede crear, actualizar, inhabilitar y eliminar usuarios con roles.
- El administrador puede crear especialidades y editar información pública.
- La aplicación usa configuración de conexión segura desde variables de entorno.
- Los tests Unit, Integration y E2E están definidos y se ejecutan en el pipeline.
