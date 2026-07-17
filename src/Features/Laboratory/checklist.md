# Checklist de Calidad: Laboratory

## 1. Requisitos funcionales del rol Laboratorista / Médico / Paciente
- [ ] El laboratorista puede ver órdenes de laboratorio pendientes.
- [ ] El laboratorista puede registrar resultados para cada orden.
- [ ] El laboratorista puede validar resultados antes de publicarlos.
- [ ] El médico puede solicitar órdenes y ver resultados de sus pacientes.
- [ ] El paciente puede ver sus resultados de laboratorio.

## 2. Seguridad de datos clínicos y personales
- [ ] El acceso a órdenes y resultados está restringido por rol.
- [ ] Los pacientes solo ven sus propios resultados.
- [ ] El laboratorista no ve datos clínicos personales innecesarios.
- [ ] La cadena de conexión y secretos no están hardcodeados.
- [ ] Los resultados se transmiten de forma segura.

## 3. Criterios de aceptación del módulo
- [ ] El laboratorista puede registrar resultados y validar órdenes.
- [ ] No se valida una orden sin resultados asociados.
- [ ] Los resultados válidos son visibles para el médico y paciente autorizados.
- [ ] El inventario de laboratorio se gestiona y muestra correctamente.

## 4. Plan de pruebas y cobertura
- [ ] Existen pruebas unitarias para reglas de validación de órdenes.
- [ ] Existen pruebas de integración de endpoints con PostgreSQL real.
- [ ] Existe un E2E que cubre el flujo de registro y validación de resultados.
- [ ] La cobertura mínima es 95% en lógica de dominio y aplicación.
- [ ] Se excluyen artefactos de build, migraciones y archivos de configuración.
