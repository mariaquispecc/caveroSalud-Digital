# Feature Specification: Autenticación y Seguridad

**Feature Branch**: `[AUTHENTICATION]`

**Created**: 2026-07-16

**Status**: Draft

**Input**: User description: "El módulo de autenticación debe permitir a los usuarios registrarse, iniciar sesión, recuperar contraseña y mantener roles para control de acceso."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Registro de paciente (Priority: P1)

Un usuario debe poder registrarse como paciente con datos básicos.

**Why this priority**: Es la puerta de entrada al sistema y habilita el acceso del paciente.

**Independent Test**: Crear un nuevo usuario paciente y verificar que aparece en Identity con el rol `Paciente`.

**Acceptance Scenarios**:

1. **Given** un formulario válido, **When** el usuario envía datos de registro, **Then** se crea la cuenta y se asigna el rol `Paciente`.
2. **Given** un email ya registrado, **When** intenta registrarse, **Then** el sistema rechaza la solicitud.

---

### User Story 2 - Inicio de sesión con roles (Priority: P1)

El usuario debe iniciar sesión y obtener acceso según su rol.

**Why this priority**: Controla el acceso a paneles y recursos por perfil.

**Independent Test**: Iniciar sesión con un usuario de rol `Administrador` y `Paciente` y verificar claims/roles.

**Acceptance Scenarios**:

1. **Given** credenciales válidas, **When** el usuario inicia sesión, **Then** recibe un token o sesión autenticada.
2. **Given** contraseña incorrecta, **When** intenta iniciar sesión, **Then** se devuelve un error de autenticación.

---

### User Story 3 - Recuperación de contraseña (Priority: P2)

El usuario debe recuperar su contraseña mediante su email.

**Why this priority**: Permite restablecer acceso sin intervención manual.

**Independent Test**: Solicitar un restablecimiento y verificar la generación de un token.

**Acceptance Scenarios**:

1. **Given** un email registrado, **When** pide recuperación, **Then** se envía un token al correo.
2. **Given** un email no registrado, **When** pide recuperación, **Then** el sistema no revela que no existe la cuenta.

---

### Edge Cases

- Si el usuario intenta iniciar sesión con un rol deshabilitado, el acceso debe negarse.
- Si la contraseña es demasiado débil, el registro debe rechazarse.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-AU-001**: El sistema MUST permitir el registro de nuevos pacientes.
- **FR-AU-002**: El sistema MUST permitir inicio de sesión con credenciales válidas.
- **FR-AU-003**: El sistema MUST soportar recuperación de contraseña mediante email.
- **FR-AU-004**: El sistema MUST aplicar roles `Administrador`, `Doctor`, `Paciente`,`Farmaceutico`y`Laboratorista`.
- **FR-AU-005**: El sistema MUST proteger rutas sensibles con autorización basada en roles.

### Key Entities

- **ApplicationUser**: datos de identidad y rol.
- **IdentityRole**: rol asignado al usuario.

## Success Criteria *(mandatory)*

- **SC-AU-001**: Un paciente se registra y obtiene el rol `Paciente`.
- **SC-AU-002**: El inicio de sesión funciona con credenciales válidas.
- **SC-AU-003**: El flujo de recuperación de contraseña genera un token sin exponer si el email existe.

## Assumptions

- El proyecto ya usa ASP.NET Core Identity.
- El portal público y los paneles internos respetan los roles guardados.
