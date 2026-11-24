-- ============================================================
-- BASE DE DATOS: Comisariato
-- Motor: Microsoft SQL Server
-- Propósito: Talleres de transacciones
-- IMPORTANTE:
--   0. Por favor revisar cada aspecto del script para poder debatir las buenas prácticas o fallas del mismo.
--   1. Este es un script de instalación inicial; ejecutar una sola vez
--      sobre una base de datos Comisariato vacía.
--   2. No elimina la base de datos ni sus datos automáticamente.
--   3. Si necesita reiniciar las prácticas, utilice el script de reinicio
--      separado incluido al final como comentario y ejecútelo con cuidado.
-- ============================================================

IF DB_ID(N'Comisariato') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE Comisariato;');
END;
GO

USE Comisariato;
GO

-- ============================================================
-- TABLAS CATÁLOGO / MAESTRAS
-- ============================================================

CREATE TABLE dbo.Categorias (
    CategoriaID INT IDENTITY(1,1) NOT NULL,
    Nombre NVARCHAR(100) NOT NULL,
    Descripcion NVARCHAR(255) NULL,
    Activa BIT NOT NULL
        CONSTRAINT DF_Categorias_Activa DEFAULT (1),
    CONSTRAINT PK_Categorias PRIMARY KEY (CategoriaID),
    CONSTRAINT UQ_Categorias_Nombre UNIQUE (Nombre)
);
GO

CREATE TABLE dbo.Proveedores (
    ProveedorID INT IDENTITY(1,1) NOT NULL,
    RUC VARCHAR(13) NOT NULL,
    RazonSocial NVARCHAR(150) NOT NULL,
    Telefono VARCHAR(20) NULL,
    Email NVARCHAR(100) NULL,
    Direccion NVARCHAR(255) NULL,
    Activo BIT NOT NULL
        CONSTRAINT DF_Proveedores_Activo DEFAULT (1),
    CONSTRAINT PK_Proveedores PRIMARY KEY (ProveedorID),
    CONSTRAINT UQ_Proveedores_RUC UNIQUE (RUC)
);
GO

CREATE TABLE dbo.Clientes (
    ClienteID INT IDENTITY(1,1) NOT NULL,
    Cedula VARCHAR(10) NOT NULL,
    Nombres NVARCHAR(100) NOT NULL,
    Apellidos NVARCHAR(100) NOT NULL,
    Telefono VARCHAR(20) NULL,
    Email NVARCHAR(100) NULL,
    FechaRegistro DATETIME2(0) NOT NULL
        CONSTRAINT DF_Clientes_Fecha DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_Clientes PRIMARY KEY (ClienteID),
    CONSTRAINT UQ_Clientes_Cedula UNIQUE (Cedula)
);
GO

CREATE TABLE dbo.Empleados (
    EmpleadoID INT IDENTITY(1,1) NOT NULL,
    Cedula VARCHAR(10) NOT NULL,
    Nombres NVARCHAR(100) NOT NULL,
    Apellidos NVARCHAR(100) NOT NULL,
    Cargo NVARCHAR(50) NOT NULL,
    FechaIngreso DATE NOT NULL,
    Salario DECIMAL(10,2) NOT NULL,
    Activo BIT NOT NULL
        CONSTRAINT DF_Empleados_Activo DEFAULT (1),
    CONSTRAINT PK_Empleados PRIMARY KEY (EmpleadoID),
    CONSTRAINT UQ_Empleados_Cedula UNIQUE (Cedula),
    CONSTRAINT CK_Empleados_Salario CHECK (Salario > 0)
);
GO

-- ============================================================
-- PRODUCTOS
-- ============================================================

CREATE TABLE dbo.Productos (
    ProductoID INT IDENTITY(1,1) NOT NULL,
    CodigoBarras VARCHAR(50) NOT NULL,
    Nombre NVARCHAR(150) NOT NULL,
    CategoriaID INT NOT NULL,
    ProveedorID INT NULL,
    PrecioCompra DECIMAL(10,2) NOT NULL,
    PrecioVenta DECIMAL(10,2) NOT NULL,
    Stock INT NOT NULL
        CONSTRAINT DF_Productos_Stock DEFAULT (0),
    StockMinimo INT NOT NULL
        CONSTRAINT DF_Productos_StockMin DEFAULT (0),
    Activo BIT NOT NULL
        CONSTRAINT DF_Productos_Activo DEFAULT (1),
    CONSTRAINT PK_Productos PRIMARY KEY (ProductoID),
    CONSTRAINT UQ_Productos_CodigoBarras UNIQUE (CodigoBarras),
    CONSTRAINT FK_Productos_Categorias
        FOREIGN KEY (CategoriaID) REFERENCES dbo.Categorias(CategoriaID),
    CONSTRAINT FK_Productos_Proveedores
        FOREIGN KEY (ProveedorID) REFERENCES dbo.Proveedores(ProveedorID),
    CONSTRAINT CK_Productos_PrecioCompra CHECK (PrecioCompra >= 0),
    CONSTRAINT CK_Productos_PrecioVenta CHECK (PrecioVenta >= 0),
    -- Regla didáctica: el comisariato no vende por debajo del costo.
    -- Eliminar esta restricción si se desea modelar promociones o pérdidas.
    CONSTRAINT CK_Productos_Margen CHECK (PrecioVenta >= PrecioCompra),
    CONSTRAINT CK_Productos_Stock CHECK (Stock >= 0),
    CONSTRAINT CK_Productos_StockMin CHECK (StockMinimo >= 0)
);
GO

