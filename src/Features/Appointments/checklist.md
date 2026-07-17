# Checklist de Calidad: Appointments

## 1. Requisitos funcionales del rol Paciente / Médico / Administrador
- [ ] El paciente puede reservar una cita con especialidad y médico disponible.
- [ ] El paciente puede consultar sus citas activas y su estado.
- [ ] El paciente puede cancelar una cita respetando la regla de 24 horas.
- [ ] El médico puede ver su agenda y sus citas asignadas.
- [ ] El administrador puede ver y gestionar todas las citas.

## 2. Seguridad de datos clínicos y personales
- [ ] Los datos de pacientes y médicos no se exponen a usuarios sin autorización.
- [ ] El acceso a citas está filtrado por rol y propiedad (paciente/doctor).
- [ ] La validación de autorización se aplica en los endpoints y servicios.
- [ ] La API no devuelve información sensible de forma no autorizada.
- [ ] La cadena de conexión y secretos se leen desde configuración segura.

## 3. Criterios de aceptación del módulo
- [ ] Reservar una cita valida la disponibilidad y asigna el estado correcto.
- [ ] Cancelar una cita respeta la ventana de tiempo configurada.
- [ ] El paciente solo ve sus propias citas.
- [ ] El médico solo ve su propia agenda.
- [ ] La gestión de citas desde el administrador actualiza los estados.

## 4. Plan de pruebas y cobertura
- [ ] Existen pruebas unitarias para reglas de disponibilidad y cancelación.
- [ ] Existen pruebas de integración de endpoints con PostgreSQL real.
- [ ] Existe un flujo E2E que cubre reserva y consulta de citas.
- [ ] La cobertura mínima es 95% en lógica de dominio y aplicación.
- [ ] Se excluyen los archivos `obj`, `bin`, migraciones y archivos de configuración.
