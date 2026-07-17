# Checklist de Calidad: Authentication

## 1. Requisitos funcionales del rol correspondiente
- [ ] El paciente puede registrarse con datos obligatorios.
- [ ] Los usuarios de roles administrativos pueden crear cuentas para médicos, laboratoristas y farmacéuticos.
- [ ] El login devuelve tokens JWT con claims de rol.
- [ ] El flujo de primer ingreso forzado funciona para contraseñas temporales.
- [ ] El usuario puede solicitar y ejecutar la recuperación de contraseña.

## 2. Seguridad de datos clínicos y personales
- [ ] Las contraseñas nunca se devuelven ni se almacenan en texto plano.
- [ ] La generación de tokens JWT usa secretos configurables y seguros.
- [ ] La recuperación de contraseña no revela si un email existe.
- [ ] Los datos personales se gestionan solo con endpoints autenticados.
- [ ] La cadena de conexión y valores secretos se extraen de App Settings/variables de entorno.

## 3. Criterios de aceptación del módulo
- [ ] Registro de paciente crea un usuario con rol `Paciente`.
- [ ] Login devuelve token válido y roles.
- [ ] El administrador puede crear usuarios con roles y contraseñas temporales.
- [ ] El cambio de contraseña temporal desactiva el flag de primer ingreso.
- [ ] La recuperación de contraseña genera un token válido y permite restablecer la contraseña.

## 4. Plan de pruebas y cobertura
- [ ] Existen pruebas unitarias para validación de registro, login y recuperación.
- [ ] Existen pruebas de integración de los endpoints de autenticación con PostgreSQL real.
- [ ] Existe un test E2E para el flujo de registro/login/primer ingreso.
- [ ] La cobertura mínima es 95% en lógica de dominio y aplicación.
- [ ] Se excluyen los archivos de configuración y producción de la medición de cobertura.
