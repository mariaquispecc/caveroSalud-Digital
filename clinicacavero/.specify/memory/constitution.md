# ClinicaCavero Constitution

## Core Principles

### I. Feature-First Modularity
Cada módulo se trata como una unidad funcional autónoma dentro de `src/Features`. Los requisitos, la lógica de negocio, la interfaz (ya sean componentes Blazor o vistas Razor Pages) y las pruebas deben estar claramente definidos por módulo.

### II. Secure-by-Default
La seguridad se aplica desde el primer diseño: autenticación basada en cookies/JWT, rol-based authorization, validación de entrada, cifrado de datos sensibles y sanidad de configuración.

### III. Test-First y Calidad Medible
Las pruebas se definen antes de la implementación siempre que sea posible. El objetivo es alcanzar una cobertura mínima del 95% en los proyectos de prueba, con exclusiones justificadas para `obj`, `bin`, migrations y archivos de configuración.

### IV. Documentación Continua
Cada característica debe tener especificación, plan, checklist y tareas asociadas en `src/Features`. Los cambios de alcance o arquitectura deben reflejarse en estos documentos.

### V. Simplicidad Operacional
Adoptar arquitectura limpia y patrones familiares: Web API + Blazor Server e integración híbrida con Razor Pages, EF Core con PostgreSQL, Azure App Service. Evitar soluciones excesivamente complejas y preferir implementaciones mantenibles.

## Constraints and Standards

- Stack: ASP.NET Core 8 Web API, Blazor Server, Razor Pages, EF Core, PostgreSQL, Azure App Service, GitHub Actions.
- No secretos en el código o en repositorio. Configuración segura mediante `appsettings.*`, variables de entorno y Azure Key Vault cuando sea necesario.
- CI/CD obligatorio: compilación, análisis, unit/integration/e2e tests, cobertura, despliegue controlado.
- Las migraciones de base de datos se versionan y revisan; no se aplican en producción sin aprobación.

## Workflow and Quality Gates

- Todo PR debe incluir descripción de alcance, pruebas realizadas y chequeo de `plan`, `checklist` y `tasks` por módulo.
- Antes de merge: pasar compilación, tests unitarios, integración y E2E relevantes para los cambios.
- Las decisiones de diseño crítico deben documentarse en las especificaciones de módulo y en `plan-overview.md`.

## Governance

La constitución rige sobre prácticas ad hoc y es el criterio de referencia para auditorías de calidad. Cualquier enmienda requiere documentación clara, versión de cambio y fecha de ratificación.

**Version**: 1.1.0 | **Ratified**: 2026-07-17 | **Last Amended**: 2026-07-17
