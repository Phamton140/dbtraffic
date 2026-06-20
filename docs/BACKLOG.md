# Backlog de DbTraffic

## Leyenda

- **Prioridad**: Alta / Media / Baja
- **Estado**: Pendiente / En progreso / Completado / Cancelado
- **Fase**: 0-6 según plan de desarrollo

---

## Fase 0: Fundamentos

| ID | Tarea | Prioridad | Estado | Fase |
|----|-------|-----------|--------|------|
| B-001 | Crear repositorio Git y estructura de carpetas | Alta | Completado | 0 |
| B-002 | Definir stack tecnológico y documentar ADRs iniciales | Alta | Completado | 0 |
| B-003 | Crear solución .NET 8 con proyectos Core, Infrastructure, Shared, Web y tests | Alta | Completado | 0 |
| B-004 | Definir modelo de dominio inicial y esquema SQL Server del producto | Alta | Completado | 0 |
| B-005 | Implementar conexión de prueba y lectura básica de DMV | Alta | Completado | 0 |
| B-006 | Configurar pipeline de CI básica (build + test) | Media | Completado | 0 |
| B-007 | Documentar guía de instalación local | Media | Completado | 0 |

## Fase 1: Catálogo de Procesos

| ID | Tarea | Prioridad | Estado | Fase |
|----|-------|-----------|--------|------|
| B-008 | Implementar entidades de dominio de procesos y objetos | Alta | Completado | 1 |
| B-009 | Implementar repositorios de catálogo en SQL Server | Alta | Completado | 1 |
| B-010 | Crear API CRUD de procesos | Alta | Completado | 1 |
| B-011 | Crear UI CRUD de procesos en Blazor | Alta | Completado | 1 |
| B-012 | Crear API CRUD de instancias objetivo | Alta | Completado | 1 |
| B-013 | Crear UI CRUD de instancias objetivo | Media | Completado | 1 |
| B-014 | Agregar validaciones de dominio | Media | Completado | 1 |
| B-015 | Tests unitarios e integración del catálogo | Alta | Completado | 1 |

## Fase 2: Descubrimiento Asistido

| ID | Tarea | Prioridad | Estado | Fase |
|----|-------|-----------|--------|------|
| B-016 | Implementar lector de SQL Agent Jobs | Alta | Completado | 2 |
| B-017 | Implementar lector de objetos de esquema | Alta | Completado | 2 |
| B-018 | Crear worker de descubrimiento periódico | Alta | Completado | 2 |
| B-019 | Crear pantalla de jobs y objetos descubiertos | Media | Completado | 2 |
| B-020 | Implementar asociación manual job-proceso | Alta | Completado | 2 |
| B-021 | Almacenar snapshots de descubrimiento | Media | Completado | 2 |
| B-022 | Tests de integración del descubrimiento | Alta | Completado | 2 |

## Fase 3: Motor de Reglas y Riesgo

| ID | Tarea | Prioridad | Estado | Fase |
|----|-------|-----------|--------|------|
| B-023 | Definir abstracciones del motor de reglas (`IRule`, `RuleContext`, `RuleResult`) | Alta | Completado | 3 |
| B-024 | Implementar `ObjectOverlapRule` | Alta | Completado | 3 |
| B-025 | Implementar `HighIntensityOverlapRule` | Alta | Completado | 3 |
| B-026 | Implementar `EstimatedDurationExceedsWindowRule` | Media | Completado | 3 |
| B-027 | Implementar `InstanceResourcePressureRule` | Media | Completado | 3 |
| B-028 | Implementar cálculo de score de riesgo | Alta | Completado | 3 |
| B-029 | Crear endpoint `GET /risk` | Alta | Completado | 3 |
| B-030 | Crear pantalla de análisis de riesgo | Alta | Completado | 3 |
| B-031 | Tests unitarios del motor de reglas (>80%) | Alta | Completado | 3 |

## Fase 4: Recomendaciones

| ID | Tarea | Prioridad | Estado | Fase |
|----|-------|-----------|--------|------|
| B-032 | Diseñar algoritmo de búsqueda de ventanas de bajo riesgo | Alta | Completado | 4 |
| B-033 | Implementar servicio de recomendaciones | Alta | Completado | 4 |
| B-034 | Crear endpoint `GET /recommendations` | Alta | Completado | 4 |
| B-035 | Crear pantalla de recomendaciones | Alta | Completado | 4 |
| B-036 | Implementar simulación "ejecutar ahora" | Media | Completado | 4 |
| B-037 | Tests del algoritmo de recomendaciones | Alta | Completado | 4 |

## Fase 5: Monitoreo e Historial

| ID | Tarea | Prioridad | Estado | Fase |
|----|-------|-----------|--------|------|
| B-038 | Implementar worker de monitoreo de DMV | Alta | Completado | 5 |
| B-039 | Crear dashboard de actividad actual | Alta | Completado | 5 |
| B-040 | Implementar registro de ejecuciones manuales | Alta | Completado | 5 |
| B-041 | Importar historial desde `msdb.dbo.sysjobhistory` | Media | Completado | 5 |
| B-042 | Calibrar duraciones estimadas con historial | Media | Completado | 5 |
| B-043 | Pantalla de historial de ejecuciones | Media | Completado | 5 |
| B-044 | Tests de integración del monitoreo | Alta | Completado | 5 |

## Fase 6: Cierre del MVP

| ID | Tarea | Prioridad | Estado | Fase |
|----|-------|-----------|--------|------|
| B-045 | Revisión de arquitectura y deuda técnica | Alta | Completado | 6 |
| B-046 | Documentación de usuario | Alta | Completado | 6 |
| B-047 | Documentación de operación | Alta | Completado | 6 |
| B-048 | Pruebas end-to-end con Playwright | Media | Completado | 6 |
| B-049 | Pruebas de carga de la API | Baja | Pendiente | 6 |
| B-050 | Preparar demo interno | Alta | Pendiente | 6 |
| B-051 | Release 0.1.0 | Alta | En progreso | 6 |

## Backlog futuro (post-MVP)

| ID | Tarea | Prioridad |
|----|-------|-----------|
| F-001 | Soporte multi-instancia y multi-tenant | Media |
| F-002 | Orquestación automática de jobs | Baja |
| F-003 | Machine learning estadístico para predicción de duración | Media |
| F-004 | Integración con Azure SQL y AWS RDS | Baja |
| F-005 | Notificaciones (email, Teams, webhooks) | Media |
| F-006 | API pública documentada (OpenAPI) | Alta |
| F-007 | Marketplace de reglas personalizadas | Baja |
| F-008 | Migración de UI a Blazor WASM o SPA | Media |
| F-009 | Dashboard de métricas de negocio (SLAs, costos) | Medium |
| F-010 | Autenticación avanzada y control de roles | Alta |
