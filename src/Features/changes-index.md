# Índice de Cambios de Features

Este documento resume los ajustes recientes aplicados a la documentación de `src/Features`, con enlaces directos a los módulos y tipos de artefactos.

## Resumen por módulo

- `Administration`
  - Ajustes en `plan.md` y checklist para gestión de horarios y disponibilidad.
  - Pruebas añadidas para validación de bloqueos de calendario.

- `Appointments`
  - Clarificación de disponibilidad, bloques de horario y verificaciones de solapamiento.
  - Tareas de pruebas específicas para reservas válidas y estados de cita.

- `Authentication`
  - Alineación de roles con `Paciente`, `Médico`, `Laboratorista`, `Farmacéutico`, `Administrador`.
  - Requisitos de JWT y control de sesión centralizados.

- `ClinicalHistory`
  - Definición más clara del rol de Administrador como auditor/configurador.
  - Validación de acceso y protección de registros clínicos.

- `Doctors`
  - Requisito añadido para bloquear la edición de registros clínicos cerrados.
  - Pruebas de estado de record cerrado y autorización de edición.

- `Laboratory`
  - Cobertura de flujo de resultados y notificaciones del laboratorio.
  - Casos de prueba para publicación y acceso de resultados.

- `Pharmacy`
  - Flujo de validación de recetas, inventario y pedidos.
  - Pruebas de autorización y reconciliación de stock.

- `Public`
  - Refuerzo de la sincronización de contenido validado por administrador.
  - Integración y E2E para exposición pública de información institucional.

## Documentos centrales

- `plan-overview.md`: actualizado con resumen de cambios recientes.
- `speckit-changes-summary.md`: mantiene un estado consolidado de las correcciones y ajustes de speckit.

## Uso

Este índice puede usarse como referencia rápida para revisar qué módulos recibieron cambios de documentación y qué aspectos clave deben ser validados antes de la implementación.
