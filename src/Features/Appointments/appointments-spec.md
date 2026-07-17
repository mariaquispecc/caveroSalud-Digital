# Feature Specification: Gestión de Citas

**Feature Branch**: `[APPOINTMENTS-MODULE]`

**Created**: 2026-07-16

**Status**: Draft

**Input**: User description: "El módulo de citas debe permitir a pacientes reservar, cancelar y ver turnos, validando disponibilidad y gestionando el estado de las citas."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Reservar turno médico (Priority: P1)

Un paciente debe poder solicitar una cita con especialidad y doctor disponible.

**Why this priority**: Es la función básica que produce valor al paciente y activa el flujo clínico.

**Independent Test**: Crear una nueva cita validando que el horario esté libre y la especialidad exista.

**Acceptance Scenarios**:

1. **Given** un paciente autenticado, **When** selecciona un doctor o especialidad disponible y envía la solicitud, **Then** el sistema registra la cita como `PendingApproval` o `Scheduled` según la política.
2. **Given** un horario ocupado, **When** el paciente intenta reservar el mismo slot, **Then** el sistema rechaza la reserva.

---

### User Story 2 - Cancelar turno con plazo mínimo (Priority: P1)

Un paciente debe cancelar una cita con al menos 24 horas de anticipación.

**Why this priority**: Evita cancelaciones de último minuto que afectan la agenda.

**Independent Test**: Intentar cancelar una cita con menos de 24 horas y verificar que se rechaza.

**Acceptance Scenarios**:

1. **Given** una cita programada con más de 24 horas restantes, **When** el paciente la cancela, **Then** se marca como `Cancelled`.
2. **Given** una cita a menos de 24 horas, **When** el paciente intenta cancelar, **Then** recibe un error.

---

### User Story 3 - Ver citas propias (Priority: P2)

El paciente debe ver sus citas activas y su estado.

**Why this priority**: Mejora la transparencia y evita duplicados.

**Independent Test**: Consultar `my appointments` y verificar la lista filtrada por paciente.

**Acceptance Scenarios**:

1. **Given** un paciente autenticado, **When** consulta sus turnos, **Then** ve sólo sus citas en estado `Scheduled` y `PendingApproval`.

---

### Edge Cases

- Si un paciente intenta reservar una cita para la misma especialidad y doctor con solapamiento, validar la colisión.
- Si el doctor no tiene disponibilidad, el sistema debe impedir la reserva.
- Si la cita se mueve mientras el paciente está llenando el formulario, la reserva debe fallar al guardar.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-AP-001**: El sistema MUST permitir crear citas con doctor, paciente, especialidad, inicio y fin.
- **FR-AP-002**: El sistema MUST validar disponibilidad del doctor antes de asignar el turno.
- **FR-AP-003**: El sistema MUST impedir cancelaciones dentro de las 24 horas previas.
- **FR-AP-004**: El sistema MUST permitir al paciente consultar sus próximas citas.
- **FR-AP-005**: El sistema MUST marcar citas conflictivas como no elegibles.

### Key Entities

- **Appointment**: cita médica con `PatientId`, `DoctorId`, `Speciality`, `StartAt`, `EndAt`, `Status`.
- **DoctorAvailability**: bloques de horario disponibles por doctor.

## Success Criteria *(mandatory)*

- **SC-AP-001**: Un paciente puede crear una cita válida si el horario está libre.
- **SC-AP-002**: El sistema rechaza cancelaciones a menos de 24 horas.
- **SC-AP-003**: El paciente sólo ve sus propias citas.

## Assumptions

- La disponibilidad del doctor ya existe a través de `DoctorAvailability`.
- El módulo de administración controla el estado general de las especialidades.
- El estado `PendingApproval` es aceptable para las citas en revisión.