-- ============================================================
-- VENTAS (cabecera + detalle)
-- ============================================================

CREATE TABLE dbo.Ventas (
    VentaID INT IDENTITY(1,1) NOT NULL,
    NumeroFactura VARCHAR(20) NOT NULL,
    ClienteID INT NULL, -- NULL representa consumidor final no identificado.
    EmpleadoID INT NOT NULL,
    FechaVenta DATETIME2(0) NOT NULL
        CONSTRAINT DF_Ventas_Fecha DEFAULT (SYSDATETIME()),
    Subtotal DECIMAL(10,2) NOT NULL,
    IVA DECIMAL(10,2) NOT NULL,
    Total DECIMAL(10,2) NOT NULL,
    FormaPago NVARCHAR(30) NOT NULL,
    Estado NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Ventas_Estado DEFAULT (N'Completada'),
    CONSTRAINT PK_Ventas PRIMARY KEY (VentaID),
    CONSTRAINT UQ_Ventas_NumeroFactura UNIQUE (NumeroFactura),
    CONSTRAINT FK_Ventas_Clientes
        FOREIGN KEY (ClienteID) REFERENCES dbo.Clientes(ClienteID),
    CONSTRAINT FK_Ventas_Empleados
        FOREIGN KEY (EmpleadoID) REFERENCES dbo.Empleados(EmpleadoID),
    CONSTRAINT CK_Ventas_Subtotal CHECK (Subtotal >= 0),
    CONSTRAINT CK_Ventas_IVA CHECK (IVA >= 0),
    CONSTRAINT CK_Ventas_Total CHECK (Total >= 0),
    CONSTRAINT CK_Ventas_FormaPago
        CHECK (FormaPago IN (N'Efectivo', N'Tarjeta', N'Transferencia')),
    CONSTRAINT CK_Ventas_Estado
        CHECK (Estado IN (N'Pendiente', N'Completada', N'Anulada'))
);
GO

CREATE TABLE dbo.DetalleVenta (
    DetalleVentaID INT IDENTITY(1,1) NOT NULL,
    VentaID INT NOT NULL,
    ProductoID INT NOT NULL,
    Cantidad INT NOT NULL,
    PrecioUnitario DECIMAL(10,2) NOT NULL,
    Descuento DECIMAL(10,2) NOT NULL
        CONSTRAINT DF_DV_Descuento DEFAULT (0),
    SubtotalLinea AS
        (CONVERT(DECIMAL(12,2), Cantidad * PrecioUnitario - Descuento)) PERSISTED,
    CONSTRAINT PK_DetalleVenta PRIMARY KEY (DetalleVentaID),
    CONSTRAINT FK_DetalleVenta_Ventas
        FOREIGN KEY (VentaID) REFERENCES dbo.Ventas(VentaID),
    CONSTRAINT FK_DetalleVenta_Productos
        FOREIGN KEY (ProductoID) REFERENCES dbo.Productos(ProductoID),
    CONSTRAINT CK_DV_Cantidad CHECK (Cantidad > 0),
    CONSTRAINT CK_DV_Precio CHECK (PrecioUnitario >= 0),
    CONSTRAINT CK_DV_Descuento CHECK (Descuento >= 0),
    CONSTRAINT CK_DV_DescuentoMaximo
        CHECK (Descuento <= Cantidad * PrecioUnitario),
    CONSTRAINT UQ_DV_Venta_Producto UNIQUE (VentaID, ProductoID)
);
GO

-- ============================================================
-- COMPRAS A PROVEEDORES (cabecera + detalle)
-- ============================================================

