-- DbTraffic - Datos de prueba iniciales
-- Ejecutar después de schema.sql

USE DbTraffic;
GO

-- Instancia de prueba (LocalDB)
DECLARE @instanceId UNIQUEIDENTIFIER = NEWID();

INSERT INTO catalog.Instances (Id, Name, ConnectionString, Description)
VALUES (
    @instanceId,
    N'LocalDB Demo',
    N'Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;',
    N'Instancia de demostración para desarrollo local'
);

-- Reglas iniciales
INSERT INTO rules.RuleDefinitions (Id, Name, Description, RuleType, DefaultWeight)
VALUES
    (NEWID(), N'Object Overlap', N'Detecta procesos que comparten objetos críticos en el mismo horario.', N'DbTraffic.Core.Rules.ObjectOverlapRule', 1.0),
    (NEWID(), N'High Intensity Overlap', N'Detecta procesos de alta intensidad ejecutándose simultáneamente.', N'DbTraffic.Core.Rules.HighIntensityOverlapRule', 0.8),
    (NEWID(), N'Estimated Duration Exceeds Window', N'Detecta cuando la duración estimada excede la ventana disponible.', N'DbTraffic.Core.Rules.EstimatedDurationExceedsWindowRule', 0.6),
    (NEWID(), N'Instance Resource Pressure', N'Detecta presión actual de recursos en la instancia.', N'DbTraffic.Core.Rules.InstanceResourcePressureRule', 0.7);
GO
