# Feature Specification: Módulo de Laboratorio

**Feature Branch**: `[LABORATORY-WORKFLOW]`

**Created**: 2026-07-16

**Status**: Draft

**Input**: User description: "El laboratorio debe procesar órdenes médicas, registrar resultados y mantener inventario para permitir reportes y validaciones."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver órdenes de laboratorio pendientes (Priority: P1)

El laboratorista debe ver las órdenes pendientes de resultado.

**Why this priority**: Permite priorizar la ejecución del trabajo de laboratorio.

**Independent Test**: Consultar órdenes con estado `Requested` y verificar la lista.

**Acceptance Scenarios**:

1. **Given** órdenes pendientes, **When** el laboratorista consulta, **Then** ve sólo las órdenes `Requested`.

---

### User Story 2 - Registrar y validar resultados (Priority: P1)

El laboratorista debe introducir resultados y validarlos para su publicación.

**Why this priority**: Asegura que los resultados sean revisados antes de hacerse visibles.

**Independent Test**: Enviar resultados para una orden y luego validarlos.

**Acceptance Scenarios**:

1. **Given** una orden solicitada, **When** el laboratorista presenta resultados, **Then** se crea un conjunto de resultados asociados.
2. **Given** resultados ingresados, **When** valida la orden, **Then** el estado cambia a `Validated` y los resultados se publican.

---

### User Story 3 - Gestionar inventario de laboratorio (Priority: P2)

El personal de laboratorio debe ver y actualizar cantidades de inventario.

**Why this priority**: Mantiene control de insumos y evita faltantes.

**Independent Test**: Consultar inventario y actualizar una cantidad.

**Acceptance Scenarios**:

1. **Given** un inventario existente, **When** consulta, **Then** ve los ítems y sus cantidades.
2. **Given** un ítem existente, **When** actualiza cantidad y umbral, **Then** los cambios quedan guardados.

---

### Edge Cases

- Si una orden no tiene resultados, no se debe poder validar.
- Si el inventario cae por debajo del umbral, debe identificarse como `low stock`.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-LAB-001**: El sistema MUST mostrar órdenes de laboratorio pendientes.
- **FR-LAB-002**: El sistema MUST permitir registrar resultados por orden.
- **FR-LAB-003**: El sistema MUST permitir validar resultados antes de publicarlos.
- **FR-LAB-004**: El sistema MUST manejar inventario con cantidad y umbral de alerta.
- **FR-LAB-005**: El sistema MUST rechazar validaciones cuando no hay resultados.

### Key Entities

- **LabOrder**: orden solicitada asociada a una cita y paciente.
- **LabResult**: resultado de laboratorio asociado a una orden.
- **InventoryItem**: insumo con cantidad y umbral.

## Success Criteria *(mandatory)*

- **SC-LAB-001**: El laboratorista ve órdenes `Requested`.
- **SC-LAB-002**: El laboratorista puede registrar resultados y validarlos.
- **SC-LAB-003**: El inventario muestra items y detecta bajo stock.

## Assumptions

- Las órdenes se crean sólo desde la atención médica.
- La validación de resultados es un paso obligatorio antes de la publicación.
