# Notas de release - DbTraffic v0.1.0

**Fecha**: 2026-06-20

## Resumen

DbTraffic v0.1.0 es el **MVP** de la plataforma para prevención de conflictos entre procesos SQL Server. Responde la pregunta central **"¿Es seguro ejecutar este proceso ahora?"** mediante un motor de reglas explícito, datos de catálogo, horarios y estado de la instancia.

---

## Funcionalidades incluidas

### Fase 0 - Fundamentos
- Solución .NET 8 con proyectos Core, Infrastructure, Shared, Web y tests.
- Documentación base: arquitectura, alcance del MVP, backlog, roadmap y ADRs.
- Pipeline CI en GitHub Actions.

### Fase 1 - Catálogo de procesos
- CRUD de instancias SQL Server y procesos.
- Registro de objetos críticos y horarios de ejecución.
- Validaciones de dominio.

### Fase 2 - Descubrimiento asistido
- Lectura de SQL Agent Jobs y objetos de esquema desde la instancia objetivo.
- Worker de descubrimiento periódico.
- Pantalla de asociación manual job-proceso.

### Fase 3 - Motor de reglas y riesgo
- Abstracciones `IRule`, `RuleContext`, `RuleResult`, `RiskLevel`.
- Reglas implementadas:
  - `ObjectOverlapRule`
  - `HighIntensityOverlapRule`
  - `EstimatedDurationExceedsWindowRule`
  - `InstanceResourcePressureRule`
- `RiskCalculationService` y `RiskContextProvider`.
- Endpoint `GET /api/risk` y página `/risk`.

### Fase 4 - Recomendaciones
- Algoritmo de búsqueda de ventanas de bajo riesgo.
- `RecommendationService` y endpoint `GET /api/recommendations`.
- Página `/recommendations` con simulación "ejecutar ahora".

### Fase 5 - Monitoreo e historial
- Entidades `Execution` e `InstanceSnapshot`.
- `MonitoringService`, `ExecutionService` y workers de background.
- Importación de historial desde `msdb.dbo.sysjobhistory`.
- Calibración de duraciones estimadas con ejecuciones pasadas.
- Endpoints `/api/monitoring` y `/api/executions`.
- Páginas `/monitoring` y `/history`.

### Fase 6 - Cierre del MVP
- Pruebas end-to-end con Playwright.
- Corrección de `RiskContextProvider` para usar métricas reales de la instancia (bloqueos, CPU, memoria).
- Documentación de usuario, operaciones y notas de release.

---

## Métricas de calidad

| Indicador | Valor |
|-----------|-------|
| Tests unitarios + integración + E2E | 43 |
| Cobertura de reglas del motor | 100 % de reglas implementadas |
| Build | 0 advertencias, 0 errores |
| Pipeline CI | Verde |

---

## Limitaciones conocidas

- **CPU y memoria**: se intentan leer desde DMVs del sistema (`sys.dm_os_ring_buffers`, `sys.dm_os_process_memory`). La disponibilidad y precisión dependen de la edición de SQL Server, permisos y SO. En algunos entornos pueden reportar 0 o valores aproximados.
- **Bloqueos**: se leen correctamente desde `sys.dm_exec_requests` / `sys.dm_os_waiting_tasks`.
- **Sin acción automática**: DbTraffic recomienda pero no ejecuta ni detiene procesos.
- **Objetos críticos**: requieren registro manual o asociación desde el descubrimiento.
- **Escalabilidad**: v0.1.0 es una única instancia Blazor Server. La ejecución horizontal requiere trabajo futuro.

---

## Instrucciones de instalación

Consulta:

- [Guía de instalación local](SETUP.md)
- [Guía de usuario](USER_GUIDE.md)
- [Guía de operaciones](OPERATIONS_GUIDE.md)

---

## Próximos pasos (post-MVP)

- Soporte multi-instancia y multi-tenant.
- API pública documentada (OpenAPI).
- Notificaciones (email, Teams, webhooks).
- Predicción estadística de duración.
- Autenticación avanzada y control de roles.

---

## Enlaces

- Repositorio: https://github.com/Phamton140/dbtraffic
- Changelog completo: [CHANGELOG.md](CHANGELOG.md)
