# Checklist de Calidad: Clinical History

## 1. Requisitos funcionales del rol Médico / Paciente
- [ ] El médico puede ver la historia clínica de pacientes con consultas previas.
- [ ] El paciente puede ver exclusivamente su propia historia clínica.
- [ ] El médico puede agregar observaciones y cerrar registros clínicos.
- [ ] Los registros se consultan y muestran con diagnósticos, tratamientos y recetas.

## 2. Seguridad de datos clínicos y personales
- [ ] La exhibición de la historia clínica está restringida según rol y paciente.
- [ ] El paciente no ve la historia de otros pacientes.
- [ ] El médico solo accede a pacientes autorizados por su relación clínica.
- [ ] El Administrador no accede a historias clínicas de pacientes sin permiso especial.
- [ ] Los datos sensibles se protegen en tránsito y en almacenamiento.
- [ ] La cadena de conexión y secretos no están hardcodeados.

## 3. Criterios de aceptación del módulo
- [ ] Los registros clínicos se almacenan con datos completos de la consulta.
- [ ] El médico puede cerrar registros y preventivamente no editarlos después.
- [ ] El paciente visualiza historial ordenado por fecha.
- [ ] La historia se muestra con sus elementos asociados (recetas, órdenes, observaciones).

## 4. Plan de pruebas y cobertura
- [ ] Existen pruebas unitarias para reglas de visibilidad y cierre de registros.
- [ ] Existen pruebas de integración de los endpoints de consulta de historia.
- [ ] Existe un E2E que verifica un flujo médico/paciente de acceso a historia.
- [ ] La cobertura mínima es 95% en lógica de dominio y aplicación.
- [ ] Se excluyen artefactos de build, migraciones y archivos configurables.
