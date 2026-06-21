# Flujo de trabajo con DbTraffic

Este documento explica paso a paso cómo usar DbTraffic desde cero, incluso si nunca has trabajado con la herramienta. No asume conocimientos técnicos avanzados.

---

## 1. ¿Qué es DbTraffic?

DbTraffic es un **controlador de tráfico para bases de datos SQL Server**.

Imagina que en tu empresa hay varios procesos que corren sobre la misma base de datos: backups, mantenimiento de índices, cargas de datos (ETL), reportes, etc. Si dos procesos pesados corren al mismo tiempo, pueden:

- Bloquearse entre sí.
- Lentificar la base de datos.
- Provocar errores o timeouts.

DbTraffic te ayuda a **evitar esos conflictos**:

1. Registras los procesos que existen.
2. Le dices cuándo quieres ejecutar uno.
3. DbTraffic analiza si hay riesgo de colisión.
4. Te recomienda mejores horarios si es necesario.

---

## 2. Conceptos clave

| Concepto | Significado | Ejemplo |
|----------|-------------|---------|
| **Instancia** | Un servidor SQL Server que quieres monitorear. | `DESKTOP-ABC\SQLEXPRESS` |
| **Proceso** | Cualquier tarea que corre en la base de datos. | Backup nocturno, rebuild de índices, ETL |
| **Objeto** | Tablas, vistas, stored procedures, etc., que un proceso usa. | `dbo.Clientes`, `dbo.Facturas` |
| **Ventana** | El rango de tiempo en el que planeas ejecutar un proceso. | Sábado 02:00 - 04:00 |
| **Riesgo** | Probabilidad de que el proceso choque con otros o con la carga del servidor. | Bajo, Medio, Alto, Crítico |
| **Snapshot** | Una foto de la salud del servidor en un momento dado. | CPU 30%, memoria 50%, 2 bloqueos |
| **Ejecución** | Registro de que un proceso corrió, cuándo y cuánto duró. | Backup completado en 45 minutos |

---

## 3. Antes de empezar

### Requisitos

