# Registro de decisiones de producto

## 2026-06-20: Aprobación del alcance del MVP

**Decisión**: Aprobar el alcance reducido del MVP con enfoque híbrido (registro manual + descubrimiento asistido) y motor de reglas sin IA.

**Razón**: Reducir riesgo técnico y acelerar time-to-value.

**Impacto**: El MVP no intentará descubrir automáticamente todas las dependencias ni usar IA.

**Responsable**: Product Owner / CTO

## 2026-06-20: Selección de Blazor Server

**Decisión**: Usar Blazor Server como tecnología de interfaz web para el MVP.

**Razón**: Mismo stack .NET, desarrollo rápido, comparte modelos con backend.

**Impacto**: Escalabilidad horizontal limitada en frontend. Se evaluará migración post-MVP.

**Responsable**: Arquitecto

## 2026-06-20: SQL Server como base de datos del producto

**Decisión**: Usar SQL Server como base de datos principal del producto.

**Razón**: Coherencia con el ecosistema del cliente objetivo y conocimiento existente en el equipo.

**Impacto**: Posible costo adicional de licenciamiento. No se soportan otros motores en el MVP.

**Responsable**: Arquitecto

## 2026-06-20: Estructura de solución en proyectos separados

**Decisión**: Dividir la solución en cuatro proyectos principales (`DbTraffic.Shared`, `DbTraffic.Core`, `DbTraffic.Infrastructure`, `DbTraffic.Web`) y tres proyectos de prueba (`DbTraffic.Core.Tests`, `DbTraffic.Infrastructure.Tests`, `DbTraffic.Web.Tests`).

**Razón**: Mantener separación de responsabilidades, facilitar pruebas unitarias y permitir evolución independiente de componentes.

**Impacto**: Mayor cantidad de proyectos que una solución monolítica simple, pero mejor mantenibilidad a mediano plazo.

**Responsable**: Arquitecto

## 2026-06-20: Uso de Microsoft.Data.SqlClient para acceso a SQL Server

**Decisión**: Usar `Microsoft.Data.SqlClient` como proveedor de acceso a datos para SQL Server en lugar de `System.Data.SqlClient`.

**Razón**: Es el proveedor recomendado y mantenido por Microsoft para .NET Core/.NET 5+, con mejoras de rendimiento y seguridad.

**Impacto**: Dependencia adicional, pero alineada con el ecosistema .NET moderno.

**Responsable**: Arquitecto

## 2026-06-20: Uso de Dapper para acceso a datos

**Decisión**: Usar Dapper como micro-ORM para los repositorios SQL Server del producto.

**Razón**: Ligero, rápido, permite control total del SQL y es adecuado para un MVP donde las consultas son explícitas.

**Impacto**: Menos productividad que Entity Framework para cambios de esquema, pero mejor rendimiento y transparencia.

**Responsable**: Arquitecto

## 2026-06-20: Exponer entidades de dominio en la API y UI

**Decisión**: En el MVP, la API Minimal y los componentes Blazor usan directamente las entidades de dominio (`Instance`, `Process`).

**Razón**: Reduce la cantidad de DTOs duplicados y acelera el desarrollo inicial.

**Impacto**: Acoplamiento temporal entre contratos de API y modelo de dominio. Se introducirán DTOs cuando la API evolucione hacia versión pública o multi-cliente.

**Responsable**: Arquitecto
