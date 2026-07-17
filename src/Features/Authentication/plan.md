Feature: Authentication & Authorization

Objetivo:
- Proveer registro, inicio de sesión, gestión de usuarios, recuperación de contraseña y primer ingreso obligatorio.
- Soportar roles `Paciente`, `Médico`, `Laboratorista`, `Farmacéutico`, `Administrador`.

Contexto técnico compartido:
- API: ASP.NET Core 8 Web API en C#.
- Persistencia: EF Core + `Npgsql.EntityFrameworkCore.PostgreSQL`.
- Identidad: ASP.NET Core Identity con `ApplicationUser : IdentityUser<Guid>`.
- Autorización: JWT con roles y políticas de acceso por rol.
- Frontend: Blazor Server.
- Local: PostgreSQL en Docker Compose `postgres:16-alpine`.
- Producción: PostgreSQL externo y Azure App Service.
- Configuración segura: env vars / App Settings.

Implementación propuesta:
- `AuthController` en `src/Features/Authentication/Controllers` para `register`, `login`, `refresh`, `request-password-reset`, `reset-password` y `change-temporary-password`.
- `UserManager<ApplicationUser>`, `RoleManager<IdentityRole<Guid>>` y servicios de JWT en `Application`.
- `ApplicationUser` con campos extendidos como `FullName`, `Dni`, `Phone`, `IsTemporaryPassword`.
- `IEmailSender` + `SmtpEmailSender` configurable para envíos de tokens de contraseña.
- Blazor Server con páginas de registro, login y cambio obligatorio de contraseña.

Conexión y despliegue:
- Desarrollo local usa `docker-compose.yml` y `DefaultConnection` en la configuración.
- En Azure App Service, definir:
  - `ConnectionStrings:DefaultConnection`
  - `JWT__Issuer`, `JWT__Audience`, `JWT__Secret`
  - `ASPNETCORE_ENVIRONMENT=Production`
  - SMTP settings si se usa correo real.
- La cadena de conexión no se codifica en código fuente.
- Para Neon/Supabase, la App Service debe usar la cadena provista y, si es necesario, configurar SSL `TrustServerCertificate=true`.

Pipeline CI/CD:
- `checkout`
- `setup-dotnet` 8.x
- `dotnet restore`
- `dotnet build`
- `dotnet test --collect:"XPlat Code Coverage"`
- `dotnet publish src/CaveroSalud.Api/CaveroSalud.Api.csproj`
- `azure/webapps-deploy@v3`.
- Secrets: `AZURE_WEBAPP_PUBLISH_PROFILE`, `JWT__Secret`, `SMTP__User`, `SMTP__Password`, `AZURE_CREDENTIALS`.

Pruebas:
- Unitarias: xUnit + Moq para validación de registro, creación de roles, generación de JWT y flujos de contraseña.
- Integración: xUnit + WebApplicationFactory + Testcontainers con `postgres:16-alpine` para validar endpoints y la persistencia de Identity.
- E2E: Playwright para flujo principal de autenticación: registro de paciente, login, primer ingreso, cambio de contraseña.
- Cobertura: coverlet con objetivo mínimo 95% en lógica de dominio/aplicación. Excluir:
  - `**/obj/**`, `**/bin/**`, `**/Migrations/**`, `**/*Designer.cs`, `**/*g.cs`, `Program.cs`, `Startup.cs`, `appsettings*.json`.

Criterios de aceptación:
- Registro de paciente y login funcionan con JWT.
- Administrador puede crear usuarios de roles específicos con contraseñas temporales.
- Flujos de recuperación de contraseña funcionan y no exponen si el email existe.
- La aplicación se configura mediante App Service settings en producción.
