Feature: Doctors

Objetivo:
- Ofrecer al médico un panel de control para ver su agenda, atender consultas y documentar atención.
- Facilitar que el médico registre diagnósticos, órdenes de laboratorio y recetas.

Contexto técnico compartido:
- API: ASP.NET Core 8 Web API en C#.
- Persistencia: EF Core + Npgsql.
- Autenticación: Identity + JWT.
- Autorización: roles con enfoque en `Médico`.
- Frontend: Blazor Server.
- Desarrollo local: PostgreSQL con Docker Compose `postgres:16-alpine`.
- Producción: Azure App Service con DB externa (Neon/Supabase).
- Configuración de conexión y secretos desde variables de entorno.

Implementación propuesta:
- `DoctorsController` en `src/Features/Doctors/Controllers` con endpoints para agenda, finalización de consultas, creación de órdenes y recetas.
- Servicios en `Application` para encapsular la lógica de atención médica.
- Entidades `Appointment`, `ClinicalRecord`, `LabOrder`, `Prescription` en `Domain`.
- Páginas Blazor para que el médico vea su agenda diaria/semanal, atienda una consulta y gestione órdenes/recetas.
- Una vez cerrado un registro clínico, el sistema debe bloquear su edición posterior.

Conexión y despliegue:
- Usar `DefaultConnection` desde `Configuration.GetConnectionString("DefaultConnection")`.
- En Azure App Service, definir App Settings para `ConnectionStrings:DefaultConnection`, `JWT__Secret`, `JWT__Issuer`, `JWT__Audience`.
- Asegurar que la base de datos externa acepte conexiones desde Azure.

CI/CD con GitHub Actions:
- `checkout`
- `setup-dotnet` .NET 8
- `dotnet restore`
- `dotnet build`
- `dotnet test --collect:"XPlat Code Coverage"`
- `dotnet publish`
- `azure/webapps-deploy@v3`

Pruebas:
- Unitarias: xUnit + Moq para reglas de negocio del médico.
- Integración: xUnit + WebApplicationFactory + Testcontainers con `postgres:16-alpine`.
- E2E: Playwright para flujo médico: login, ver agenda y registrar atención.
- Cobertura mínima 95% en lógica de dominio/aplicación. Excluir `obj`, `bin`, `Migrations`, `*Designer.cs`, `*g.cs`, `Program.cs`, `Startup.cs`, `appsettings*.json`.

Criterios de aceptación:
- El médico ve solo su agenda y no la de otros médicos.
- El médico puede registrar la atención y los resultados asociados.
- El flujo de órdenes y recetas funciona con persistencia en PostgreSQL real.
