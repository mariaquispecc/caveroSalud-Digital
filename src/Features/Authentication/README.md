Authentication module

Endpoints (API):
- POST /api/v1/auth/register — registro de paciente (self-service).
- POST /api/v1/auth/login — inicio de sesión, devuelve rol y `redirect`.
- POST /api/v1/auth/admin/create-user — administración: crear usuario con rol y contraseña temporal. (Requiere rol `Administrador`).
- POST /api/v1/auth/request-password-reset — solicita envío de token de reset.
- POST /api/v1/auth/reset-password — restablece contraseña con token.
- POST /api/v1/auth/change-temporary-password — flujo para cambiar contraseña temporal.

Configuración requerida (appsettings):
- ConnectionStrings:Default
- Smtp: Host, Port, UseSsl, Username, Password, From

Sugerencia de front-end:
- El API devuelve `redirect` en `login`; el frontend debe redirigir según rol.

Notas de seguridad:
- Nunca devolver información sensible en respuestas públicas (p. ej. si un email existe).
- Tokens expiran y deben rotarse según configuración de Identity.

Próximos pasos técnicos:
- Registrar Identity en `Program.cs` con `CaveroDbContext`.
- Crear migraciones EF Core y aplicar en CI usando Testcontainers.
- Implementar pruebas unitarias e integración.
