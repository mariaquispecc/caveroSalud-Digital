CaveroSalud Digital — Estructura de carpetas

Capas:
- CaveroSalud.Api: proyecto ASP.NET Core Web API (exposición de endpoints).
- CaveroSalud.Application: casos de uso y servicios de aplicación.
- CaveroSalud.Domain: entidades, value objects y lógica de dominio.
- CaveroSalud.Infrastructure: EF Core, repositorios, migraciones y adaptadores.

Features:
- src/Features/* contiene las features independientes: Appointments, ClinicalHistory, Laboratory, Pharmacy, Administration.

Tests:
- tests/Unit: pruebas unitarias con xUnit y Moq/NSubstitute.
- tests/Integration: pruebas de integración con WebApplicationFactory y Testcontainers (PostgreSQL).
- tests/E2E: pruebas end-to-end con Playwright.

Siguiente paso: ejecutar `tools\scaffold.ps1` para crear la solution y proyectos .NET automáticamente (requiere .NET SDK).
