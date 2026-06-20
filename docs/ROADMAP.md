# Roadmap de DbTraffic

## Q1: MVP Interno

**Objetivo**: Tener un producto funcional en ambiente controlado que demuestre el valor de prevención de conflictos.

**Entregables**:
- Fases 0 a 6 completadas.
- Aplicación web ejecutándose localmente.
- Motor de reglas con al menos 4 reglas.
- UI de catálogo, riesgo, recomendaciones e historial.
- Suite de pruebas unitarias e integración.
- Documentación técnica y de usuario.

**Hitos**:
- Semana 2: Estructura y conexión a SQL Server lista.
- Semana 5: Catálogo de procesos funcional.
- Semana 8: Descubrimiento asistido funcional.
- Semana 12: Motor de reglas y recomendaciones.
- Semana 16: MVP cerrado y demo interno.

## Q2: Piloto con clientes

**Objetivo**: Validar el producto en 1-2 entornos reales, recolectar feedback y ajustar.

**Entregables**:
- Instalación en entornos de cliente (on-premise o híbrido).
- Importación de datos reales de jobs y objetos.
- Calibración de reglas con datos reales.
- Primeras versiones de API interna.
- Guía de despliegue para producción.

**Hitos**:
- Mes 1: Primer cliente piloto identificado.
- Mes 2: Producto desplegado en ambiente piloto.
- Mes 3: Feedback recopilado y plan de ajustes definido.

## Q3: MVP Comercial

**Objetivo**: Tener una versión comercializable con funcionalidades esenciales para venta.

**Entregables**:
- Soporte multi-instancia.
- Mejoras en descubrimiento automático.
- Notificaciones básicas (email).
- Panel de administración.
- Modelo de licenciamiento definido.
- SLA de soporte.

**Hitos**:
- Mes 1: Definición de modelo de precios.
- Mes 2: Funcionalidades comerciales implementadas.
- Mes 3: Lanzamiento beta comercial.

## Q4+: Escalabilidad y características avanzadas

**Objetivo**: Escalar el producto y agregar diferenciadores avanzados.

**Entregables potenciales**:
- Motor predictivo basado en estadísticas de historial.
- Soporte para Azure SQL, AWS RDS.
- API pública documentada.
- Blazor WASM o SPA para mayor escalabilidad frontend.
- Marketplace de reglas.
- Integraciones con orquestadores (Airflow, Azure Data Factory).

**Hitos**:
- Año 2 Q1: Motor predictivo experimental.
- Año 2 Q2: API pública y marketplace de reglas.
- Año 2 Q3-Q4: Expansión de motores de BD soportados.

## Dependencias críticas

- Disponibilidad de ambiente SQL Server representativo.
- Feedback de clientes piloto.
- Definición de modelo de licenciamiento.
- Recursos de desarrollo y operación.
