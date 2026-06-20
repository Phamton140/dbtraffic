# ADR-002: Arquitectura de componentes

## Estado

Aceptado

## Contexto

DbTraffic requiere separar claramente la lógica de negocio del acceso a datos, la interfaz web y los servicios de background. Esto facilita las pruebas, el mantenimiento y la evolución futura.

## Decisión

Dividir la solución en los siguientes proyectos:

1. **DbTraffic.Shared**: modelos, contratos y utilidades compartidas.
2. **DbTraffic.Core**: dominio, motor de reglas y algoritmos puros.
3. **DbTraffic.Infrastructure**: acceso a datos, workers y lectores de instancias objetivo.
4. **DbTraffic.Web**: aplicación Blazor Server y API web.

Las dependencias son:

- `DbTraffic.Core` depende de `DbTraffic.Shared`.
- `DbTraffic.Infrastructure` depende de `DbTraffic.Shared` y `DbTraffic.Core`.
- `DbTraffic.Web` depende de `DbTraffic.Shared`, `DbTraffic.Core` e `DbTraffic.Infrastructure`.

## Consecuencias

### Positivas

- Lógica de negocio testeable sin dependencias de infraestructura.
- Facilidad para reemplazar implementaciones (por ejemplo, cambiar el motor de base de datos del producto en el futuro).
- Workers y API pueden escalar de forma independiente.

### Negativas

- Mayor número de proyectos que una solución monolítica simple.
- Curva de aprendizaje inicial para nuevos desarrolladores.

## Alternativas consideradas

- **Aplicación monolítica en un solo proyecto**: más simple al inicio, pero difícil de escalar y probar.
- **Microservicios desde el inicio**: excesivo para un MVP, aumenta complejidad operativa.

## Notas

La arquitectura puede evolucionar hacia servicios independientes si el producto crece, pero el diseño actual permite esa transición sin reescribir la lógica central.
