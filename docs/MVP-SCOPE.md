# Alcance del MVP de DbTraffic

## Propósito del MVP

El MVP debe responder a una pregunta concreta para el usuario técnico:

> **"¿Es seguro ejecutar este proceso ahora?"**

A partir de esa pregunta, el producto debe detectar conflictos potenciales, calcular un nivel de riesgo y recomendar ventanas de ejecución más seguras.

## Funcionalidades incluidas

### 1. Registro y catálogo de procesos

- CRUD de procesos manuales.
- Campos: nombre, tipo, frecuencia, duración estimada, ventana preferida, intensidad (CPU/IO/memoria).
- Asociación de objetos SQL relevantes (tablas, vistas, stored procedures) declarados por el usuario.
- Registro de instancias SQL Server objetivo.

### 2. Descubrimiento asistido

- Lectura de SQL Agent Jobs desde `msdb`.
- Lectura de objetos de esquema (`sys.objects`, `sys.tables`, `sys.views`, `sys.procedures`).
- Asociación manual entre jobs descubiertos y procesos registrados.
- No se intenta inferir automáticamente todas las dependencias.

### 3. Mapa de relaciones

- Grafo simple: procesos → objetos → procesos.
- Visualización básica en la UI web.
- Listado de procesos que comparten objetos.

### 4. Detección de conflictos por reglas

- Regla de solapamiento de objetos críticos.
- Regla de alta intensidad concurrente.
- Regla de duración estimada que excede ventana disponible.
- Regla de presión actual de recursos en la instancia.

### 5. Cálculo de nivel de riesgo

- Score numérico basado en fórmula ponderada explícita.
- Categorías: Bajo, Medio, Alto, Crítico.
- Explicación de cada hallazgo que contribuye al score.

### 6. Recomendación de horarios

- Búsqueda de ventanas futuras con riesgo bajo.
- Consideración de procesos registrados y duraciones estimadas.
- Simulación de "¿qué pasaría si ejecuto ahora?".

### 7. Monitoreo de actividad

- Lectura periódica de `sys.dm_exec_requests` y `sys.dm_tran_locks`.
- Dashboard de procesos en ejecución.
- Estado actual de la instancia objetivo.

### 8. Historial

- Registro manual de ejecuciones.
- Importación de historial desde `msdb.dbo.sysjobhistory`.
- Calibración de duraciones estimadas con base en historial.

### 9. UI web técnica

- Dashboard con semáforo de riesgo.
- Catálogo de procesos.
- Formulario de análisis de riesgo.
- Vista de recomendaciones.
- Vista de historial.

## Qué NO entra en el MVP

- Inteligencia artificial o machine learning predictivo.
- Orquestación automática (ejecutar, detener o reprogramar jobs).
- Descubrimiento automático completo de dependencias.
- Soporte multi-tenant.
- Soporte para motores de base de datos distintos a SQL Server.
- Notificaciones por email, Teams u otros canales.
- API pública completa (solo endpoints internos de la UI).
- Autenticación avanzada (MVP: básica o Windows Auth).

## Criterios de éxito del MVP

1. Un usuario puede registrar un proceso y sus objetos en menos de 2 minutos.
2. El sistema detecta un conflicto obvio entre dos procesos que comparten un objeto crítico en el mismo horario.
3. El score de riesgo es explicable (el usuario puede ver por qué es alto).
4. El sistema sugiere al menos 3 ventanas futuras de ejecución.
5. El dashboard muestra el estado actual de la instancia objetivo.
6. El historial permite calibrar la duración estimada de un proceso.
7. La aplicación se ejecuta localmente con SQL Server LocalDB.

## Métricas de seguimiento

- Número de procesos registrados.
- Número de conflictos detectados.
- Precisión de duraciones estimadas vs. reales (MAPE).
- Tiempo promedio para registrar un proceso.
- Cobertura de tests automatizados.
