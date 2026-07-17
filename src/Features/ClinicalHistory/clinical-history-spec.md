# Feature Specification: Historia Clínica

**Feature Branch**: `[CLINICAL-HISTORY]`

**Created**: 2026-07-16

**Status**: Draft

**Input**: User description: "El módulo de historia clínica debe permitir a pacientes y médicos revisar los registros de consulta, observaciones, recetas y resultados de laboratorio."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Paciente revisa su historial (Priority: P1)

Un paciente debe poder ver su historial clínico completo con registros recientes.

**Why this priority**: Permite al paciente revisar su propia atención y seguimiento.

**Independent Test**: Consultar `my-history` y verificar que devuelve solo registros del paciente.

**Acceptance Scenarios**:

1. **Given** un paciente autenticado, **When** consulta su historia, **Then** recibe registros ordenados por fecha.
2. **Given** varios registros, **When** se lista el historial, **Then** incluye diagnósticos, tratamientos, recetas y órdenes.

---

### User Story 2 - Médico revisa historia de paciente (Priority: P2)

Un médico debe poder ver la historia de un paciente con quien ya tuvo contacto.

**Why this priority**: Mejora la continuidad de la atención entre consultas.

**Independent Test**: Consultar la historia de un paciente con citas previas del médico.

**Acceptance Scenarios**:

1. **Given** un médico con contacto previo, **When** solicita la historia del paciente, **Then** obtiene los registros.
2. **Given** un médico sin contacto previo, **When** intenta ver la historia, **Then** recibe `Forbid`.

---

### User Story 3 - Agregar observaciones y cerrar registro (Priority: P3)

El médico puede añadir observaciones y cerrar un registro clínico.

**Why this priority**: Permite documentar evolución y cerrar consultas.

**Independent Test**: Añadir observaciones a un registro abierto y luego cerrarlo.

**Acceptance Scenarios**:

1. **Given** un registro abierto, **When** el médico agrega observaciones, **Then** se guarda el texto.
2. **Given** un registro cerrado, **When** intenta editarlo, **Then** recibe un error.

---

### Edge Cases

- Si un registro ya está cerrado, no debe aceptarse edición.
- Si no existen recetas u órdenes asociadas, el historial todavía debe mostrarse.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-CH-001**: El sistema MUST permitir pacientes ver su propia historia clínica.
- **FR-CH-002**: El sistema MUST permitir médicos ver historias de pacientes con contacto previo.
- **FR-CH-003**: El sistema MUST mostrar recetas y órdenes de laboratorio asociadas a cada registro.
- **FR-CH-004**: El sistema MUST permitir añadir observaciones a registros abiertos.
- **FR-CH-005**: El sistema MUST permitir cerrar registros para evitar ediciones posteriores.

### Key Entities

- **ClinicalRecord**: registro médico con diagnóstico, tratamiento, observaciones y estado cerrado.
- **Prescription**: recetas relacionadas a la misma cita del registro.
- **LabOrder**: órdenes de laboratorio relacionadas a la cita del registro.

## Success Criteria *(mandatory)*

- **SC-CH-001**: El paciente ve solo su historial.
- **SC-CH-002**: El médico ve la historia solo si tiene contacto previo.
- **SC-CH-003**: Las observaciones se guardan y los registros cerrados no se editan.

## Assumptions

- Un paciente puede tener múltiples registros de consultas.
- Los registros de consultas se basan en citas completadas.
