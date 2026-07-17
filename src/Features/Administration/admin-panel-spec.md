# Feature Specification: Panel de Administrador

**Feature Branch**: `[ADMIN-ROLE-PANEL]`

**Created**: 2026-07-16

**Status**: Draft

**Input**: User description: "Crear el panel del rol Administrador para gestionar usuarios del sistema, administrar citas pendientes, controlar especialidades y horarios, actualizar la información pública institucional y ver un resumen general del estado del servicio."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Gestionar usuarios del sistema (Priority: P1)

Un administrador necesita crear, editar y desactivar cuentas para médicos, laboratoristas, farmacéuticos y personal administrativo.

**Why this priority**: El acceso y los permisos del personal son críticos para mantener el servicio seguro y operativo.

**Independent Test**: Crear un usuario con rol específico, editar sus datos, cambiar su rol y desactivarlo. Verificar que el usuario no puede iniciar sesión cuando está desactivado.

**Acceptance Scenarios**:

1. **Given** un administrador autenticado, **When** envía datos válidos para un nuevo usuario, **Then** el sistema crea la cuenta con el rol solicitado y asigna una contraseña temporal.
2. **Given** un usuario existente, **When** el administrador actualiza su información o rol, **Then** el sistema guarda el cambio y el usuario conserva su acceso con la nueva configuración.
3. **Given** un usuario activo, **When** el administrador lo desactiva, **Then** el usuario queda bloqueado y no puede iniciar sesión.

---

### User Story 2 - Aprobar y reprogramar citas pendientes (Priority: P1)

Un administrador debe poder aprobar o reprogramar citas que están en estado pendiente antes de que se confirmen definitivamente.

**Why this priority**: La agenda de pacientes depende de la revisión y validación de turnos pendientes.

**Independent Test**: Tomar una cita pendiente, aprobarla y reprogramarla a una nueva hora. Verificar que la cita actualiza su estado y horario.

**Acceptance Scenarios**:

1. **Given** una cita en estado `PendingApproval`, **When** el administrador la aprueba, **Then** la cita se mantiene como confirmada y lista para servicio.
2. **Given** una cita en estado `PendingApproval`, **When** el administrador reprograma la cita con hora válida, **Then** la cita actualiza su `StartAt` y `EndAt`.
3. **Given** un intento de reprogramar a un horario ocupado, **When** el administrador envía la solicitud, **Then** el sistema rechaza el cambio y solicita otra fecha/hora.

---

### User Story 3 - Administrar especialidades y horarios disponibles (Priority: P2)

Un administrador necesita crear, editar y desactivar especialidades, y asegurar que los horarios disponibles se mantengan actualizados para la agenda médica.

**Why this priority**: Las especialidades y la disponibilidad configuran el catálogo de turnos ofrecidos a pacientes.

**Independent Test**: Crear una nueva especialidad, editar su descripción y marcarla como inactiva. Verificar que deja de mostrarse en el catálogo público de especialidades.

**Acceptance Scenarios**:

1. **Given** una especialidad nueva, **When** el administrador la crea con nombre y descripción, **Then** la especialidad queda registrada como activa.
2. **Given** una especialidad existente, **When** el administrador la edita, **Then** los cambios quedan guardados.
3. **Given** una especialidad activa, **When** el administrador la desactiva, **Then** deja de aparecer en listas públicas y de reserva.

---

### User Story 4 - Mantener la información institucional del portal público (Priority: P2)

Un administrador debe actualizar la información del portal institucional para que el sitio web público muestre datos vigentes.

**Why this priority**: El portal público es la cara visible de la organización y debe reflejar datos correctos.

**Independent Test**: Cambiar el título, tagline y datos de contacto del portal; verificar que el nuevo bloque de información se guarda.

**Acceptance Scenarios**:

1. **Given** que no existe información pública registrada aún, **When** el administrador ingresa el contenido, **Then** el sistema crea el primer registro.
2. **Given** información pública vigente, **When** el administrador actualiza el contenido, **Then** el sistema guarda los nuevos datos y actualiza la fecha de modificación.
3. **Given** un contenido público desactualizado, **When** se consulta el endpoint público, **Then** debe devolver el último registro editable.

---

### User Story 5 - Ver resumen general del servicio (Priority: P3)

Un administrador debe ver un resumen operativo con citas del día, personal activo, métricas de atención y estado del inventario.

**Why this priority**: Proporciona visibilidad rápida del estado del servicio y alerta sobre problemas operativos.

