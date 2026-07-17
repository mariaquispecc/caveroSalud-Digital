Feature: Clinical History

Objetivo:
- Registrar y consultar historias clínicas con acceso restringido por rol.
- Garantizar trazabilidades, visibilidad y protección de datos de pacientes.

Contexto técnico compartido:
- API: ASP.NET Core 8 Web API en C#.
- Persistencia: EF Core + Npgsql.
- Autenticación: Identity + JWT.
- Autorización: roles y políticas para `Paciente`, `Médico` y `Administrador`; el Administrador actúa principalmente como auditor/configurador y no accede directamente a historias clínicas ordinarias sin autorización especial.
- Frontend: Blazor Server.
- Local: Docker Compose `postgres:16-alpine`.
- Producción: PostgreSQL externo y Azure App Service.

Implementación propuesta:
- Endpoints en `src/Features/ClinicalHistory/Controllers/ClinicalHistoryController.cs` para consultar registros y crear observaciones.
- Entidades `ClinicalRecord`, `Encounter`, `Diagnosis`, `Treatment` en Domain.
- Servicios de aplicación con validación de acceso:
  - pacientes ven sólo su historia.
  - médicos ven la historia de pacientes con consulta previa.
- Repositorios EF Core en Infrastructure y filtros seguros a nivel de consulta.

Conexión y despliegue:
- Configurar cadena de conexión en `appsettings` y variables de entorno.
- En App Service, usar App Settings para `ConnectionStrings:DefaultConnection` y JWT.
- Asegurar SSL y reglas de acceso para la base de datos externa.

CI/CD:
- `checkout`
- `setup-dotnet`
- `dotnet restore`
- `dotnet build`
- `dotnet test --collect:"XPlat Code Coverage"`
- `dotnet publish`
- `azure/webapps-deploy@v3`

Pruebas:
- Unitarias: xUnit + Moq para reglas de negocio, validación de acceso y creación de registros.
- Integración: xUnit + WebApplicationFactory + Testcontainers PostgreSQL real para validar endpoints y consultas.
- E2E: Playwright para flujo médico: login, ver historia de paciente y agregar observaciones.
- Cobertura: coverlet mínimo 95% en lógica de dominio/aplicación. Excluir:
  - `**/obj/**`, `**/bin/**`, `**/Migrations/**`, `**/*Designer.cs`, `**/*g.cs`, `Program.cs`, `Startup.cs`, `appsettings*.json`.

Criterios de aceptación:
- Paciente visualiza sólo su propia historia.
- Médico accede correctamente a pacientes autorizados.
- Los datos se guardan y recuperan desde PostgreSQL real.
