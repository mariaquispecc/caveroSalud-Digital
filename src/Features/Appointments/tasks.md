# Tasks de Pruebas: Appointments

## Pruebas unitarias
- [ ] Crear tests para la validación de disponibilidad de citas.
- [ ] Crear tests para la validación de disponibilidad basada en bloques de horario / `DoctorAvailability`.
- [ ] Crear tests para la regla de cancelación con 24 horas de antelación.
- [ ] Crear tests para la creación de citas con doctor, paciente y especialidad.
- [ ] Crear tests para la consulta de citas por paciente.
- [ ] Crear tests para la lógica de estados de cita.

## Pruebas de integración
- [ ] Crear tests de integración para el endpoint de creación de citas con PostgreSQL real.
- [ ] Crear tests de integración para la cancelación de citas.
- [ ] Crear tests de integración para la consulta de citas del paciente.
- [ ] Crear tests de integración para la consulta de agenda del médico.
- [ ] Crear tests de integración para la gestión de citas con rol administrador.

## Pruebas end-to-end
- [ ] Crear un flujo E2E de paciente: login, reservar cita, ver confirmación.
- [ ] Crear un flujo E2E de paciente: cancelar cita y verificar el estado.
- [ ] Crear un flujo E2E de médico: login y ver agenda.

## Cobertura final
- [ ] Ejecutar reporte de cobertura Coverlet para el módulo Appointments.
- [ ] Añadir tests faltantes hasta alcanzar 95% de cobertura en lógica de dominio y aplicación.
