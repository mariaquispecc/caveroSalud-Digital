# Plan General de Implementación y Pruebas

## Visión general

Este plan define la implementación del proyecto en ASP.NET Core 8 Web API con EF Core y PostgreSQL, autenticación con ASP.NET Core Identity + JWT, autorización por roles y frontend en Blazor Server. La configuración local usa Docker Compose con `postgres:16-alpine`, y la producción despliega a Azure App Service con una base de datos PostgreSQL externa (Neon o Supabase).

## Arquitectura

- API: ASP.NET Core 8 Web API en C#.
- Persistencia: Entity Framework Core con `Npgsql.EntityFrameworkCore.PostgreSQL`.
- Identidad: ASP.NET Core Identity con `ApplicationUser : IdentityUser<Guid>`.
- Autorización: JWT + políticas de roles.
- Frontend: Blazor Server.
- Base de datos local: Docker Compose con `postgres:16-alpine`.
- Base de datos de producción: proveedor externo gratuito (Neon o Supabase).
- Despliegue: Azure App Service (tier gratuito F1 o Basic B1).

## Roles del sistema

- Administrador
- Médico
- Paciente
- Laboratorista
- Farmacéutico

## Configuración de la cadena de conexión

- La conexión siempre se lee desde la configuración:
  - `appsettings.json`/`appsettings.Development.json` para valores de ejemplo.
  - Variables de entorno y App Settings en Azure para producción.
- Nunca se debe hardcodear la cadena en código fuente.
- Ejemplo en `Program.cs`:

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<CaveroDbContext>(options =>
    options.UseNpgsql(connectionString));
```

- Variables clave:
  - `ConnectionStrings:DefaultConnection`
  - `JWT__Issuer`
  - `JWT__Audience`
  - `JWT__Secret`
  - `ASPNETCORE_ENVIRONMENT`

## Conexión de Azure App Service a la base de datos externa

- Configurar en Azure App Service `Configuration > Application settings`:
  - `ConnectionStrings:DefaultConnection` con la cadena completa de PostgreSQL.
  - `JWT__Issuer`, `JWT__Audience`, `JWT__Secret`.
- La base de datos externa debe aceptar conexiones desde Azure.
- En Neon o Supabase, generar la cadena con usuario, contraseña, host, puerto, base de datos y parámetros SSL.
- Si el proveedor requiere SSL, usar `Ssl Mode=Require;Trust Server Certificate=true`.
- No incluir secretos en el repositorio Git.

## Pipeline CI/CD con GitHub Actions

### Flujo básico

1. `checkout`
2. `setup-dotnet` para .NET 8
3. `dotnet restore`
4. `dotnet build --configuration Release`
5. `dotnet test --configuration Release --collect:"XPlat Code Coverage"`
6. `dotnet publish src/CaveroSalud.Api/CaveroSalud.Api.csproj --configuration Release --output ./publish`
7. `azure/webapps-deploy@v3` para desplegar a Azure App Service

### Secrets y configuración requerida

- `AZURE_WEBAPP_PUBLISH_PROFILE`
- `AZURE_APP_SERVICE_NAME`
- `AZURE_RESOURCE_GROUP`
- `JWT__Secret`
- `SMTP__User`, `SMTP__Password` (si aplica)
- Variables de conexión a la base de datos si se prueba contra un entorno real o staging.

## Estrategia de pruebas

### Pruebas unitarias

- Framework: xUnit.
- Mocking: Moq.
- Objetivo: lógica de aplicación y dominio.
- Áreas clave:
  - Validaciones de negocio.
  - Reglas de acceso por rol.
  - Generación de JWT.
  - Cálculos de disponibilidad, cancelación y stock.

### Pruebas de integración

- Framework: xUnit + `WebApplicationFactory<TEntryPoint>`.
- Base de datos: Testcontainers con contenedor PostgreSQL real (`postgres:16-alpine`).
- Alcance:
  - Endpoints HTTP de la API.
  - Acceso a datos a través de EF Core.
  - Migraciones reales y esquema idéntico a producción.

### Pruebas end-to-end (E2E)

- Herramienta: Playwright.
- Flujo principal por rol:
  - Administrador: login, crear usuarios, asignar roles, gestionar contenido público.
  - Paciente: login, reservar cita, ver citas.
  - Médico: login, ver agenda, registrar atención.
  - Laboratorista: login, procesar orden de laboratorio.
  - Farmacéutico: login, dispensar receta.

### Cobertura de código

- Herramienta: coverlet.
- Meta mínima: 95% de cobertura sobre la lógica de dominio y aplicación.
- Exclusiones:
  - `**/obj/**`
  - `**/bin/**`
  - `**/Migrations/**`
  - `**/*Designer.cs`
  - `**/*g.cs`
  - `Program.cs`
  - `Startup.cs`
  - `appsettings*.json`
  - archivos de prueba propios si se configuran en el mismo proyecto.

## Estructura de carpetas recomendada

- `src/Features/<Modulo>/Controllers`
- `src/Features/<Modulo>/DTOs`
- `src/Features/<Modulo>/Services`
- `src/Features/<Modulo>/Tests`
- `src/CaveroSalud.Application`
- `src/CaveroSalud.Infrastructure`
- `src/CaveroSalud.Domain`
- `tests/Integration`
- `tests/EndToEnd`

## Notas finales

- Mantener consistencia de tecnologías entre módulos.
- Documentar cualquier cambio de entorno en `README.md` y en los archivos `plan.md` por módulo.
- Priorizar seguridad y configuración sin secretos en el código.

## Ajustes recientes aplicados

- `Administration`: gestión de horarios/disponibilidad añadida al plan y a las pruebas.
- `Appointments`: clarificada la validación de disponibilidad con bloques de horario y se agregaron pruebas específicas.
- `ClinicalHistory`: aclarado el rol del Administrador como auditor/configurador y se añadió validación de denegación de acceso.
- `Doctors`: añadido el requisito de bloquear edición de registros clínicos cerrados y pruebas asociadas.
- `Public`: reforzada la sincronización admin → contenido público con pruebas de integración y E2E.
