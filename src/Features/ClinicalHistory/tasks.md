# Tasks de Pruebas: Clinical History

## Pruebas unitarias
- [ ] Crear tests para la lógica de visibilidad de la historia clínica.
- [ ] Crear tests para la creación y cierre de registros clínicos.
- [ ] Crear tests para la asociación de recetas y órdenes a un registro.
- [ ] Crear tests para la validación de acceso por rol.
- [ ] Crear tests para la consulta de historia filtrada por paciente.

## Pruebas de integración
- [ ] Crear tests de integración para el endpoint de consulta de historia del paciente.
- [ ] Crear tests de integración para que el médico acceda a la historia de pacientes autorizados.
- [ ] Crear tests de integración para verificar que un administrador sin permiso no accede a historias clínicas ordinarias.
- [ ] Crear tests de integración para la creación de un registro clínico.
- [ ] Crear tests de integración para el cierre de registros y prohibición de edición posterior.
- [ ] Crear tests de integración para la consulta de datos asociados (recetas, órdenes).

## Pruebas end-to-end
- [ ] Crear un flujo E2E de paciente que consulte su historia clínica.
- [ ] Crear un flujo E2E de médico que acceda a la historia de un paciente y agregue observaciones.
- [ ] Crear un flujo E2E que verifique la denegación de acceso si un médico no está autorizado.

## Cobertura final
- [ ] Ejecutar reporte de cobertura Coverlet para el módulo Clinical History.
- [ ] Añadir tests faltantes hasta alcanzar 95% de cobertura.
