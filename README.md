# DbTraffic

DbTraffic es una plataforma empresarial para la prevención de conflictos entre procesos de bases de datos SQL Server.

## Propósito

Muchas organizaciones ejecutan simultáneamente SQL Agent Jobs, procesos ETL, stored procedures, consultas manuales, procesos batch e integraciones que compiten por recursos, generando bloqueos, deadlocks, timeouts y lentitud.

DbTraffic actúa como un **controlador de tráfico para cargas de trabajo de bases de datos**: conoce qué procesos existen, qué objetos utilizan, qué recursos consumen, qué dependencias tienen y qué conflictos podrían generar, para recomendar decisiones antes de ejecutar un proceso.

## Estado del proyecto

- **Fase actual**: Fase 0 - Fundamentos.
- **Versión**: 0.1.0 (en desarrollo).
- **Estado del repositorio**: estable, estructura inicial lista.

Consulta la documentación en `/docs` para más detalles.

## Stack tecnológico

- **Lenguaje**: C# 12
- **Framework**: .NET 8
- **Base de datos del producto**: SQL Server
- **Interfaz web**: Blazor Server
- **Pruebas**: xUnit
- **Control de versiones**: Git

## Estructura del repositorio

```
├── database/        # Scripts SQL de esquema y seed
├── docs/            # Documentación del producto y ADRs
├── scripts/         # Scripts de utilidad y despliegue
├── src/             # Código fuente
│   ├── DbTraffic.Core/          # Dominio y motor de reglas
│   ├── DbTraffic.Infrastructure/# Acceso a datos y workers
│   ├── DbTraffic.Shared/        # Modelos y utilidades compartidos
│   └── DbTraffic.Web/           # API web y Blazor Server
└── tests/           # Pruebas automatizadas
    ├── DbTraffic.Core.Tests/
    ├── DbTraffic.Infrastructure.Tests/
    └── DbTraffic.Web.Tests/
```

## Configuración del entorno de desarrollo

### Requisitos

- .NET 8 SDK o superior
- SQL Server (LocalDB, Developer Edition o Docker) para la base de datos del producto
- Git

### Pasos

1. Clonar el repositorio:
   ```bash
   git clone https://github.com/tu-organizacion/dbtraffic.git
   cd dbtraffic
   ```

2. Restaurar paquetes:
   ```bash
   dotnet restore
   ```

3. Crear la base de datos del producto ejecutando los scripts en `database/schema.sql`.

4. Ejecutar la aplicación web:
   ```bash
   dotnet run --project src/DbTraffic.Web
   ```

5. Ejecutar pruebas:
   ```bash
   dotnet test
   ```

## Documentación relevante

- [Guía de instalación local](docs/SETUP.md)
- [Arquitectura](docs/ARCHITECTURE.md)
- [Alcance del MVP](docs/MVP-SCOPE.md)
- [Backlog](docs/BACKLOG.md)
- [Roadmap](docs/ROADMAP.md)
- [Decisiones de producto](docs/PRODUCT-DECISIONS.md)
- [Registro de cambios](docs/CHANGELOG.md)
- [ADRs](docs/ADRs/)

## Integración continua

El repositorio incluye un workflow de GitHub Actions en `.github/workflows/ci.yml` que compila la solución y ejecuta las pruebas en cada push y pull request.

## Licencia

Por definir.
