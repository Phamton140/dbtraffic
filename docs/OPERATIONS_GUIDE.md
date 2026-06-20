# Guía de operaciones - DbTraffic v0.1.0

## Requisitos de infraestructura

- **.NET 8 SDK** o superior.
- **SQL Server** para la base de datos del producto (Developer, Express, LocalDB o Docker).
- **SQL Server objetivo** (instancia a monitorear) con permisos de solo lectura.
- **Git** (para clonado y despliegue).
- **Docker** (solo si se ejecutan tests de integración y E2E con Testcontainers).

---

## Variables de configuración

La aplicación lee configuración desde `appsettings.json`, `appsettings.Development.json`, variables de entorno o secretos.

```json
{
  "DbTraffic": {
    "DemoInstance": {
      "Name": "Producción",
      "ConnectionString": "Server=...;Database=master;..."
    },
    "Discovery": {
      "IntervalMinutes": 60
    },
    "Monitoring": {
      "IntervalMinutes": 5,
      "RetentionDays": 7
    }
  }
}
```

| Sección | Propósito |
|---------|-----------|
| `DbTraffic:DemoInstance:ConnectionString` | Instancia SQL Server objetivo que se lee para DMVs, jobs y objetos. |
| `DbTraffic:Discovery:IntervalMinutes` | Frecuencia del worker de descubrimiento. |
| `DbTraffic:Monitoring:IntervalMinutes` | Frecuencia del worker de monitoreo. |
| `DbTraffic:Monitoring:RetentionDays` | Retención de snapshots antiguos. |

> **Seguridad**: en producción usa secretos (Azure Key Vault, AWS Secrets Manager, variables de entorno). Nunca versiones cadenas de conexión con credenciales.

---

## Permisos necesarios en SQL Server

La cuenta de conexión a la instancia objetivo debe poder leer:

- `master`: DMVs (`sys.dm_exec_requests`, `sys.dm_os_waiting_tasks`, `sys.dm_os_ring_buffers`, `sys.dm_os_process_memory`, etc.).
- `msdb`: SQL Agent Jobs (`msdb.dbo.sysjobs`, `msdb.dbo.sysjobhistory`).
- Base de datos objetivo: catálogo de objetos (`sys.objects`).

Rol recomendado: `db_datareader` en `msdb` y la base de datos objetivo, más permisos de lectura de DMVs (generalmente concedidos a `public` o con `VIEW SERVER STATE`).

---

## Despliegue

### 1. Clonar y compilar

```bash
git clone https://github.com/tu-organizacion/dbtraffic.git
cd dbtraffic
dotnet restore
dotnet build --configuration Release
```

### 2. Crear la base de datos del producto

Ejecuta `database/schema.sql` en la instancia SQL Server que almacenará el catálogo, historial y métricas de DbTraffic.

```bash
sqlcmd -S <servidor-producto> -i database/schema.sql
```

### 3. Configurar la aplicación

Edita `src/DbTraffic.Web/appsettings.Production.json` o usa variables de entorno:

```bash
export DbTraffic__DemoInstance__ConnectionString="Server=..."
```

### 4. Ejecutar

```bash
dotnet run --project src/DbTraffic.Web --configuration Release
```

O publicar y desplegar:

```bash
dotnet publish src/DbTraffic.Web --configuration Release --output ./publish
```

---

## Workers de background

DbTraffic ejecuta dos servicios en segundo plano:

| Worker | Frecuencia | Función |
|--------|------------|---------|
| `DiscoveryWorker` | `DbTraffic:Discovery:IntervalMinutes` | Descubre SQL Agent Jobs y objetos del esquema. |
| `MonitoringWorker` | `DbTraffic:Monitoring:IntervalMinutes` | Captura snapshots de métricas de la instancia. |

Si la conexión a SQL Server falla, los workers registran el error y reintentan en el siguiente ciclo.

---

## Health checks

El endpoint `/api/health/sql` verifica:

- Conectividad con la instancia SQL Server objetivo.
- Cantidad de requests activos.

Ejemplo:

```bash
curl http://localhost:5000/api/health/sql
```

---

## Ejecución de pruebas

### Pruebas unitarias y de integración

```bash
dotnet test
```

### Tests que requieren Docker (Testcontainers)

```bash
export RUN_INTEGRATION_TESTS=true
export RUN_E2E_TESTS=true
dotnet test
```

> Los tests E2E requieren que los navegadores de Playwright estén instalados. En CI se instalan automáticamente; localmente usa `dotnet tool install --global Microsoft.Playwright.CLI && playwright install chromium`.

---

## Pipeline de CI/CD

El workflow `.github/workflows/ci.yml`:

1. Restaura dependencias.
2. Compila en Release.
3. Instala navegadores de Playwright.
4. Ejecuta todos los tests con `RUN_INTEGRATION_TESTS=true` y `RUN_E2E_TESTS=true`.
5. Verifica formato con `dotnet format`.

---

## Monitoreo del sistema

- Revisa logs en la salida estándar o en el proveedor de logs configurado.
- Consulta `/api/monitoring` para snapshots recientes.
- Revisa `/history` para ejecuciones registradas.

---

## Backup y mantenimiento

- Realiza backup periódico de la base de datos del producto (`DbTraffic`).
- Los snapshots antiguos se purgan automáticamente según `RetentionDays`.
- El historial de ejecuciones se mantiene indefinidamente salvo que se implemente una política de retención adicional.

---

## Limitaciones operativas

- **CPU y memoria**: los valores provienen de DMVs del sistema. Pueden requerir permisos elevados o no estar disponibles en todas las ediciones de SQL Server. Consulta la [guía de usuario](USER_GUIDE.md) para más detalles.
- **Alta disponibilidad**: en v0.1.0 la aplicación es una única instancia Blazor Server. Para escalar horizontalmente se requiere refactorizar workers y estado.