**Independent Test**: Abrir el panel de resumen y verificar que muestra números de citas del día, personal activo, recetas y análisis, y cantidad de ítems con bajo stock.

**Acceptance Scenarios**:

1. **Given** el día actual, **When** el administrador abre el dashboard, **Then** el sistema muestra el total de citas agendadas para hoy.
2. **Given** varios empleados activos, **When** se consulta el resumen, **Then** el sistema muestra la cantidad de personal activo.
3. **Given** inventario con umbrales definidos, **When** se consulta el resumen, **Then** el sistema muestra los ítems con stock bajo.

---

### Edge Cases

- ¿Qué sucede si un paciente intenta cancelar una cita con menos de 24 horas de anticipación? Debe rechazarse y notificar que el plazo expiró.
- ¿Qué pasa si un administrador reprograma una cita a un horario que ya está reservado? El sistema debe rechazar la reprogramación.
- ¿Qué ocurre si existe más de un rol válido para un mismo usuario? En esta versión, cada usuario tiene un único rol principal.
- ¿Cómo se gestiona la falta de inventario? El resumen muestra ítems por debajo del umbral, pero la entrega de recetas falla si el stock es insuficiente.
- ¿Qué ocurre si no hay información pública registrada? El `GET public-info` debe devolver `404` o un estado vacío que indique que falta configurar el contenido.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema MUST permitir que el administrador cree usuarios con rol `Médico`, `Laboratorista`, `Farmacéutico` o `Administrador`.
- **FR-002**: El sistema MUST permitir editar datos de usuario, incluyendo nombre, DNI, email, teléfono, especialidad y rol.
- **FR-003**: El sistema MUST permitir desactivar y reactivar usuarios a través del bloqueo de la cuenta.
- **FR-004**: El sistema MUST permitir aprobar o reprogramar citas en estado `PendingApproval`.
- **FR-005**: El sistema MUST validar que la reprogramación no colisione con un slot ocupado.
- **FR-006**: El sistema MUST permitir crear, editar y desactivar especialidades.
- **FR-007**: El sistema MUST permitir gestionar la información institucional pública del portal en un único bloque editable.
- **FR-008**: El sistema MUST entregar un resumen general con citas del día, personal activo, pendientes, recetas, análisis e inventario de bajo stock.
- **FR-009**: El sistema MUST marcar una cita como `PendingApproval` antes de ser confirmada por admin o doctor.
- **FR-010**: El sistema MUST evitar que un paciente tenga más de una cita activa con el mismo doctor en el mismo rango horario.

### Key Entities *(include if feature involves data)*

- **User**: Representa a médicos, laboratoristas, farmacéuticos, administradores y pacientes; incluye rol, nombre, DNI, email, teléfono, especialidad y estado activo.
- **Appointment**: Representa un turno médico con estado, horario, paciente, doctor y especialidad.
- **Speciality**: Representa una especialidad médica administrable con nombre, descripción y estado activo.
- **PublicInfo**: Representa el bloque editable de información institucional pública.
- **InventoryItem**: Representa stock de medicamentos o insumos con umbral de bajo stock.
- **Prescription**: Representa recetas emitidas, estado de entrega y asociación a cita.
- **LabOrder**: Representa órdenes de laboratorio asociadas a citas y su estado de validación.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El administrador puede crear y editar usuarios con rol específico sin error.
- **SC-002**: El administrador puede aprobar o reprogramar citas pendientes, y el sistema rechaza reprogramaciones con conflicto de horario.
- **SC-003**: El administrador puede cambiar y guardar la información pública del portal institucional.
- **SC-004**: El dashboard de resumen muestra las métricas solicitadas (citas del día, personal activo, pendientes, recetas, análisis, bajo stock).
- **SC-005**: El sistema bloquea la cancelación de citas con menos de 24 horas de anticipación.

## Assumptions

- El rol `Administrador` existe y los usuarios pertenecientes a él pueden acceder a todos los endpoints administrativos.
- El sistema usa autenticación basada en Identity con un único rol primario por usuario.
- La información pública del portal se almacena en un solo registro editable; no hay múltiples secciones independientes en esta versión.
- No hay facturación real en el modelo actual, por lo que `ingresos del día` se interpreta como métricas operativas y recuento de atenciones.
- Las especialidades se administran como entidades independientes y pueden ocultarse/desactivarse sin eliminar datos históricos.
