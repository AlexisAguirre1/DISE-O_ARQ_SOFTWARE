using Microsoft.EntityFrameworkCore;
using PyFinal_1.Models;

namespace PyFinal_1.Data
{
    public class ReclamosDbContext : DbContext
    {
        public ReclamosDbContext(
            DbContextOptions<ReclamosDbContext> options)
            : base(options)
        {
        }

        // Tablas de SQL Server
        public DbSet<ReclamoBD> Reclamos { get; set; }
        public DbSet<TipoReclamoBD> TiposReclamo { get; set; }
        public DbSet<OperarioBD> Operarios { get; set; }
        public DbSet<UsuarioBD> Usuarios { get; set; }
        public DbSet<HistorialReclamoBD> HistorialReclamos { get; set; }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Tabla Reclamos
            modelBuilder.Entity<ReclamoBD>(entity =>
            {
                entity.ToTable("Reclamos");
                entity.HasKey(e => e.IdReclamo);

                entity.Property(e => e.CodigoReclamo)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .IsRequired();

                entity.Property(e => e.Detalle)
                    .HasMaxLength(500)
                    .IsUnicode(false)
                    .IsRequired();

                entity.Property(e => e.FechaRegistro)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(e => e.FechaActualizacion)
                    .HasColumnType("datetime");
            });

            // Tabla TiposReclamo
            modelBuilder.Entity<TipoReclamoBD>(entity =>
            {
                entity.ToTable("TiposReclamo");
                entity.HasKey(e => e.IdTipoReclamo);

                entity.Property(e => e.NombreTipo)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .IsRequired();

                entity.Property(e => e.Descripcion)
                    .HasMaxLength(200)
                    .IsUnicode(false);
            });

            // Tabla Operarios (especialidad de cada operario)
            modelBuilder.Entity<OperarioBD>(entity =>
            {
                entity.ToTable("Operarios");
                entity.HasKey(e => e.IdUsuario);
            });

            // Tabla Usuarios (lectura: nombre y rol)
            modelBuilder.Entity<UsuarioBD>(entity =>
            {
                entity.ToTable("Usuarios");
                entity.HasKey(e => e.IdUsuario);
            });

            // Tabla HistorialReclamos (trazabilidad de cambios de estado)
            modelBuilder.Entity<HistorialReclamoBD>(entity =>
            {
                entity.ToTable("HistorialReclamos");
                entity.HasKey(e => e.IdHistorial);

                entity.Property(e => e.Comentario)
                    .HasMaxLength(300)
                    .IsUnicode(false);

                entity.Property(e => e.FechaCambio)
                    .HasColumnType("datetime");
            });
        }
    }
}