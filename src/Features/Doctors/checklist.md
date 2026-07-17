# Checklist de Calidad: Doctors

## 1. Requisitos funcionales del rol Médico
- [ ] El médico puede ver su agenda diaria y semanal.
- [ ] El médico puede acceder solo a sus citas asignadas.
- [ ] El médico puede registrar diagnósticos y observaciones.
- [ ] El médico puede generar órdenes de laboratorio.
- [ ] Los registros clínicos cerrados no se pueden editar nuevamente.
- [ ] El médico puede crear recetas y asociarlas a la cita.

## 2. Seguridad de datos clínicos y personales
- [ ] El médico no ve agendas ajenas ni historias de pacientes no autorizados.
- [ ] El acceso al endpoint de médico está protegido con rol `Médico`.
- [ ] Los datos personales del paciente se muestran solo si son necesarios.
- [ ] La cadena de conexión y los secretos se extraen de configuración segura.
- [ ] No se exponen contraseñas ni datos clínicos innecesarios.

## 3. Criterios de aceptación del módulo
- [ ] El médico puede consultar únicamente sus citas.
- [ ] El médico puede registrar una atención y guardar el registro clínico.
- [ ] Las órdenes de laboratorio y recetas se guardan correctamente.
- [ ] El estado de la cita se actualiza al finalizar la atención.
- [ ] Los resultados asociados son visibles cuando corresponda.

## 4. Plan de pruebas y cobertura
- [ ] Existen pruebas unitarias para las reglas de negocio médicas.
- [ ] Existen pruebas de integración que validan la API con PostgreSQL real.
- [ ] Existe un test E2E que cubre el flujo de atención del médico.
- [ ] La cobertura mínima es 95% en lógica de dominio y aplicación.
- [ ] Se excluyen `obj`, `bin`, migraciones y archivos de configuración de la medición.
