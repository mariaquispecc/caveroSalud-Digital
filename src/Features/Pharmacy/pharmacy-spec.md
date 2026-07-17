# Feature Specification: Módulo de Farmacia

**Feature Branch**: `[PHARMACY-WORKFLOW]`

**Created**: 2026-07-16

**Status**: Draft

**Input**: User description: "El farmacéutico debe ver recetas pendientes, administrar inventario y registrar la entrega de medicamentos."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver recetas pendientes (Priority: P1)

El farmacéutico debe listar recetas que están esperando dispensación.

**Why this priority**: Permite gestionar eficientemente la entrega de medicamentos.

**Independent Test**: Consultar recetas con estado `Requested` y verificar que aparecen sólo las pendientes.

**Acceptance Scenarios**:

1. **Given** recetas pendientes, **When** el farmacéutico consulta, **Then** ve la lista de recetas solicitadas.

---

### User Story 2 - Entregar receta y descontar inventario (Priority: P1)

El farmacéutico debe marcar una receta como entregada y descontar el stock.

**Why this priority**: Controla la disponibilidad de medicamentos y la trazabilidad de entregas.

**Independent Test**: Entregar una receta y verificar la reducción de inventario.

**Acceptance Scenarios**:

1. **Given** una receta con stock suficiente, **When** se entrega, **Then** el estado cambia a `Delivered` y el stock se descuenta.
2. **Given** stock insuficiente para algún medicamento, **When** se intenta entregar, **Then** la entrega falla y se notifica la falta.

---

### User Story 3 - Consultar inventario y bajo stock (Priority: P2)

El farmacéutico debe ver el inventario y distinguir ítems con stock bajo.

**Why this priority**: Facilita la reposición y la planificación de compras.

**Independent Test**: Consultar inventario y verificar los ítems marcados como `low stock`.

**Acceptance Scenarios**:

1. **Given** stock por debajo del umbral, **When** se consulta el inventario, **Then** aparece marcado como bajo.
2. **Given** stock suficiente, **When** se consulta, **Then** aparece normal.

---

### Edge Cases

- Si el nombre del medicamento no coincide con inventario, la entrega debe fallar.
- Si el stock no cubre toda la receta, no debe permitirse entrega parcial.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-PH-001**: El sistema MUST mostrar recetas pendientes de entrega.
- **FR-PH-002**: El sistema MUST permitir marcar recetas como entregadas.
- **FR-PH-003**: El sistema MUST descontar inventario por ítem entregado.
- **FR-PH-004**: El sistema MUST rechazar entregas si hay stock insuficiente.
- **FR-PH-005**: El sistema MUST identificar ítems con bajo stock.

### Key Entities

- **Prescription**: receta con estado y detalles de entrega.
- **InventoryItem**: medicamento en stock con cantidad y umbral.
- **PrescriptionItem**: línea de receta.

## Success Criteria *(mandatory)*

- **SC-PH-001**: El farmacéutico ve recetas pendientes.
- **SC-PH-002**: La entrega de recetas actualiza el inventario.
- **SC-PH-003**: Las entregas sin stock se bloquean.

## Assumptions

- Las recetas solicitadas provienen del módulo médico.
- El farmacéutico accede solo a recetas en estado `Requested`.
