-- Complemento del taller. Ejecutar DESPUES de 01_base_original.sql en Comisariato.
-- No elimina ni reinicia datos. SQL Server 2016 SP1+; compatibilidad >= 130.
SET NOCOUNT ON;
IF DB_NAME() = N'master' OR OBJECT_ID(N'dbo.Productos', N'U') IS NULL
    THROW 51000, 'Seleccione la base Comisariato ya instalada antes de ejecutar este script.', 1;
GO

IF OBJECT_ID(N'dbo.ConfiguracionFacturacion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConfiguracionFacturacion (
        ConfiguracionID INT NOT NULL CONSTRAINT PK_ConfiguracionFacturacion PRIMARY KEY,
        TasaIVA DECIMAL(5,4) NOT NULL,
        CONSTRAINT CK_Config_Unica CHECK (ConfiguracionID=1),
        CONSTRAINT CK_Config_Tasa CHECK (TasaIVA BETWEEN 0 AND 1)
    );
END;
IF NOT EXISTS (SELECT 1 FROM dbo.ConfiguracionFacturacion WHERE ConfiguracionID=1)
    INSERT dbo.ConfiguracionFacturacion VALUES (1, 0.1500);
-- 15% es una convencion academica configurable, no una determinacion tributaria.
GO

CREATE OR ALTER VIEW dbo.vw_ProductosStockBajo AS
    SELECT ProductoID, CodigoBarras, Nombre, PrecioVenta, Stock, StockMinimo
    FROM dbo.Productos
    WHERE Activo=1 AND Stock<=StockMinimo;
GO

CREATE OR ALTER FUNCTION dbo.fn_VentasPorEmpleado(@EmpleadoID INT)
RETURNS TABLE AS RETURN (
    SELECT VentaID, NumeroFactura, ClienteID, EmpleadoID, FechaVenta,
           Subtotal, IVA, Total, FormaPago, Estado
    FROM dbo.Ventas WHERE EmpleadoID=@EmpleadoID
);
GO

