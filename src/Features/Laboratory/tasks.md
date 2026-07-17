# Tasks de Pruebas: Laboratory

## Pruebas unitarias
- [ ] Crear tests para la lógica de registro de resultados de laboratorio.
- [ ] Crear tests para la validación de orden antes de cambio a `Validated`.
- [ ] Crear tests para la gestión de inventario de laboratorio.
- [ ] Crear tests para reglas de acceso de rol `Laboratorista`.
- [ ] Crear tests para la consulta de órdenes y resultados.

## Pruebas de integración
- [ ] Crear tests de integración para el endpoint de registro de resultados con PostgreSQL real.
- [ ] Crear tests de integración para la validación de órdenes.
- [ ] Crear tests de integración para la consulta de resultados por paciente.
- [ ] Crear tests de integración para la gestión de inventario.
- [ ] Crear tests de integración para el acceso protegido del laboratorista.

## Pruebas end-to-end
- [ ] Crear un flujo E2E de laboratorista: login, ver orden pendiente, registrar resultado y validar.
- [ ] Crear un flujo E2E de paciente: login y ver resultados de laboratorio.
- [ ] Crear un flujo E2E para validar que una orden no se valida sin resultados.

## Cobertura final
- [ ] Ejecutar reporte de cobertura Coverlet para el módulo Laboratory.
- [ ] Añadir tests faltantes hasta alcanzar 95% de cobertura.
