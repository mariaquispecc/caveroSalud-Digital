# Checklist de Calidad: Public

## 1. Requisitos funcionales del rol visitante
- [ ] Un visitante anónimo puede ver la información institucional.
- [ ] Un visitante puede ver las especialidades disponibles.
- [ ] Un visitante puede leer pasos claros para agendar cita.
- [ ] La información pública no requiere autenticación.
- [ ] El contenido refleja los datos administrados por el panel de administración.

## 2. Seguridad de datos clínicos y personales
- [ ] No se expone información clínica en el portal público.
- [ ] No se muestra información personal de pacientes.
- [ ] La conexión a la base de datos usa configuración segura.
- [ ] El contenido público se entrega a través de endpoints sin datos sensibles.
- [ ] La cadena de conexión y secretos no están hardcodeados.

## 3. Criterios de aceptación del módulo
- [ ] La página pública muestra misión, visión y contacto.
- [ ] Las especialidades disponibles se listan correctamente.
- [ ] Los pasos para agendar son visibles y comprensibles.
- [ ] El contenido se puede actualizar desde el backend administrativo.
- [ ] Las páginas públicas cargan sin error ni login.

## 4. Plan de pruebas y cobertura
- [ ] Existen pruebas unitarias para la lógica de contenido público.
- [ ] Existen pruebas de integración que validan los endpoints públicos con PostgreSQL real.
- [ ] Existe un test E2E que valida la experiencia de un visitante público.
- [ ] La cobertura mínima es 95% en lógica de dominio y aplicación.
- [ ] Se excluyen `obj`, `bin`, migraciones y archivos de configuración de la medición.
