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

## [0.1.0] - Por definir

- Lanzamiento del MVP.
