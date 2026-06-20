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
