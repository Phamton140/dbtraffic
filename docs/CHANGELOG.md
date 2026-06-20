# Changelog

Todos los cambios notables de este proyecto se documentarán en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/),
y este proyecto adhiere a [Semantic Versioning](https://semver.org/lang/es/).

## [Unreleased]

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

## [0.1.0] - Por definir

- Lanzamiento del MVP.
