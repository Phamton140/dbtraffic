# Changelog

Todos los cambios notables de este proyecto se documentarán en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/),
y este proyecto adhiere a [Semantic Versioning](https://semver.org/lang/es/).

## [Unreleased]

### Fixed
- `Routes.razor`: se agregó `@rendermode InteractiveServer` para que todo el árbol de componentes de la aplicación (incluyendo `MainLayout` y las páginas) se ejecute dentro del mismo circuito interactivo de Blazor Server. Esto corrige el problema por el que los clics en botones, selects y otros componentes MudBlazor no respondían.
- `MainLayout.razor`: se movió `<MudPopoverProvider />` desde `App.razor` al layout principal. En Blazor Server con `InteractiveServer`, el provider debe estar dentro del circuito interactivo; de lo contrario, la UI se rompe al interactuar con componentes MudBlazor.
- `MainLayout.razor`: se movió la creación del timer de actualización de estado SQL a `OnAfterRender` para evitar fugas durante el prerendering.
- `DatabaseInitializer`: ya no silencia errores de inicialización. Ahora conecta primero a `master` para crear la base de datos `DbTraffic` si no existe, y luego aplica el esquema. Si falla, la aplicación se detiene con un error claro en lugar de arrancar en un estado roto.
- `InstanceEndpoints`: validación del formato del connection string con `SqlConnectionStringBuilder` en los endpoints `POST` y `PUT`. Devuelve `400 Bad Request` con el mensaje de error del parser de SQL Server.
- `Instance.Validate()`: ahora verifica que el connection string incluya al menos un componente `Server=` o `Data Source=`.

### Fixed
- `SqlServerInstanceClient.GetInstanceMetricsAsync`: corrección del error `Invalid column name 'timestamp'` al incluir `[timestamp]` en el SELECT interno de `sys.dm_os_ring_buffers`.
- `SqlServerInstanceClient.GetInstanceMetricsAsync`: separación de métricas en consultas independientes (ActiveRequests, BlockingSessions, WaitTimeMs, CpuPercent, MemoryPercent).
- Degradación elegente en `SqlServerInstanceClient`: si una métrica falla, se registra una advertencia y se devuelve `0` sin lanzar excepción ni detener `MonitoringWorker`.
- `SqlServerInstanceClient.CanConnectAsync`: ahora captura excepciones durante la apertura de conexión, evitando que connection strings inválidas propaguen errores.
- `DiscoveryService.DiscoverInstanceAsync`: ya no relanza excepciones cuando el descubrimiento de una instancia falla. Registra el error y continúa con las demás instancias activas.
- `App.razor`: se agregaron referencias a Bootstrap 5.3.2, Bootstrap Icons 1.11.1 y Bootstrap JS desde CDN para corregir el diseño de la UI.
- `SqlServerInstanceClient`: corrección de desbordamiento aritmético en `WaitTimeMs` al hacer `SUM(wait_time)`; ahora se usa `SUM(CAST(wait_time AS BIGINT))`.
- `SqlServerInstanceClient`: corrección de casting en `MemoryPercent` (`System.Decimal` a `System.Double`) forzando `CAST(... AS FLOAT)` en la consulta.
- `SqlServerInstanceClient`: casting a `FLOAT` en `CpuPercent` para garantizar compatibilidad de tipo de retorno con `reader.GetDouble`.

### Added
- Tests unitarios `InstanceTests` para validar reglas de dominio de instancias.
- Tests de integración `DatabaseInitializerTests` que verifican la creación automática de la base de datos y del esquema en un contenedor SQL Server limpio.
- Dashboard funcional en la página de inicio (`/`): muestra KPIs reales de instancias, procesos, ejecuciones, tasa de éxito, últimas ejecuciones y proceso más ejecutado.
- Endpoint `GET /api/dashboard/summary` para obtener datos agregados del dashboard.
- Modelos `DashboardSummary` y `DashboardExecution` en `DbTraffic.Shared`.
- Test de integración `DashboardTests.Summary_Endpoint_Returns_Dashboard_Data`.
- `DatabaseInitializer`: aplica automáticamente `database/schema.sql` al arrancar la aplicación si detecta que las tablas del producto no existen.
- `DomainExceptionMiddleware`: captura excepciones de dominio y devuelve respuestas `400 Bad Request` con el mensaje de validación, evitando que errores de validación rompan el circuito de Blazor Server.

### Changed
- `Instances.razor`: el snackbar de error ahora muestra el mensaje detallado devuelto por el servidor cuando falla la creación de una instancia.
- Páginas Blazor (`Instances.razor`, `Processes.razor`, `Discovery.razor`, `History.razor`, `Home.razor`, `Monitoring.razor`, `Recommendations.razor`, `RiskAnalysis.razor`): se quitó `@rendermode InteractiveServer` de cada página porque el rendermode interactivo ahora se aplica globalmente desde `Routes.razor`.
- Tests de integración de `DbTraffic.Web.Tests` ahora usan un `WebApplicationFactoryFixture` compartido basado en Testcontainers MsSql, con detección rápida de Docker para saltar los tests cuando no hay motor de contenedores disponible (por ejemplo, entornos de desarrollo locales sin Docker).

## [1.0.0] - 2026-06-20

### Added
- Fase 7: modernización completa de UI/UX con MudBlazor 9.5.0.
- Tema corporativo con paleta de colores: primario `#1565C0`, secundario `#1976D2`, éxito `#2E7D32`, advertencia `#ED6C02`, crítico `#D32F2F`, fondo `#F5F7FA`, superficie `#FFFFFF`.
- Layout profesional con `MudAppBar`, `MudDrawer` colapsable y topbar con estado SQL, hora y versión.
- Snackbar global para retroalimentación de acciones (crear, eliminar, importar, calibrar, etc.).
- Rediseño de todas las páginas Blazor con componentes MudBlazor (`MudTable`, `MudCard`, `MudSelect`, `MudProgressCircular`, `MudAlert`, `MudTabs`, `MudProgressLinear`).
- Página `/risk` convertida en experiencia principal con gauge circular grande, score numérico y lista de hallazgos con chips de color.
- Dashboard de inicio con tarjetas de navegación a las funcionalidades principales.
- Estados vacíos y esqueletos de carga en todas las pantallas.
- Tooltips y roles ARIA en botones de acción.

### Changed
- `App.razor`: reemplazadas referencias CDN de Bootstrap por fuentes Roboto y assets de MudBlazor.
- `Program.cs`: registro de `MudServices` con configuración de Snackbar.
- `_Imports.razor`: agregados usings globales de MudBlazor y `DbTraffic.Infrastructure.SqlServer`.
- `MainLayout.razor` y `NavMenu.razor`: reemplazados por implementaciones MudBlazor.

### Removed
- Dependencias CDN de Bootstrap 5.3.2, Bootstrap Icons y Bootstrap JS de `App.razor`.

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