- Tener instalado [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
- Tener acceso a un SQL Server (puede ser LocalDB, SQL Express, Developer Edition o Docker).
- Tener creada la base de datos del producto ejecutando `database/schema.sql`.

### Arrancar la aplicación

Desde la carpeta raíz del proyecto ejecuta:

```powershell
dotnet run --project src/DbTraffic.Web/DbTraffic.Web.csproj
```

Cuando veas un mensaje como:

```
Now listening on: http://localhost:5000
```

Abre tu navegador en `http://localhost:5000`.

> **Nota:** Si la app ya está corriendo y haces cambios, deténla primero con `Ctrl+C` o con:
> ```powershell
> Stop-Process -Name DbTraffic.Web -Force -ErrorAction SilentlyContinue
> ```

---

## 4. Flujo paso a paso

### Paso 1: Registrar una instancia SQL Server

1. En el menú lateral, haz clic en **Instancias**.
2. Completa el formulario de la derecha:
   - **Nombre**: un nombre descriptivo, por ejemplo `Producción`.
   - **Cadena de conexión**: la cadena para conectarte a SQL Server. Ejemplo:
     ```
     Server=localhost\SQLEXPRESS;Database=DbTraffic;Trusted_Connection=True;TrustServerCertificate=True;
     ```
   - **Descripción**: opcional, para recordar para qué sirve esa instancia.
3. Haz clic en **Crear**.
4. En la topbar debería aparecer un chip blanco que dice **SQL: OK**. Eso significa que la app pudo conectarse.

> Si dice **SQL: Offline** o **SQL: Unknown**, revisa que SQL Server esté encendido y que la cadena de conexión sea correcta.

---

### Paso 2: Registrar un proceso

1. Ve al menú **Procesos**.
2. Completa el formulario:
   - **Nombre**: por ejemplo `Backup BD_Produccion`.
   - **Instancia**: selecciona la instancia que registraste.
   - **Tipo**: `Backup`, `Maintenance`, `ETL`, etc.
   - **Duración estimada**: cuántos minutos crees que durará. Ejemplo: `60`.
   - **Intensidad CPU / IO / Memoria**: qué tan pesado es el proceso.
     - **Low** (verde): proceso ligero.
     - **Medium** (amarillo): proceso moderado.
     - **High** (rojo): proceso pesado.
     - **Critical** (gris oscuro): proceso muy crítico o pesado.
3. Haz clic en **Crear**.

Repite este paso para todos los procesos que quieras controlar. Por ejemplo:

| Proceso | Tipo | Duración | CPU | IO |
|---------|------|----------|-----|----|
| Backup BD_Produccion | Backup | 60 min | Low | Medium |
| Rebuild índices BD_Produccion | Maintenance | 90 min | Medium | High |
| Carga diaria de ventas | ETL | 30 min | High | High |

---

### Paso 3: Descubrir objetos y jobs automáticamente

DbTraffic puede leer de SQL Server los **SQL Agent Jobs** y los **objetos de la base de datos** (tablas, vistas, stored procedures) para ayudarte a asociarlos a tus procesos.

1. Ve a **Descubrimiento**.
2. Selecciona la instancia.
3. Haz clic en **Ejecutar descubrimiento**.
4. Espera unos segundos y luego haz clic en **Ver resultados**.
5. Revisa las pestañas:
   - **Jobs descubiertos**: tareas programadas en SQL Agent.
   - **Objetos descubiertos**: tablas, vistas, etc.

---

### Paso 4: Asociar un job a un proceso

1. En la pestaña **Jobs descubiertos**, busca el job que corresponde a tu proceso.
2. En la columna **Asociar**, selecciona el proceso del menú desplegable.
3. El job ahora queda vinculado a ese proceso.

Esto sirve para que, al analizar riesgo, DbTraffic sepa qué objetos usa el proceso.

---

### Paso 5: Analizar el riesgo de ejecutar un proceso

Supón que quieres saber si puedes ejecutar el `Rebuild índices BD_Produccion` el sábado a las 03:30.

1. Ve al menú **Riesgo**.
2. Selecciona el proceso.
3. Selecciona la fecha y hora propuesta.
4. Haz clic en **Analizar riesgo**.

Verás:

- Un **gauge circular grande** con el score numérico.
- Un **chip de color** con el nivel de riesgo:
  - 🟢 **None / Low**: seguro ejecutar.
  - 🟡 **Medium**: precaución.
  - 🔴 **High / Critical**: riesgo alto, mejor buscar otra ventana.
- Una lista de **hallazgos** con las reglas que detectaron problemas.

#### Ejemplo práctico

Si tienes:

- `Backup BD_Produccion` programado sábado 02:00 - 04:00.
- `Rebuild índices BD_Produccion` sábado 03:30 - 05:00.

Ambos tocan la misma base de datos, así que DbTraffic marcará **Alto riesgo** porque se solapan.

Si cambias el rebuild a las 05:30, el riesgo bajará porque ya no se solapan.

---

### Paso 6: Buscar mejores ventanas de ejecución

1. Ve al menú **Recomendaciones**.
2. Selecciona el proceso.
3. Elige un rango de fechas, por ejemplo del próximo lunes al domingo.
4. Elige la **granularidad**, por ejemplo 30 minutos.
5. Haz clic en **Buscar recomendaciones**.

DbTraffic evaluará todas las ventanas posibles y te mostrará una tabla ordenada de menor a mayor riesgo. Las mejores ventanas aparecerán primero.

También puedes hacer clic en **Simular "ahora"** para ver qué pasaría si ejecutaras el proceso inmediatamente.

---

### Paso 7: Monitorear la instancia en tiempo real

1. Ve a **Monitoreo**.
2. Selecciona la instancia.
3. Haz clic en **Actualizar**.

Verás tarjetas con:

- **CPU**: porcentaje de uso.
- **Memoria**: porcentaje de uso.
- **Requests activos**: consultas corriendo ahora mismo.
- **Bloqueos**: sesiones bloqueadas.

Puedes hacer clic en **Capturar snapshot** para guardar el estado actual en el historial.

> Si alguna métrica muestra 0, puede ser por permisos de SQL Server. La app continúa funcionando de todos modos.

---

### Paso 8: Registrar ejecuciones en el historial

Cada vez que un proceso corra, deberías registrarlo para que DbTraffic aprenda de la realidad.

1. Ve a **Historial**.
2. En el panel derecho, completa:
   - **Instancia**.
   - **Proceso**.
   - **Fecha y hora de inicio**.
   - **Fecha y hora de fin**.
   - **Estado**: Completed, Failed, Cancelled, Running.
3. Haz clic en **Registrar**.

También puedes:

- **Importar desde SQL Agent**: trae el historial de jobs de los últimos 30 días.
- **Calibrar duración**: ajusta automáticamente la duración estimada de un proceso basándose en ejecuciones anteriores.

---

## 5. Escenario de prueba completo

Sigue este escenario para probar que todo funciona sin arriesgar tu base de datos real.

### Datos de prueba

1. Crea una instancia llamada `Test` apuntando a tu SQL Server local.
2. Crea dos procesos:
   - **Proceso A**: `Backup de prueba`, duración 60 min, intensidad CPU Low, IO Medium.
   - **Proceso B**: `Rebuild de prueba`, duración 90 min, intensidad CPU Medium, IO High.
3. A ambos procesos, asígnales la misma instancia `Test`.

### Prueba de riesgo

1. Ve a **Riesgo**.
2. Selecciona **Proceso B**.
3. Simula ejecución el sábado a las 03:30.
4. Debería dar riesgo bajo porque no hay otros procesos.

### Prueba de colisión

1. Edita el **Proceso A** para que su ventana sea sábado 02:00 - 04:00.
2. Edita el **Proceso B** para que su ventana sea sábado 03:00 - 05:00.
3. Ve a **Riesgo**, selecciona **Proceso B**, simula a las 03:30.
4. Ahora debería dar riesgo alto porque ambos procesos se solapan.

### Prueba de recomendaciones

1. Ve a **Recomendaciones**.
2. Selecciona **Proceso B**.
3. Busca en el próximo fin de semana con granularidad de 30 min.
4. Debería sugerir ventanas fuera del horario del Proceso A.

---

## 6. Interpretación de colores

### Niveles de riesgo

| Color | Nivel | Significado | Acción recomendada |
|-------|-------|-------------|--------------------|
| 🟢 Verde | None / Low | Sin riesgo o riesgo mínimo. | Ejecutar con confianza. |
| 🟡 Amarillo | Medium | Hay algo de solapamiento o carga. | Evaluar si es aceptable. |
| 🔴 Rojo | High | Solapamiento importante o alta carga. | Buscar otra ventana. |
| ⚫ Negro | Critical | Múltiples problemas graves. | No ejecutar ahí. |

### Intensidad de procesos

| Color | Nivel | Significado |
|-------|-------|-------------|
| 🟢 Verde | Low | Proceso ligero. |
| 🟡 Amarillo | Medium | Proceso moderado. |
| 🔴 Rojo | High | Proceso pesado. |
| ⚫ Negro | Critical | Proceso muy pesado o crítico. |

---

## 7. Cómo interpretar el dashboard

La página de inicio muestra:

- **Instancias**: cuántos servidores tienes registrados.
- **Procesos**: cuántos procesos tienes catalogados.
- **Ejecuciones**: cuántas ejecuciones has registrado.
- **Tasa de éxito**: porcentaje de ejecuciones completadas sin error.
- **Últimas ejecuciones**: tabla con las 5 ejecuciones más recientes.
- **Proceso más ejecutado**: cuál es el proceso que más veces ha corrido.

Este dashboard te da una visión rápida del estado general.

---

## 8. Solución de problemas comunes

### La app no arranca

- Revisa que el puerto 5000 no esté ocupado.
- Revisa que tengas .NET 8 SDK instalado.

### SQL: Offline

- Verifica que SQL Server esté corriendo.
- Verifica que la cadena de conexión sea correcta.
- Si usas SQL Express, incluye `\SQLEXPRESS` en el servidor.

### No veo métricas en Monitoreo

- Puede ser por permisos. La cuenta de SQL Server necesita acceso a DMVs como `sys.dm_os_ring_buffers`.
- La app mostrará 0 y continuará funcionando.

### Los tests E2E no corren localmente

- Necesitas Docker y la variable de entorno `RUN_E2E_TESTS=true`.
- Sin Docker, los tests E2E se saltan automáticamente.

### El dashboard muestra todo en cero

- Es normal si aún no has registrado instancias, procesos ni ejecuciones. Empieza por registrar al menos una instancia y un proceso.

---

## 9. Resumen del flujo diario

1. **Mañana**: revisa el **dashboard** para ver el estado general.
2. **Antes de ejecutar un proceso**: usa **Riesgo** para validar el horario.
3. **Si el riesgo es alto**: usa **Recomendaciones** para encontrar una mejor ventana.
4. **Durante la ejecución**: usa **Monitoreo** para ver la salud del servidor.
5. **Después de la ejecución**: registra la ejecución en **Historial**.
6. **Periódicamente**: usa **Calibrar duración** para que las estimaciones sean más precisas.

---

## 10. Enlaces útiles

- [Guía de instalación](SETUP.md)
- [Guía de usuario](USER_GUIDE.md)
- [Guía de operaciones](OPERATIONS_GUIDE.md)
- [Notas de release](RELEASE_NOTES.md)
- [Backlog](BACKLOG.md)
- [Changelog](CHANGELOG.md)
