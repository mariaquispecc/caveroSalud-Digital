# Checklist de Calidad: Administration

## 1. Requisitos funcionales del rol Administrador
- [ ] El administrador puede crear, leer, actualizar y eliminar usuarios.
- [ ] El administrador puede asignar y revocar roles a usuarios.
- [ ] El administrador puede aprobar, reprogramar o rechazar citas.
- [ ] El administrador puede gestionar horarios y disponibilidad de citas.
- [ ] El administrador puede crear, actualizar y eliminar especialidades.
- [ ] El administrador puede actualizar la información pública del portal.
- [ ] El administrador puede ver un resumen con totales de citas, usuarios y especialidades.

## 2. Seguridad de datos clínicos y personales
- [ ] Los endpoints administrativos están protegidos con autorización por rol `Administrador`.
- [ ] Ningún endpoint administrativo expone datos clínicos sensibles sin autorización adecuada.
- [ ] Los datos personales de usuarios se almacenan y transmiten de forma segura.
- [ ] Las contraseñas no se devuelven en ninguna respuesta.
- [ ] La cadena de conexión y secretos no están hardcodeados en el código.

## 3. Criterios de aceptación del módulo
- [ ] El administrador puede gestionar usuarios y sus roles exitosamente.
- [ ] Las especialidades se administran correctamente y se reflejan en el portal público.
- [ ] La información pública se actualiza y queda disponible sin autenticación.
- [ ] La operación de aprobación/reprogramación de citas actualiza el estado de la cita.
- [ ] El resumen administrativo muestra datos consistentes con la base.

## 4. Plan de pruebas y cobertura
- [ ] Existe al menos un conjunto de pruebas unitarias para la lógica de administración.
- [ ] Existe un test de integración para un endpoint administrativo con PostgreSQL real.
- [ ] Existe un test E2E que verifica login de administrador y creación/edición de un recurso.
- [ ] La cobertura mínima esperada es 95% en la lógica de aplicación/dominio.
- [ ] Se excluyen archivos de configuración y generación automática de la medición de cobertura.
