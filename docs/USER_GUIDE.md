# Guía de usuario - DbTraffic v0.1.0

## Propósito

DbTraffic responde a una pregunta central: **"¿Es seguro ejecutar este proceso ahora?"**

El sistema conoce los procesos que corren en tus instancias SQL Server, los objetos que tocan, sus horarios y el estado actual de la instancia. Con esa información calcula un **score de riesgo** y sugiere **ventanas de ejecución** más seguras.

> **Alcance del MVP v0.1.0**: DbTraffic lee información de las instancias en modo solo lectura (DMV, catálogos del sistema, `msdb`). No ejecuta, detiene ni modifica procesos automáticamente.

---

## Flujo de trabajo típico

### 1. Registrar la instancia SQL Server

Ve a **Instancias** (`/instances`) y crea un registro con:

- **Nombre**: un alias descriptivo.
- **Cadena de conexión**: connection string de solo lectura hacia la instancia.
- **Descripción** (opcional).

La cuenta debe tener permisos de lectura sobre `master`, `msdb` y la base de datos objetivo.

### 2. Registrar el proceso

Ve a **Procesos** (`/processes`) y crea un proceso indicando:

- **Nombre** y **tipo** (SQL Agent Job, ETL, Stored Procedure, etc.).
- **Instancia** a la que pertenece.
- **Duración estimada** en minutos.
- **Intensidad** de CPU, IO y memoria.

> En v0.1.0 la interfaz web captura solo estos campos. La **ventana preferida**, los **horarios** y los **objetos críticos** deben cargarse directamente en la base de datos del producto o mediante la API (ver paso 3).

### 3. Definir objetos críticos, horarios y ventana preferida

En v0.1.0 la UI no incluye un formulario para estos datos. Cárgalos directamente en las tablas del catálogo de DbTraffic:

| Tabla | Qué contiene |
|-------|--------------|
| `catalog.ProcessObjects` | Objetos de base de datos que usa el proceso. Marca `IsCritical = 1` para los conflictivos. |
| `catalog.ProcessSchedules` | Horarios habituales de ejecución del proceso. |
| `catalog.Processes` | Campos `PreferredWindowStart` y `PreferredWindowEnd` (tipo `time`). |

Ejemplo para insertar un objeto crítico:

```sql
INSERT INTO catalog.ProcessObjects (Id, ProcessId, SchemaName, ObjectName, ObjectType, IsCritical)
VALUES (NEWID(), '<ProcessId>', 'dbo', 'Orders', 'Table', 1);
```

Estos datos son los que activan las reglas `ObjectOverlapRule`, `EstimatedDurationExceedsWindowRule` y la detección de solapamientos.

### 4. Descubrir jobs y objetos (opcional pero recomendado)

Ve a **Descubrimiento** (`/discovery`) y ejecuta el escáner:

- Lee los SQL Agent Jobs de `msdb.dbo.sysjobs`.
- Lee los objetos del esquema desde `sys.objects`.
- Permite asociar un job descubierto a un proceso existente.

### 5. Analizar riesgo

Ve a **Análisis de riesgo** (`/risk`):

1. Selecciona el proceso.
2. Selecciona la fecha y hora propuesta (por defecto "ahora").
3. Presiona **Analizar riesgo**.

El sistema muestra:

- **Nivel de riesgo**: None, Low, Medium, High o Critical.
- **Score total**: un número entre 0 y 100.
- **Hallazgos**: cada regla que detectó un problema, con su score parcial y detalles.

#### Reglas que intervienen

| Regla | Qué detecta |
|-------|-------------|
| Object Overlap | Otro proceso solapado usa los mismos objetos críticos. |
| High Intensity Overlap | Otro proceso de alta intensidad de CPU/IO/memoria se solapa. |
| Estimated Duration Exceeds Window | La duración estimada no cabe dentro de la ventana preferida. |
| Instance Resource Pressure | La instancia tiene bloqueos activos, alta CPU o alta memoria. |

### 6. Buscar recomendaciones

Ve a **Recomendaciones** (`/recommendations`):

1. Selecciona el proceso.
2. Define el rango de búsqueda y la granularidad (por ejemplo, cada 30 minutos).
3. Presiona **Buscar recomendaciones**.

El sistema evalúa cada ventana candidata y devuelve las de menor riesgo ordenadas por score.

Usa **Simular "ahora"** para ver el riesgo inmediato sin cambiar de pantalla.

---

## Interpretación del riesgo

| Nivel | Significado | Acción sugerida |
|-------|-------------|-----------------|
| **None** | No se detectaron riesgos. | Ejecutar con normalidad. |
| **Low** | Riesgo bajo, factores menores. | Ejecutar si no hay política restrictiva. |
| **Medium** | Hay factores a considerar. | Evaluar los hallazgos antes de ejecutar. |
| **High** | Conflictos significativos. | Posponer o ejecutar solo si es crítico. |
| **Critical** | Conflicto grave. | No ejecutar en este horario. |

---

## Monitoreo e historial

- **Monitoreo** (`/monitoring`): muestra snapshots recientes de la instancia (requests activos, bloqueos, CPU, memoria).
- **Historial** (`/history`): registra ejecuciones manuales e importa historial desde `msdb.dbo.sysjobhistory`. Permite calibrar la duración estimada de un proceso con base en ejecuciones pasadas.

---

## Limitaciones conocidas de v0.1.0

- **UI de administración de procesos**: la pantalla `/processes` permite crear y eliminar procesos, pero **no permite editar objetos críticos, horarios ni ventana preferida**. Estos datos deben cargarse directamente en la base de datos del producto o mediante la API.
- **CPU y memoria**: DbTraffic intenta leer estos valores desde DMVs del sistema (`sys.dm_os_ring_buffers` y `sys.dm_os_process_memory`). La precisión depende de permisos, edición de SQL Server y configuración del sistema operativo. En algunos entornos estos valores pueden ser 0 o aproximados.
- **Bloqueos**: se leen correctamente desde `sys.dm_os_waiting_tasks` / `sys.dm_exec_requests`.
- **No hay acción automática**: DbTraffic solo recomienda; no inicia, detiene ni cambia jobs.

---

## Siguientes pasos

Consulta la [guía de operaciones](OPERATIONS_GUIDE.md) para desplegar y mantener DbTraffic, y las [notas de release](RELEASE_NOTES.md) para conocer el alcance completo de la versión.
