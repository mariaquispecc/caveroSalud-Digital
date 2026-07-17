# Feature Specification: Panel de Médico

**Feature Branch**: `[DOCTORS-DASHBOARD]`

**Created**: 2026-07-16

**Status**: Draft

**Input**: User description: "El panel del médico debe mostrar sus citas, permitir registrar atención, crear órdenes de laboratorio y generar recetas."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver agenda del médico (Priority: P1)

El médico debe ver sus citas de hoy y de la semana.

**Why this priority**: Permite la planificación diaria y la organización de consultas.

**Independent Test**: Consultar el dashboard y verificar la lista de citas filtrada por doctor.

**Acceptance Scenarios**:

1. **Given** un médico autenticado, **When** consulta su dashboard, **Then** ve las citas del día y de la semana.
2. **Given** citas agendadas, **When** actualiza la vista, **Then** las citas se ordenan cronológicamente.

---

### User Story 2 - Registrar atención médica (Priority: P1)

El médico debe poder atender una cita y registrar el historial clínico.

**Why this priority**: Es el núcleo de la atención clínica.

**Independent Test**: Atender una cita y verificar la creación de un registro clínico y la finalización de la cita.

**Acceptance Scenarios**:

1. **Given** una cita válida, **When** el médico registra diagnóstico y tratamiento, **Then** se crea un registro clínico y la cita pasa a `Completed`.
2. **Given** una atención con órdenes de laboratorio, **When** el médico las agrega, **Then** se crean órdenes con estado `Requested`.
3. **Given** una atención con recetas, **When** el médico las agrega, **Then** se crean recetas asociadas a la cita.

---

### Edge Cases

- Si la cita ya fue completada, no se debe permitir atendida duplicada.
- Si el paciente no corresponde al doctor, el sistema debe denegar el acceso.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-DO-001**: El sistema MUST mostrar la agenda diaria y semanal del médico.
- **FR-DO-002**: El sistema MUST permitir crear registros clínicos asociados a una cita.
- **FR-DO-003**: El sistema MUST permitir crear órdenes de laboratorio durante la atención.
- **FR-DO-004**: El sistema MUST permitir generar recetas con múltiples ítems.
- **FR-DO-005**: El sistema MUST marcar la cita como `Completed` al finalizar la atención.

### Key Entities

- **ClinicalRecord**: historia médica asociada a una cita.
- **LabOrder**: orden de laboratorio asociada a cita y doctor.
- **Prescription**: recetas emitidas por un médico.

## Success Criteria *(mandatory)*

- **SC-DO-001**: El médico puede ver su agenda correctamente.
- **SC-DO-002**: El médico puede crear un registro clínico y finalizar la cita.
- **SC-DO-003**: El médico puede emitir órdenes de laboratorio y recetas.

## Assumptions

- El médico sólo puede atender citas asignadas a él.
- La información del paciente se obtiene de la cita y del historial.
