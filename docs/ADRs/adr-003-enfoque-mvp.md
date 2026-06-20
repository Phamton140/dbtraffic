# ADR-003: Enfoque del MVP

## Estado

Aceptado

## Contexto

La idea original propone descubrir automáticamente procesos, dependencias y conflictos para prevenir problemas antes de que ocurran. Sin embargo, el descubrimiento automático completo de dependencias en SQL Server es técnicamente difícil (SQL dinámico, tablas temporales, dependencias externas) y puede generar resultados pobres o falsos positivos.

## Decisión

El MVP se enfocará en un modelo híbrido:

- **Registro manual** de procesos críticos y sus objetos relevantes.
- **Descubrimiento asistido** de SQL Agent Jobs y objetos de esquema.
- **Asociación manual** entre jobs descubiertos y procesos registrados.
- **Motor de reglas explícito** basado en reglas configurables, sin IA/ML.
- **Recomendaciones** de horario basadas en calendario, objetos compartidos e intensidad.

El MVP responde a la pregunta: **"¿Es seguro ejecutar este proceso ahora?"**

## Consecuencias

### Positivas

- Time-to-value más rápido.
- Menor riesgo técnico.
- Resultados explicables y configurables.
- Base sólida para agregar ML/IA en el futuro.

### Negativas

- Requiere esfuerzo manual del usuario para registrar procesos.
- El valor depende de la calidad del catálogo.
- Puede verse menos "automágico" que soluciones con IA.

## Alternativas consideradas

- **Descubrimiento totalmente automático desde el inicio**: más ambicioso, pero riesgo alto de resultados imprecisos.
- **Solo registro manual sin descubrimiento**: más simple, pero menos atractivo para usuarios con muchos jobs existentes.

## Notas

Se documentará el nivel de confianza de cada recomendación para ser transparente sobre qué depende del catálogo manual y qué proviene del descubrimiento.
