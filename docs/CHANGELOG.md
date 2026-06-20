# Changelog

Todos los cambios notables de este proyecto se documentarán en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/),
y este proyecto adhiere a [Semantic Versioning](https://semver.org/lang/es/).

## [Unreleased]

### Fixed
- `SqlServerInstanceClient.GetInstanceMetricsAsync`: corrección del error `Invalid column name 'timestamp'` al incluir `[timestamp]` en el SELECT interno de `sys.dm_os_ring_buffers`.
- `SqlServerInstanceClient.GetInstanceMetricsAsync`: separación de métricas en consultas independientes (ActiveRequests, BlockingSessions, WaitTimeMs, CpuPercent, MemoryPercent).
- Degradación elegente en `SqlServerInstanceClient`: si una métrica falla, se registra una advertencia y se devuelve `0` sin lanzar excepción ni detener `MonitoringWorker`.
- `SqlServerInstanceClient.CanConnectAsync`: ahora captura excepciones durante la apertura de conexión, evitando que connection strings inválidas propaguen errores.
- `DiscoveryService.DiscoverInstanceAsync`: ya no relanza excepciones cuando el descubrimiento de una instancia falla. Registra el error y continúa con las demás instancias activas.
- `App.razor`: se agregaron referencias a Bootstrap 5.3.2, Bootstrap Icons 1.11.1 y Bootstrap JS desde CDN para corregir el diseño de la UI.

### Changed
- `MonitoringService` y `ExecutionService` ahora reciben `ILogger<SqlServerInstanceClient>` y lo pasan a las instancias de `SqlServerInstanceClient`.

### Added
- Tests para `SqlServerInstanceClient`: verificación de métricas contra Testcontainers y validación de `CanConnectAsync` con connection string inválida.
- Tests para `DiscoveryService`: validación de que una instancia defectuosa no impide descubrir las demás.

### Added
- Tests para `SqlServerInstanceClient`: verificación de métricas contra Testcontainers y validación de `CanConnectAsync` con connection string inválida.

## [0.1.0] - 2026-06-20

### Added
- Estructura inicial del repositorio.
- Solución .NET 8 con proyectos Core, Infrastructure, Shared, Web y tests.
- Documentación base: README, arquitectura, alcance del MVP, backlog, roadmap.
- ADRs iniciales: stack tecnológico, arquitectura de componentes, enfoque del MVP.
- Registro de decisiones de producto y changelog.
- Cliente SQL Server para lectura de DMV (`SqlServerInstanceClient`).
- Modelos iniciales para instancias y requests activas.
- Endpoint de prueba `/api/health/sql` para validar conectividad con SQL Server.
- Dependencia `Microsoft.Data.SqlClient` para acceso a SQL Server.
- Workflow de GitHub Actions CI en `.github/workflows/ci.yml`.
- Guía de instalación local en `docs/SETUP.md`.
- Entidades de dominio: `Instance`, `Process`, `ProcessObject`, `ProcessSchedule`.
- Enums de dominio: `ProcessType`, `IntensityLevel`, `ObjectType`, `ObjectAccessType`.
- Repositorios SQL Server con Dapper: `InstanceRepository`, `ProcessRepository`.
- Factoría de conexiones `SqlConnectionFactory`.
- Endpoints CRUD `/api/instances` y `/api/processes`.
- UI Blazor Server: páginas de instancias y procesos con listado, creación y eliminación.
- Validaciones de dominio y excepción `DomainException`.
- Tests unitarios de validación de entidades.
- Entidades de descubrimiento: `DiscoveredJob`, `DiscoveredObject`.
- Lector SQL Server para jobs (`msdb.dbo.sysjobs`) y objetos (`sys.objects`).
- Repositorio `DiscoveryRepository` con persistencia y asociación.
- `DiscoveryService` y `DiscoveryWorker` para descubrimiento periódico.
- Endpoints de descubrimiento: `/api/discovery/run`, `/api/discovery/jobs`, `/api/discovery/objects`, `/api/discovery/associate`.
- Página Blazor `/discovery` para ejecutar descubrimiento y asociar jobs a procesos.
- Tests de integración del repositorio de descubrimiento.
- Dependencias Dapper y Microsoft.Extensions.Hosting/Logging/Options.
- Motor de reglas: abstracciones `IRule`, `RuleContext`, `RuleResult`, `RiskLevel`.
- Reglas implementadas: `ObjectOverlapRule`, `HighIntensityOverlapRule`, `EstimatedDurationExceedsWindowRule`, `InstanceResourcePressureRule`.
- `RiskCalculationService` para agregar scores y determinar nivel de riesgo.
- `RiskContextProvider` para construir el contexto desde repositorios y DMV.
- Endpoint `GET /api/risk` para consultar riesgo de un proceso en un horario propuesto.
- Página Blazor `/risk` para análisis visual de riesgo.
- Tests unitarios del motor de reglas (21 tests, cobertura completa de reglas).
- Algoritmo de búsqueda de ventanas de bajo riesgo.
- `RecommendationService` con evaluación iterativa de ventanas candidatas.
- Endpoint `GET /api/recommendations` con parámetros de rango y granularidad.
- Página Blazor `/recommendations` para buscar ventanas y simular ejecución "ahora".
- Tests unitarios del servicio de recomendaciones con Moq.
- Entidades de monitoreo e historial: `Execution`, `InstanceSnapshot`.
- Repositorios `ExecutionRepository` e `InstanceSnapshotRepository`.
- Modelos DMV: `InstanceMetrics`, `JobHistoryEntry`.
- Extensión de `SqlServerInstanceClient` para métricas de instancia e historial de jobs.
- `MonitoringService` y `MonitoringWorker` para captura periódica de snapshots.
- `ExecutionService` para registro manual, importación desde `msdb.dbo.sysjobhistory` y calibración de duraciones.
- Endpoints `/api/monitoring` y `/api/executions`.
- Páginas Blazor `/monitoring` y `/history`.
- Tests unitarios e integración para ejecuciones y snapshots.
- Proyecto de pruebas end-to-end `DbTraffic.E2ETests` con Playwright.
- `DbTrafficWebApplicationFactory` para levantar la aplicación con Kestrel real y SQL Server en Testcontainers durante los tests E2E.
- Tests E2E de navegación básica: carga de home page y navegación a instancias.

### Changed
- Pipeline CI actualizada para instalar navegadores de Playwright y ejecutar tests E2E (`RUN_E2E_TESTS=true`).
- Backlog actualizado con Fase 5 completada.

### Fixed
- Aplicación de `database/schema.sql` dividida en batches por `GO` para compatibilidad con Dapper en tests de integración.
- Estrategia de espera de Testcontainers basada en puerto TCP 1433 para estabilidad en GitHub Actions.
- `RiskContextProvider` ahora utiliza métricas reales de la instancia (`GetInstanceMetricsAsync`) en lugar de construir `InstanceResourceState` con valores fijos en cero. Esto activa correctamente `InstanceResourcePressureRule` para bloqueos y, cuando las DMVs lo permiten, CPU y memoria.