CREATE OR ALTER PROCEDURE dbo.usp_RegistrarVenta
    @NumeroFactura VARCHAR(20),
    @ClienteID INT = NULL,
    @EmpleadoID INT,
    @FormaPago NVARCHAR(30),
    @Detalles NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @Propia BIT = CASE WHEN @@TRANCOUNT=0 THEN 1 ELSE 0 END;
    BEGIN TRY
        IF @Propia=1 BEGIN TRANSACTION;
        IF @NumeroFactura IS NULL OR LEN(LTRIM(RTRIM(@NumeroFactura)))=0
           OR @NumeroFactura COLLATE Latin1_General_100_BIN2 LIKE '%[^A-Za-z0-9-]%'
           OR @EmpleadoID IS NULL OR @EmpleadoID<=0
           OR (@ClienteID IS NOT NULL AND @ClienteID<=0)
           OR @FormaPago IS NULL OR @FormaPago NOT IN (N'Efectivo',N'Tarjeta',N'Transferencia')
            THROW 51000, 'Campos obligatorios invalidos.', 1;

        IF ISJSON(@Detalles)<>1 OR @Detalles IS NULL OR LEFT(LTRIM(@Detalles),1)<>N'['
            THROW 51000, 'El detalle debe ser un array JSON.', 1;
        IF (SELECT COUNT(*) FROM OPENJSON(@Detalles)) NOT BETWEEN 1 AND 100
            THROW 51000, 'Debe enviar entre 1 y 100 productos.', 1;
        IF EXISTS(SELECT 1 FROM OPENJSON(@Detalles) WHERE [type]<>5)
            THROW 51000, 'Cada detalle debe ser un objeto JSON.', 1;

        DECLARE @Lineas TABLE (
            ProductoID INT NULL, Cantidad INT NULL, Descuento DECIMAL(18,4) NULL,
            Precio DECIMAL(10,2) NULL, StockAnterior INT NULL
        );
        INSERT @Lineas(ProductoID,Cantidad,Descuento)
        SELECT TRY_CONVERT(INT,JSON_VALUE([value],'$.productoId')),
               TRY_CONVERT(INT,JSON_VALUE([value],'$.cantidad')),
               TRY_CONVERT(DECIMAL(18,4),JSON_VALUE([value],'$.descuento'))
        FROM OPENJSON(@Detalles);
        IF EXISTS(SELECT 1 FROM @Lineas WHERE ProductoID IS NULL OR ProductoID<=0
                  OR Cantidad IS NULL OR Cantidad NOT BETWEEN 1 AND 1000000
                  OR Descuento IS NULL OR Descuento<0 OR Descuento>99999999.99
                  OR Descuento<>ROUND(Descuento,2))
            THROW 51000, 'Producto, cantidad o descuento invalidos.', 1;
        IF EXISTS(SELECT ProductoID FROM @Lineas GROUP BY ProductoID HAVING COUNT(*)>1)
            THROW 51000, 'No repita un producto en el detalle.', 1;

        IF NOT EXISTS(SELECT 1 FROM dbo.Empleados WHERE EmpleadoID=@EmpleadoID)
            THROW 51004, 'El empleado no existe.', 1;
        IF EXISTS(SELECT 1 FROM dbo.Empleados WHERE EmpleadoID=@EmpleadoID AND Activo=0)
            THROW 51022, 'El empleado esta inactivo.', 1;
        IF @ClienteID IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.Clientes WHERE ClienteID=@ClienteID)
            THROW 51004, 'El cliente no existe.', 1;

        -- El bloqueo de rango y UNIQUE protegen la factura ante solicitudes simultaneas.
        IF EXISTS(SELECT 1 FROM dbo.Ventas WITH (UPDLOCK,HOLDLOCK) WHERE NumeroFactura=@NumeroFactura)
            THROW 51009, 'El numero de factura ya existe.', 1;

        DECLARE @ID INT, @Cantidad INT, @Stock INT, @Precio DECIMAL(10,2), @Activo BIT;
        -- Orden global por ID para reducir interbloqueos entre ventas/anulaciones.
        DECLARE productos CURSOR LOCAL FAST_FORWARD FOR SELECT ProductoID,Cantidad FROM @Lineas ORDER BY ProductoID;
        OPEN productos;
        FETCH NEXT FROM productos INTO @ID,@Cantidad;
        WHILE @@FETCH_STATUS=0
        BEGIN
            SET @Stock=NULL;
            SELECT @Stock=Stock,@Precio=PrecioVenta,@Activo=Activo
            FROM dbo.Productos WITH (UPDLOCK,HOLDLOCK) WHERE ProductoID=@ID;
            IF @Stock IS NULL THROW 51004, 'Un producto no existe.', 1;
            IF @Activo=0 THROW 51022, 'No se puede vender un producto inactivo.', 1;
            IF @Stock<@Cantidad THROW 51022, 'Stock insuficiente.', 1;
            UPDATE @Lineas SET Precio=@Precio,StockAnterior=@Stock WHERE ProductoID=@ID;
            FETCH NEXT FROM productos INTO @ID,@Cantidad;
        END;
        CLOSE productos;
        DEALLOCATE productos;

        IF EXISTS(SELECT 1 FROM @Lineas WHERE Descuento>CONVERT(DECIMAL(20,2),Cantidad)*Precio)
            THROW 51022, 'El descuento supera el importe de la linea.', 1;
        DECLARE @Subtotal DECIMAL(28,2), @IVA DECIMAL(28,2), @Total DECIMAL(28,2), @Tasa DECIMAL(5,4);
        SELECT @Tasa=TasaIVA FROM dbo.ConfiguracionFacturacion WHERE ConfiguracionID=1;
        IF @Tasa IS NULL THROW 51022, 'Falta configurar la tasa de IVA.', 1;
        SELECT @Subtotal=SUM(CONVERT(DECIMAL(20,2),Cantidad)*Precio-Descuento) FROM @Lineas;
        SET @IVA=ROUND(@Subtotal*@Tasa,2);
        SET @Total=@Subtotal+@IVA;
        IF @Total>99999999.99 OR @Subtotal>99999999.99 OR @IVA>99999999.99
            THROW 51022, 'El importe supera la capacidad monetaria del esquema.', 1;

        INSERT dbo.Ventas(NumeroFactura,ClienteID,EmpleadoID,Subtotal,IVA,Total,FormaPago,Estado)
        VALUES(@NumeroFactura,@ClienteID,@EmpleadoID,@Subtotal,@IVA,@Total,@FormaPago,N'Completada');
        DECLARE @VentaID INT=CONVERT(INT,SCOPE_IDENTITY());
        INSERT dbo.DetalleVenta(VentaID,ProductoID,Cantidad,PrecioUnitario,Descuento)
        SELECT @VentaID,ProductoID,Cantidad,Precio,Descuento FROM @Lineas;
        UPDATE p SET Stock=p.Stock-l.Cantidad
        FROM dbo.Productos p JOIN @Lineas l ON l.ProductoID=p.ProductoID;
        INSERT dbo.MovimientosInventario(ProductoID,TipoMovimiento,Cantidad,StockAnterior,StockNuevo,ReferenciaTipo,ReferenciaID,EmpleadoID)
        SELECT ProductoID,N'Salida',Cantidad,StockAnterior,StockAnterior-Cantidad,N'Venta',@VentaID,@EmpleadoID FROM @Lineas;

        IF @Propia=1 COMMIT;
        SELECT VentaID,NumeroFactura,ClienteID,EmpleadoID,FechaVenta,Subtotal,IVA,Total,FormaPago,Estado
        FROM dbo.Ventas WHERE VentaID=@VentaID;
    END TRY
    BEGIN CATCH
        IF @Propia=1 AND XACT_STATE()<>0 ROLLBACK;
        -- En JDBC la transaccion pertenece a @Transactional; Java hace rollback.
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AnularVenta @VentaID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @Propia BIT=CASE WHEN @@TRANCOUNT=0 THEN 1 ELSE 0 END;
    BEGIN TRY
        IF @Propia=1 BEGIN TRANSACTION;
        IF @VentaID IS NULL OR @VentaID<=0 THROW 51000, 'El ID de venta debe ser positivo.', 1;
        DECLARE @Estado NVARCHAR(20), @EmpleadoID INT;
        SELECT @Estado=Estado,@EmpleadoID=EmpleadoID FROM dbo.Ventas WITH(UPDLOCK,HOLDLOCK) WHERE VentaID=@VentaID;
        IF @Estado IS NULL THROW 51004, 'La venta no existe.', 1;
        IF @Estado=N'Anulada' THROW 51009, 'La venta ya esta anulada.', 1;
        IF @Estado<>N'Completada' THROW 51009, 'Solo se pueden anular ventas completadas.', 1;
        IF NOT EXISTS(SELECT 1 FROM dbo.DetalleVenta WHERE VentaID=@VentaID)
            THROW 51022, 'La venta no tiene detalle; revise su integridad.', 1;

        DECLARE @ProductoID INT,@Cantidad INT,@Stock INT;
        DECLARE productos CURSOR LOCAL FAST_FORWARD FOR
            SELECT ProductoID,Cantidad FROM dbo.DetalleVenta WHERE VentaID=@VentaID ORDER BY ProductoID;
        OPEN productos;
        FETCH NEXT FROM productos INTO @ProductoID,@Cantidad;
        WHILE @@FETCH_STATUS=0
        BEGIN
            SELECT @Stock=Stock FROM dbo.Productos WITH(UPDLOCK,HOLDLOCK) WHERE ProductoID=@ProductoID;
            IF CONVERT(BIGINT,@Stock)+@Cantidad>2147483647
                THROW 51022, 'La restitucion supera la capacidad del stock.', 1;
            UPDATE dbo.Productos SET Stock=Stock+@Cantidad WHERE ProductoID=@ProductoID;
            INSERT dbo.MovimientosInventario(ProductoID,TipoMovimiento,Cantidad,StockAnterior,StockNuevo,ReferenciaTipo,ReferenciaID,EmpleadoID)
            VALUES(@ProductoID,N'Entrada',@Cantidad,@Stock,@Stock+@Cantidad,N'Venta',@VentaID,@EmpleadoID);
            FETCH NEXT FROM productos INTO @ProductoID,@Cantidad;
        END;
        CLOSE productos;
        DEALLOCATE productos;
        UPDATE dbo.Ventas SET Estado=N'Anulada' WHERE VentaID=@VentaID;
        IF @Propia=1 COMMIT;
        SELECT VentaID,NumeroFactura,ClienteID,EmpleadoID,FechaVenta,Subtotal,IVA,Total,FormaPago,Estado
        FROM dbo.Ventas WHERE VentaID=@VentaID;
    END TRY
    BEGIN CATCH
        IF @Propia=1 AND XACT_STATE()<>0 ROLLBACK;
        THROW;
    END CATCH;
END;
GO
