# Checklist de Calidad: Pharmacy

## 1. Requisitos funcionales del rol Farmacéutico / Médico / Paciente
- [ ] El farmacéutico puede ver recetas pendientes de entrega.
- [ ] El farmacéutico puede dispensar recetas y descontar inventario.
- [ ] El farmacéutico puede actualizar cantidades de inventario.
- [ ] El médico puede ver recetas emitidas para sus pacientes.
- [ ] El paciente puede ver sus recetas y su estado.

## 2. Seguridad de datos clínicos y personales
- [ ] El acceso a recetas y stock está restringido por rol.
- [ ] Los pacientes ven solo sus recetas.
- [ ] Los farmacéuticos no ven datos clínicos más allá de la orden de receta necesaria.
- [ ] La cadena de conexión y secretos están en configuración segura.
- [ ] No se exponen contraseñas ni datos sensibles en respuestas.

## 3. Criterios de aceptación del módulo
- [ ] La dispensación de receta actualiza el inventario correctamente.
- [ ] No se permite dispensar sin stock suficiente.
- [ ] El paciente puede consultar el estado de su receta.
- [ ] La edición de inventario se registra y persiste.

## 4. Plan de pruebas y cobertura
- [ ] Existen pruebas unitarias para validación de stock y reglas de entrega.
- [ ] Existen pruebas de integración de endpoints con PostgreSQL real.
- [ ] Existe un E2E que valida el flujo de dispensación de receta.
- [ ] La cobertura mínima es 95% en lógica de dominio y aplicación.
- [ ] Se excluyen artefactos de build, migraciones y archivos de configuración.
