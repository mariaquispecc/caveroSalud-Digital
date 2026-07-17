# Tasks de Pruebas: Administration

## Pruebas unitarias
- [ ] Crear tests para la lógica de creación y actualización de usuarios.
- [ ] Crear tests para la asignación y revocación de roles.
- [ ] Crear tests para la lógica de horarios y disponibilidad administrada.
- [ ] Crear tests para la lógica de aprobación y reprogramación de citas.
- [ ] Crear tests para la creación y edición de especialidades.
- [ ] Crear tests para el manejo de contenido público en `PublicInfo`.

## Pruebas de integración
- [ ] Crear un test de integración para el endpoint de creación de usuarios usando `WebApplicationFactory` y PostgreSQL con Testcontainers.
- [ ] Crear un test de integración para el endpoint de asignación de roles.
- [ ] Crear un test de integración para el endpoint de aprobación/reprogramación de citas.
- [ ] Crear un test de integración para el endpoint de administración de especialidades.
- [ ] Crear un test de integración para el endpoint de actualización de información pública.

## Pruebas end-to-end
- [ ] Crear un flujo E2E donde un administrador hace login, crea un usuario, asigna un rol y verifica el acceso.
- [ ] Crear un flujo E2E donde un administrador crea una especialidad y verifica que aparece en el portal público.
- [ ] Crear un flujo E2E donde un administrador aprueba o reprograma una cita.

## Cobertura final
- [ ] Ejecutar el reporte de cobertura Coverlet para el módulo Administration.
- [ ] Identificar casos faltantes y añadir tests hasta alcanzar la cobertura mínima del 95%.
