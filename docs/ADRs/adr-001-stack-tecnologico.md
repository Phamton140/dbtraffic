:# ADR-001: Stack tecnológico

## Estado

Aceptado

## Contexto

El producto está dirigido a organizaciones que ya utilizan SQL Server como motor de base de datos principal. Se requiere un stack tecnológico coherente con el ecosistema del cliente objetivo, mantenible, con buen soporte a largo plazo y que permita un desarrollo rápido del MVP.

## Decisión

- **Lenguaje**: C# 12
- **Framework**: .NET 8 (LTS)
- **Base de datos del producto**: SQL Server
- **Interfaz web**: Blazor Server con Minimal API
- **Pruebas**: xUnit
- **Control de versiones**: Git
- **Repositorio**: GitHub

## Consecuencias

### Positivas

- Coherencia total con el stack del cliente objetivo.
- .NET 8 es LTS, con soporte extendido.
- Blazor Server permite compartir modelos entre frontend y backend.
- SQL Server como BD del producto reduce la curva de aprendizaje operativo.
- xUnit es el framework de pruebas estándar en la comunidad .NET.

### Negativas

- Blazor Server requiere conexión persistente y tiene limitaciones de escalabilidad horizontal.
- SQL Server puede implicar costos de licenciamiento adicionales.
- No se aprovechan ecosistemas alternativos (Node, Python) para prototipado rápido.

## Alternativas consideradas

- **Blazor WASM**: mejor escalabilidad frontend, pero mayor complejidad inicial y requiere API más robusta.
- **ASP.NET MVC con Razor Pages**: más maduro, pero menos moderno y menos adecuado para interactividad en tiempo real.
- **PostgreSQL como BD del producto**: más ligero, pero rompe coherencia con el cliente SQL Server.

## Notas

La decisión de Blazor Server se revisará en el roadmap post-MVP si se requiere mayor escalabilidad de frontend.
