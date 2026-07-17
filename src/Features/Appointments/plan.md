Feature: Appointments

Objetivo:
- Gestionar reservas de citas entre pacientes y médicos.
- Validar disponibilidad, permitir cancelaciones y mostrar estados al paciente.

Contexto técnico compartido:
- API: ASP.NET Core 8 Web API en C#.
- Persistencia: EF Core con `Npgsql.EntityFrameworkCore.PostgreSQL`.
- Autenticación: ASP.NET Core Identity + JWT.
- Roles: `Paciente`, `Médico`, `Administrador`, `Laboratorista`, `Farmacéutico`.
- Frontend: Blazor Server.
- Desarrollo local: PostgreSQL con Docker Compose `postgres:16-alpine`.
- Producción: base de datos externalizada y Azure App Service.
- Cadena de conexión por configuración/env vars.

Implementación propuesta:
- Endpoints REST en `src/Features/Appointments/Controllers/AppointmentController.cs` bajo `/api/v1/appointments`.
- Servicios de aplicación en capa `Application` que validan disponibilidad y reglas de cancelación.
- Disponibilidad de médicos validada contra una fuente de horario estructurada (`DoctorAvailability` o bloques de horarios); la reserva debe impedir solapamientos.
- Entidad `Appointment` en `Domain` con `PatientId`, `DoctorId`, `SpecialityId`, `StartAt`, `EndAt`, `Status`.
- Repositorios EF Core en `Infrastructure`, integrados con `CaveroDbContext`.
- Blazor Server: páginas para paciente con creación y consulta de citas, y para médico con gestión de su agenda.

Conexión a datos y despliegue:
- Usar `docker-compose.yml` local con servicio `db` y variables de entorno `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`.
- En producción en Azure App Service, configurar `ConnectionStrings:DefaultConnection` con la cadena de conexión del proveedor externo.
- Configuración de app:
  - `builder.Configuration.AddJsonFile("appsettings.json")`
  - `builder.Configuration.AddEnvironmentVariables()`
  - `builder.Services.AddDbContext<CaveroDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")))`
- En App Service, agregar App Settings para JWT y la cadena de conexión.

CI/CD con GitHub Actions:
- `checkout`
- `setup-dotnet` .NET 8
- `dotnet restore`
- `dotnet build`
- `dotnet test --collect:"XPlat Code Coverage"`
- `dotnet publish` y `azure/webapps-deploy@v3`
- Secrets: `AZURE_WEBAPP_PUBLISH_PROFILE`, `AZURE_WEBAPP_NAME`, `AZURE_CREDENTIALS`.

Pruebas:
- Unitarias: xUnit + Moq para validaciones de disponibilidad, reglas de cancelación y cálculo de horas.
- Integración: xUnit + WebApplicationFactory + `DotNet.Testcontainers` con `postgres:16-alpine`; tests de endpoints y acceso a datos.
- E2E: Playwright para flujo paciente principal: login, reservar cita, ver confirmación, cancelar cita.
- Cobertura: coverlet con 95% mínimo en lógica de dominio y aplicación. Exclusiones:
  - `**/obj/**`, `**/bin/**`, `**/Migrations/**`, `**/*Designer.cs`, `**/*g.cs`, `Program.cs`, `Startup.cs`, archivos de configuración.

Criterios de aceptación:
- Pacientes pueden reservar citas sólo cuando el doctor está disponible.
- Las cancelaciones dentro de las 24 horas son rechazadas.
- Los endpoints responden con datos correctos desde PostgreSQL real en integración.
- El pipeline CI ejecuta tests y despliega correctamente a Azure.
