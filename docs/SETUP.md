:# Guía de instalación local de DbTraffic

## Requisitos previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) o superior.
- [SQL Server](https://www.microsoft.com/es-es/sql-server/sql-server-downloads) en cualquiera de estas modalidades:
  - SQL Server Developer Edition
  - SQL Server Express
  - SQL Server LocalDB
  - SQL Server en Docker
- [Git](https://git-scm.com/)
- (Opcional) [Visual Studio 2022](https://visualstudio.microsoft.com/es/) o [Visual Studio Code](https://code.visualstudio.com/)

## 1. Clonar el repositorio

```bash
git clone https://github.com/tu-organizacion/dbtraffic.git
cd dbtraffic
```

## 2. Crear la base de datos del producto

1. Conecta a tu instancia de SQL Server con SQL Server Management Studio (SSMS), Azure Data Studio o `sqlcmd`.

2. Ejecuta el script de esquema:

   ```bash
   sqlcmd -S .\SQLEXPRESS -i database/schema.sql
   ```

   O ejecuta el archivo `database/schema.sql` desde tu herramienta de administración.

3. (Opcional) Ejecuta el script de datos de prueba:

   ```bash
   sqlcmd -S .\SQLEXPRESS -i database/seed.sql
   ```

## 3. Configurar la cadena de conexión

Edita el archivo `src/DbTraffic.Web/appsettings.Development.json` y ajusta la cadena de conexión a tu instancia local:

```json
{
  "DbTraffic": {
    "DemoInstance": {
      "Name": "Mi Instancia Local",
      "ConnectionString": "Server=.\\SQLEXPRESS;Database=master;Trusted_Connection=True;TrustServerCertificate=True;"
    }
  }
}
```

> **Nota de seguridad**: en entornos de producción, las cadenas de conexión deben almacenarse en secretos (Azure Key Vault, AWS Secrets Manager, variables de entorno, etc.) y nunca en el código fuente.

## 4. Restaurar dependencias

```bash
dotnet restore
```

## 5. Compilar la solución

```bash
dotnet build
```

## 6. Ejecutar pruebas

```bash
dotnet test
```

## 7. Ejecutar la aplicación web

```bash
dotnet run --project src/DbTraffic.Web
```

La aplicación se iniciará en `https://localhost:7105` y `http://localhost:5200` (los puertos pueden variar según `launchSettings.json`).

## 8. Verificar conectividad con SQL Server

Una vez que la aplicación esté en ejecución, abre un navegador o usa curl para consultar el endpoint de prueba:

```bash
curl http://localhost:5200/api/health/sql
```

Si todo está configurado correctamente, recibirás una respuesta similar a:

```json
{
  "connected": true,
  "activeRequestCount": 10,
  "requests": [
    {
      "sessionId": 50,
      "requestId": 0,
      "status": "sleeping",
      "command": "AWAITING COMMAND",
      "sqlText": null,
      "startTime": null,
      "databaseName": "master",
      "loginName": "sa",
      "programName": "Microsoft SQL Server Management Studio"
    }
  ]
}
```

## Estructura de la base de datos del producto

La base de datos `DbTraffic` contiene los siguientes esquemas:

- `catalog`: procesos, objetos, instancias y jobs descubiertos.
- `history`: ejecuciones y conflictos detectados.
- `rules`: definiciones de reglas y evaluaciones.
- `metrics`: snapshots de métricas de instancias.

## Solución de problemas

### Error de certificado SSL

Si recibes un error de certificado al conectarte a SQL Server, asegúrate de incluir `TrustServerCertificate=True;` en la cadena de conexión para desarrollo local. En producción, configura un certificado válido.

### No se encuentra la instancia SQL Server

Verifica que el servicio de SQL Server esté en ejecución:

```powershell
Get-Service | Where-Object { $_.Name -like '*SQL*' }
```

Si usas SQL Server Express, el nombre de servidor suele ser `.\SQLEXPRESS`. Si usas la instancia predeterminada, usa `.` o `(local)`.

### Error de permisos

El usuario de Windows o la cuenta de servicio debe tener permisos de lectura sobre:

- `master` (para DMV)
- `msdb` (para SQL Agent Jobs)
- La base de datos del producto `DbTraffic`

## Siguientes pasos

Una vez que la instalación local funcione, puedes continuar con el desarrollo según el [backlog](BACKLOG.md) y el [roadmap](ROADMAP.md).
