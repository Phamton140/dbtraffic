# Guía de Demo Interno - DbTraffic

Escenario objetivo: mostrar en 15-20 minutos cómo DbTraffic ayuda a un DBA a evitar colisiones entre procesos de mantenimiento en SQL Server.

## Preparación

1. Clonar/actualizar el repositorio.
2. Asegurar que SQL Server local esté disponible y que la base de datos `DbTraffic` exista.
3. Ejecutar las migraciones/schema:
   ```powershell
   dotnet tool restore
   dotnet run --project src/DbTraffic.Web/DbTraffic.Web.csproj --migrate
   ```
4. Iniciar la aplicación:
   ```powershell
   dotnet run --project src/DbTraffic.Web/DbTraffic.Web.csproj
   ```
5. Abrir http://localhost:5000 y verificar que el estado de SQL en la topbar esté verde.

## Escenario de datos

Usar la instancia local (`DESKTOP-B505TS5\Default` o equivalente) y registrar dos procesos de mantenimiento:

- **Proceso A - Backup BD_Produccion**
  - Tipo: Backup
  - Duración estimada: 60 min
  - Intensidad: Media
  - Ventana propuesta: sábado 02:00 - 04:00
  - Objetos: `[BD_Produccion]`

- **Proceso B - Rebuild índices BD_Produccion**
  - Tipo: Maintenance
  - Duración estimada: 90 min
  - Intensidad: Alta
  - Ventana propuesta: sábado 03:00 - 05:00
  - Objetos: `[BD_Produccion]`

## Guion de demo

### 1. Catálogo (2 min)
- Ir a **Instancias** y mostrar la instancia registrada con estado de conexión.
- Ir a **Procesos** y crear los procesos A y B.
- Destacar validaciones de dominio: duración positiva, ventana consistente, instancia obligatoria.

### 2. Descubrimiento asistido (3 min)
- Ir a **Descubrimiento** y ejecutar un escaneo.
- Mostrar jobs y objetos descubiertos desde `msdb.dbo.sysjobs` y `sys.objects`.
- Asociar el job de backup al Proceso A.

### 3. Análisis de riesgo (5 min)
- Ir a **Riesgo**.
- Seleccionar el Proceso B y simular ejecución el sábado a las 03:30.
- Mostrar el score alto y las reglas activadas:
  - `ObjectOverlapRule`: ambos procesos tocan `[BD_Produccion]`.
  - `HighIntensityOverlapRule`: el Proceso B es de alta intensidad y se solapa.
- Cambiar la hora de simulación al sábado 05:30 y mostrar cómo baja el riesgo.

### 4. Recomendaciones (3 min)
- Ir a **Recomendaciones** para el Proceso B.
- Configurar rango: próximo fin de semana, granularidad 30 min.
- Mostrar las ventanas ordenadas de menor a mayor riesgo.
- Explicar que el algoritmo respeta solapamientos, intensidad y presión de recursos.

### 5. Monitoreo e historial (4 min)
- Ir a **Monitoreo** y mostrar el snapshot más reciente de la instancia.
- Ver CPU, memoria, sesiones bloqueadoras y requests activas.
- Ir a **Historial** y registrar una ejecución manual del Proceso A.
- Mostrar cómo el historial alimenta la calibración de duraciones.

### 6. Cierre (2 min)
- Resumir valor: menos colisiones, ventanas más seguras, decisiones basadas en datos.
- Mostrar documentación: `USER_GUIDE.md`, `OPERATIONS_GUIDE.md`, `README.md`.
- Abrir a preguntas.

## Tips

- Si una métrica DMV no retorna valores (por permisos), la app muestra 0 y continúa; explicar que es degradación elegante.
- Mantener el navegador en 1920x1080 para la mejor experiencia visual actual.
- Tener un SQL Server Management Studio abierto para validar datos en vivo si alguien lo solicita.