CREATE TABLE dbo.Compras (
    CompraID INT IDENTITY(1,1) NOT NULL,
    NumeroOrden VARCHAR(20) NOT NULL,
    ProveedorID INT NOT NULL,
    EmpleadoID INT NOT NULL,
    FechaCompra DATETIME2(0) NOT NULL
        CONSTRAINT DF_Compras_Fecha DEFAULT (SYSDATETIME()),
    Total DECIMAL(10,2) NOT NULL,
    Estado NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Compras_Estado DEFAULT (N'Recibida'),
    CONSTRAINT PK_Compras PRIMARY KEY (CompraID),
    CONSTRAINT UQ_Compras_NumeroOrden UNIQUE (NumeroOrden),
    CONSTRAINT FK_Compras_Proveedores
        FOREIGN KEY (ProveedorID) REFERENCES dbo.Proveedores(ProveedorID),
    CONSTRAINT FK_Compras_Empleados
        FOREIGN KEY (EmpleadoID) REFERENCES dbo.Empleados(EmpleadoID),
    CONSTRAINT CK_Compras_Total CHECK (Total >= 0),
    CONSTRAINT CK_Compras_Estado
        CHECK (Estado IN (N'Pendiente', N'Recibida', N'Anulada'))
);
GO

CREATE TABLE dbo.DetalleCompra (
    DetalleCompraID INT IDENTITY(1,1) NOT NULL,
    CompraID INT NOT NULL,
    ProductoID INT NOT NULL,
    Cantidad INT NOT NULL,
    CostoUnitario DECIMAL(10,2) NOT NULL,
    SubtotalLinea AS
        (CONVERT(DECIMAL(12,2), Cantidad * CostoUnitario)) PERSISTED,
    CONSTRAINT PK_DetalleCompra PRIMARY KEY (DetalleCompraID),
    CONSTRAINT FK_DetalleCompra_Compras
        FOREIGN KEY (CompraID) REFERENCES dbo.Compras(CompraID),
    CONSTRAINT FK_DetalleCompra_Productos
        FOREIGN KEY (ProductoID) REFERENCES dbo.Productos(ProductoID),
    CONSTRAINT CK_DC_Cantidad CHECK (Cantidad > 0),
    CONSTRAINT CK_DC_Costo CHECK (CostoUnitario >= 0),
    -- Un producto solo puede aparecer una vez en cada orden de compra.
    CONSTRAINT UQ_DC_Compra_Producto UNIQUE (CompraID, ProductoID)
);
GO

-- ============================================================
-- BITÁCORA DE MOVIMIENTOS DE INVENTARIO
-- ============================================================

CREATE TABLE dbo.MovimientosInventario (
    MovimientoID BIGINT IDENTITY(1,1) NOT NULL,
    ProductoID INT NOT NULL,
    TipoMovimiento NVARCHAR(20) NOT NULL, -- Entrada / Salida / Ajuste
    Cantidad INT NOT NULL,
    StockAnterior INT NOT NULL,
    StockNuevo INT NOT NULL,
    ReferenciaTipo NVARCHAR(20) NULL, -- Venta / Compra / Ajuste
    ReferenciaID INT NULL,            -- Se valida en la lógica transaccional.
    FechaMovimiento DATETIME2(0) NOT NULL
        CONSTRAINT DF_MovInv_Fecha DEFAULT (SYSDATETIME()),
    EmpleadoID INT NULL,
    CONSTRAINT PK_MovimientosInventario PRIMARY KEY (MovimientoID),
    CONSTRAINT FK_MovInv_Productos
        FOREIGN KEY (ProductoID) REFERENCES dbo.Productos(ProductoID),
    CONSTRAINT FK_MovInv_Empleados
        FOREIGN KEY (EmpleadoID) REFERENCES dbo.Empleados(EmpleadoID),
    CONSTRAINT CK_MovInv_Tipo
        CHECK (TipoMovimiento IN (N'Entrada', N'Salida', N'Ajuste')),
    CONSTRAINT CK_MovInv_RefTipo
        CHECK (ReferenciaTipo IN (N'Venta', N'Compra', N'Ajuste')
               OR ReferenciaTipo IS NULL),
    CONSTRAINT CK_MovInv_Cantidad CHECK (Cantidad > 0),
    CONSTRAINT CK_MovInv_StockAnterior CHECK (StockAnterior >= 0),
    CONSTRAINT CK_MovInv_StockNuevo CHECK (StockNuevo >= 0),
    CONSTRAINT CK_MovInv_DeltaEntrada
        CHECK (TipoMovimiento <> N'Entrada'
               OR StockNuevo = StockAnterior + Cantidad),
    CONSTRAINT CK_MovInv_DeltaSalida
        CHECK (TipoMovimiento <> N'Salida'
               OR StockNuevo = StockAnterior - Cantidad),
    CONSTRAINT CK_MovInv_ReferenciaCompleta
        CHECK (
            (ReferenciaTipo IS NULL AND ReferenciaID IS NULL)
            OR
            (ReferenciaTipo IS NOT NULL AND ReferenciaID IS NOT NULL)
        )
);
GO

