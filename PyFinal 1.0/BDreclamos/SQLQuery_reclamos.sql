CREATE DATABASE BD_Reclamos;
GO
USE BD_Reclamos;
GO
-- 1. TABLA ROLES
CREATE TABLE Roles (
    IdRol INT IDENTITY(1,1) PRIMARY KEY,
    NombreRol VARCHAR(50) NOT NULL
);
GO
-- 2. TABLA USUARIOS
CREATE TABLE Usuarios (
    IdUsuario INT IDENTITY(1,1) PRIMARY KEY,
    NombresApellidos VARCHAR(100) NOT NULL,
    Correo VARCHAR(100) NOT NULL,
    Contrasena VARCHAR(100) NOT NULL,
    IdRol INT NOT NULL,
    FOREIGN KEY (IdRol) REFERENCES Roles(IdRol)
);
GO
-- 3. TABLA TIPOS DE RECLAMO
CREATE TABLE TiposReclamo (
    IdTipoReclamo INT IDENTITY(1,1) PRIMARY KEY,
    NombreTipo VARCHAR(50) NOT NULL,
    Descripcion VARCHAR(200)
);
GO
-- 4. TABLA ESTADOS DE RECLAMO
CREATE TABLE EstadosReclamo (
    IdEstado INT IDENTITY(1,1) PRIMARY KEY,
    NombreEstado VARCHAR(50) NOT NULL
);
GO
-- 5. TABLA RECLAMOS (Con auditoría básica)
CREATE TABLE Reclamos (
    IdReclamo INT IDENTITY(1,1) PRIMARY KEY,
    CodigoReclamo VARCHAR(20) NOT NULL,
    IdUsuario INT NOT NULL,
    IdTipoReclamo INT NOT NULL,
    IdEstado INT NOT NULL,
    Detalle VARCHAR(500) NOT NULL,
    FechaRegistro DATETIME DEFAULT GETDATE(),
    FechaActualizacion DATETIME NULL,
    Activo BIT DEFAULT 1,
    FOREIGN KEY (IdUsuario) REFERENCES Usuarios(IdUsuario),
    FOREIGN KEY (IdTipoReclamo) REFERENCES TiposReclamo(IdTipoReclamo),
    FOREIGN KEY (IdEstado) REFERENCES EstadosReclamo(IdEstado)
);
GO
-- 6. TABLA HISTORIAL DE RECLAMOS (Para trazabilidad de cambios)
CREATE TABLE HistorialReclamos (
    IdHistorial INT IDENTITY(1,1) PRIMARY KEY,
    IdReclamo INT NOT NULL,
    IdEstadoAnterior INT NOT NULL,
    IdEstadoNuevo INT NOT NULL,
    IdUsuarioModificacion INT NOT NULL,
    Comentario VARCHAR(300),
    FechaCambio DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (IdReclamo) REFERENCES Reclamos(IdReclamo),
    FOREIGN KEY (IdEstadoAnterior) REFERENCES EstadosReclamo(IdEstado),
    FOREIGN KEY (IdEstadoNuevo) REFERENCES EstadosReclamo(IdEstado),
    FOREIGN KEY (IdUsuarioModificacion) REFERENCES Usuarios(IdUsuario)
);
GO
-- INSERCIÓN DE DATOS INICIALES (SEEDS)
INSERT INTO Roles (NombreRol) VALUES ('Solicitante'), ('Supervisor'), ('Operario');
GO
INSERT INTO Usuarios (NombresApellidos, Correo, Contrasena, IdRol) VALUES
('Juan Perez', 'juan@gmail.com', '123456', 1),
('Maria Lopez', 'maria@gmail.com', '123456', 2),
('Carlos Ramirez', 'carlos@gmail.com', '123456', 3);
GO
INSERT INTO TiposReclamo (NombreTipo, Descripcion) VALUES
('Facturacion', 'Problemas relacionados con la facturación'),
('Cobertura', 'Problemas relacionados con la cobertura'),
('Atencion', 'Problemas relacionados con la atención al cliente'),
('Equipos', 'Problemas relacionados con equipos');
GO
INSERT INTO EstadosReclamo (NombreEstado) VALUES
('Registrado'), ('En evaluacion'), ('En proceso'), ('Resuelto'), ('Cerrado');
GO
INSERT INTO Reclamos (CodigoReclamo, IdUsuario, IdTipoReclamo, IdEstado, Detalle) VALUES
('REC001', 1, 1, 1, 'Problema con el monto de la factura.'),
('REC002', 1, 2, 1, 'Problema con la cobertura del servicio.');
GO
-- VISTAS ÚTILES PARA CONSULTAS EN EL BACKEND
CREATE VIEW VW_DetalleReclamos AS
SELECT 
    R.IdReclamo,
    R.CodigoReclamo,
    U.NombresApellidos AS Solicitante,
    U.Correo AS CorreoSolicitante,
    TR.NombreTipo AS TipoReclamo,
    ER.NombreEstado AS EstadoReclamo,
    R.Detalle,
    R.FechaRegistro,
    R.Activo
FROM Reclamos R
INNER JOIN Usuarios U ON R.IdUsuario = U.IdUsuario
INNER JOIN TiposReclamo TR ON R.IdTipoReclamo = TR.IdTipoReclamo
INNER JOIN EstadosReclamo ER ON R.IdEstado = ER.IdEstado;
GO
SELECT * FROM Roles;
SELECT * FROM Usuarios;
SELECT * FROM TiposReclamo;
SELECT * FROM EstadosReclamo;
SELECT * FROM Reclamos;
SELECT * FROM HistorialReclamos;
SELECT * FROM VW_DetalleReclamos;