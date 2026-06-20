# Arquitectura de DbTraffic

## Visión general

DbTraffic es una aplicación .NET 8 compuesta por una aplicación web (Blazor Server + API), servicios de background (workers), un motor de reglas puro, y una base de datos SQL Server propia. La plataforma se conecta a instancias SQL Server objetivo únicamente en modo lectura, usando DMV, Query Store y catálogos del sistema.

## Principios de diseño

1. **Separación de responsabilidades**: dominio, infraestructura, aplicación web y pruebas son proyectos independientes.
2. **Mínima intrusión**: no se instalan triggers, traces pesados ni modificaciones en las instancias objetivo.
3. **Extensibilidad**: el motor de reglas es plugable para permitir nuevas reglas sin reescribir lógica central.
4. **Observabilidad propia**: toda decisión, ejecución y hallazgo queda registrada en la base de datos del producto.
5. **Configuración como código**: procesos críticos y reglas pueden versionarse en Git.

## Diagrama de componentes

```
┌─────────────────────────────────────────────────────────────┐
│                         Cliente Web                         │
│                    (Blazor Server / HTTP)                   │
└───────────────────────────┬─────────────────────────────────┘
                            │ HTTPS
┌───────────────────────────▼─────────────────────────────────┐
│              API Web (.NET 8 Minimal API)                   │
│  - Gestión de procesos                                     │
│  - Consulta de riesgo                                      │
│  - Recomendaciones                                         │
│  - Historial                                               │
└───────────────────────────┬─────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        ▼                   ▼                   ▼
┌──────────────┐   ┌──────────────┐   ┌─────────────────┐
│   Motor de   │   │   Servicio   │   │   Procesador    │
│   Reglas     │   │  Descubridor │   │   de Historial  │
│  (Rules      │   │   (Scanner)  │   │   (Aggregator)  │
│   Engine)    │   │              │   │                 │
└──────┬───────┘   └──────┬───────┘   └────────┬────────┘
       │                  │                    │
       └──────────────────┼────────────────────┘
                          │
              ┌───────────▼────────────┐
              │    Base de datos       │
              │   del producto         │
              │  (SQL Server)          │
              │  - Catálogo            │
              │  - Historial           │
              │  - Métricas            │
              │  - Decisiones          │
              └───────────┬────────────┘
                          │
              ┌───────────▼────────────┐
              │   Instancias objetivo  │
              │   (SQL Server)         │
              │  DMV, Query Store,     │
              │  msdb, Agent Jobs      │
              └────────────────────────┘
```

## Componentes

### DbTraffic.Shared

Biblioteca de clases con modelos, contratos y utilidades compartidos entre todos los proyectos. No tiene dependencias de infraestructura.

- Modelos de dominio serializables.
- Contratos de servicios (interfaces).
- Excepciones de dominio.
- Utilidades comunes (fechas, validaciones, etc.).

### DbTraffic.Core

Biblioteca de clases con la lógica de negocio pura.

- Entidades de dominio.
- Motor de reglas (`IRule`, `RuleContext`, `RuleResult`).
- Cálculo de riesgo.
- Algoritmos de recomendación.
- No depende de SQL Server, HTTP ni otros frameworks de infraestructura.

### DbTraffic.Infrastructure

Biblioteca de clases con acceso a datos y servicios externos.

- Repositorios SQL Server.
- Workers de background (`IHostedService`).
- Clientes de lectura de instancias objetivo (DMV, Query Store, msdb).
- Configuración y conexiones.

### DbTraffic.Web

Aplicación Blazor Server con API web integrada.

- Páginas y componentes Blazor.
- Endpoints Minimal API.
- Autenticación y autorización (MVP: básica).
- Health checks.

## Flujo de datos

1. El **Servicio Descubridor** lee periódicamente las instancias objetivo y almacena la información en la base de datos del producto.
2. El usuario registra procesos y sus objetos relevantes a través de la UI/API.
3. Cuando se solicita un análisis de riesgo, el **Motor de Reglas** evalúa el contexto usando el catálogo, el historial y el estado actual.
4. El **Procesador de Historial** agrega ejecuciones pasadas para calibrar duraciones y detectar patrones.
5. La UI presenta el riesgo, conflictos y recomendaciones.

## Decisiones de arquitectura

Ver ADRs relacionados:

- [ADR-001: Stack tecnológico](ADRs/adr-001-stack-tecnologico.md)
- [ADR-002: Arquitectura de componentes](ADRs/adr-002-arquitectura-componentes.md)
- [ADR-003: Enfoque del MVP](ADRs/adr-003-enfoque-mvp.md)

## Patrones aplicados

- **Clean Architecture**: separación en capas con dependencias dirigidas hacia el centro.
- **Repository Pattern**: abstracción del acceso a datos.
- **BackgroundService**: workers de descubrimiento y monitoreo.
- **Options Pattern**: configuración tipada.
- **Minimal API**: endpoints concisos para el MVP.

## Escalabilidad futura

- El motor de reglas puede ejecutarse como servicio independiente.
- Los workers pueden escalar horizontalmente si se usa una cola de mensajes en el futuro.
- La UI puede migrarse a Blazor WASM o SPA si se requiere mayor escalabilidad de frontend.