-- ============================================================
-- ÍNDICES DE APOYO
-- Los índices UNIQUE compuestos ya cubren búsquedas por la primera
-- columna (VentaID / CompraID), por eso no se crean índices duplicados.
-- ============================================================

CREATE INDEX IX_Productos_CategoriaID
    ON dbo.Productos(CategoriaID);

CREATE INDEX IX_Productos_ProveedorID
    ON dbo.Productos(ProveedorID);

CREATE INDEX IX_Ventas_ClienteID
    ON dbo.Ventas(ClienteID);

CREATE INDEX IX_Ventas_EmpleadoID
    ON dbo.Ventas(EmpleadoID);

CREATE INDEX IX_Ventas_FechaVenta
    ON dbo.Ventas(FechaVenta);

-- No se crea IX_DetalleVenta_VentaID: lo cubre UQ_DV_Venta_Producto.
CREATE INDEX IX_DetalleVenta_ProductoID
    ON dbo.DetalleVenta(ProductoID);

CREATE INDEX IX_Compras_ProveedorID
    ON dbo.Compras(ProveedorID);

CREATE INDEX IX_Compras_EmpleadoID
    ON dbo.Compras(EmpleadoID);

CREATE INDEX IX_DetalleCompra_ProductoID
    ON dbo.DetalleCompra(ProductoID);

CREATE INDEX IX_MovInv_ProductoID
    ON dbo.MovimientosInventario(ProductoID);

CREATE INDEX IX_MovInv_EmpleadoID
    ON dbo.MovimientosInventario(EmpleadoID);
GO

-- ============================================================
-- DATOS DE PRUEBA
-- Ejecutar este bloque una sola vez después de crear las tablas.
-- ============================================================

INSERT INTO dbo.Categorias (Nombre, Descripcion) VALUES
(N'Lácteos', N'Leche, quesos, yogures'),
(N'Bebidas', N'Gaseosas, jugos, agua'),
(N'Aseo', N'Detergentes, jabones'),
(N'Snacks', N'Papas, galletas, dulces'),
(N'Granos', N'Arroz, azúcar, sal');

INSERT INTO dbo.Proveedores (RUC, RazonSocial, Telefono, Email) VALUES
('1790012345001', N'Distribuidora Andina S.A.', '022345678', N'ventas@andina.com'),
('1790098765001', N'Lácteos del Valle Cía. Ltda.', '022987654', N'contacto@lactvalle.com'),
('1790055555001', N'Importadora El Sol', '022111222', N'info@elsol.com');

INSERT INTO dbo.Clientes (Cedula, Nombres, Apellidos, Telefono) VALUES
('1712345678', N'María Fernanda', N'Gómez Ruiz', '0991111111'),
('1798765432', N'Carlos Andrés', N'Pérez López', '0992222222'),
('1700112233', N'Ana Lucía', N'Torres Mena', '0993333333');

INSERT INTO dbo.Empleados
    (Cedula, Nombres, Apellidos, Cargo, FechaIngreso, Salario) VALUES
('1700000001', N'Juan', N'Cajero Uno', N'Cajero', '2022-01-10', 450.00),
('1700000002', N'Pedro', N'Bodeguero Uno', N'Bodeguero', '2021-05-20', 500.00),
('1700000003', N'Lucía', N'Supervisora', N'Supervisor', '2020-03-15', 800.00);

INSERT INTO dbo.Productos
    (CodigoBarras, Nombre, CategoriaID, ProveedorID,
     PrecioCompra, PrecioVenta, Stock, StockMinimo) VALUES
('7501000000011', N'Leche Entera 1L',       1, 2, 0.75, 1.10, 100, 20),
('7501000000028', N'Queso Fresco 500g',     1, 2, 2.50, 3.80,  40, 10),
('7502000000010', N'Gaseosa Cola 2L',      2, 3, 1.20, 1.90,  60, 15),
('7503000000015', N'Detergente 1kg',       3, 1, 2.10, 3.00,  30, 10),
('7504000000012', N'Papas Fritas 150g',    4, 3, 0.60, 1.00,  80, 20),
('7505000000019', N'Arroz 5lb',            5, 1, 2.40, 3.20,  50, 15),
('7505000000026', N'Azúcar 2kg',           5, 1, 1.50, 2.10,  45, 10);
GO

-- ============================================================
-- SCRIPT OPCIONAL DE REINICIO (NO EJECUTAR COMO PARTE DE LA INSTALACIÓN)
-- Para eliminar completamente la base de datos y recrearla desde cero,
-- abrir una ventana NUEVA de consulta y ejecutar por separado:
--
-- USE master;
-- GO
-- ALTER DATABASE Comisariato SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
-- DROP DATABASE Comisariato;
-- GO
--
-- Después, ejecutar nuevamente este script de instalación.
-- ADVERTENCIA: este procedimiento elimina permanentemente los datos.
-- ============================================================
