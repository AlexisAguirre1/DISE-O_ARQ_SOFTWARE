
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

        // TABLAS DE SQL SERVER
        public DbSet<ReclamoBD> Reclamos { get; set; }
        public DbSet<TipoReclamoBD> TiposReclamo { get; set; }
        public DbSet<OperarioBD> Operarios { get; set; }
        public DbSet<UsuarioBD> Usuarios { get; set; }
        public DbSet<HistorialReclamoBD> HistorialReclamos { get; set; }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // TABLA RECLAMOS
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

            // TABLA TIPOS RECLAMO
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

            // TABLA OPERARIOS
            modelBuilder.Entity<OperarioBD>(entity =>
            {
                entity.ToTable("Operarios");

                entity.HasKey(e => e.IdUsuario);
            });

            // TABLA USUARIOS CON DNI
            modelBuilder.Entity<UsuarioBD>(entity =>
            {
                entity.ToTable("Usuarios");

                entity.HasKey(e => e.IdUsuario);

                entity.Property(e => e.NombresApellidos)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .IsRequired();

                entity.Property(e => e.Correo)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .IsRequired();

                entity.Property(e => e.Contrasena)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .IsRequired();

                entity.Property(e => e.DNI)
                    .HasMaxLength(8)
                    .IsUnicode(false)
                    .IsRequired(false);

                entity.Property(e => e.IdRol)
                    .IsRequired();
            });

            // TABLA HISTORIAL RECLAMOS
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
