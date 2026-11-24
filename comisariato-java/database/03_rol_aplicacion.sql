-- Ejecutar en la base Comisariato. Asignar luego el usuario SQL a este rol.
-- No crea logins ni almacena contrasenas. No concede INSERT/UPDATE/DELETE directos.
IF DATABASE_PRINCIPAL_ID(N'comisariato_app') IS NULL
    CREATE ROLE comisariato_app;
GO
GRANT EXECUTE ON dbo.usp_RegistrarVenta TO comisariato_app;
GRANT EXECUTE ON dbo.usp_AnularVenta TO comisariato_app;
GRANT SELECT ON dbo.vw_ProductosStockBajo TO comisariato_app;
GRANT SELECT ON dbo.fn_VentasPorEmpleado TO comisariato_app;
GRANT SELECT ON dbo.Empleados TO comisariato_app;
GRANT SELECT ON dbo.Productos TO comisariato_app;
GRANT SELECT ON dbo.MovimientosInventario TO comisariato_app;
GO
-- Ejemplo, despues de crear el login y el usuario mediante SSMS:
-- ALTER ROLE comisariato_app ADD MEMBER [usuario_comisariato];
