using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace backend.Models;

public partial class TransporteContext : DbContext
{
    public TransporteContext()
    {
    }

    public TransporteContext(DbContextOptions<TransporteContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Asiento> Asientos { get; set; }

    public virtual DbSet<Conductor> Conductors { get; set; }

    public virtual DbSet<Horario> Horarios { get; set; }

    public virtual DbSet<Menu> Menus { get; set; }

    public virtual DbSet<PuntoVentum> PuntoVenta { get; set; }

    public virtual DbSet<Rol> Rols { get; set; }

    public virtual DbSet<Rutum> Ruta { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<Vehiculo> Vehiculos { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=transporte;Username=postgres;Password=postgres");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresEnum("tipo_permiso", new[] { "menu", "submenu", "boton" });

        modelBuilder.Entity<Asiento>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("asientos_pkey");

            entity.ToTable("asientos");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Cantidad).HasColumnName("cantidad");
            entity.Property(e => e.Esminibus).HasColumnName("esminibus");
            entity.Property(e => e.Estado).HasColumnName("estado");
            entity.Property(e => e.Filas).HasColumnName("filas");
            entity.Property(e => e.Nombre)
                .HasMaxLength(20)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Conductor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("conductor_pkey");

            entity.ToTable("conductor");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Apellidos)
                .HasMaxLength(50)
                .HasColumnName("apellidos");
            entity.Property(e => e.Categoria)
                .HasMaxLength(10)
                .HasColumnName("categoria");
            entity.Property(e => e.Estado).HasColumnName("estado");
            entity.Property(e => e.Licencia)
                .HasMaxLength(20)
                .HasColumnName("licencia");
            entity.Property(e => e.Nombres)
                .HasMaxLength(50)
                .HasColumnName("nombres");
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .HasColumnName("telefono");
        });

        modelBuilder.Entity<Horario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("horario_pkey");

            entity.ToTable("horario");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Estado)
                .HasDefaultValue(true)
                .HasColumnName("estado");
            entity.Property(e => e.Fecha)
                .HasMaxLength(20)
                .HasColumnName("fecha");
            entity.Property(e => e.Hora)
                .HasMaxLength(20)
                .HasColumnName("hora");
            entity.Property(e => e.RutaId).HasColumnName("ruta_id");
            entity.Property(e => e.VehiculoId).HasColumnName("vehiculo_id");

            entity.HasOne(d => d.Ruta).WithMany(p => p.Horarios)
                .HasForeignKey(d => d.RutaId)
                .OnDelete(DeleteBehavior.Restrict)//tambien puede tener estos valores: Cascade(borra tambien los registros donde se usa esta clave foranea), SetNull(inserta null en la fk), Restrict(no permite borrar padre si tiene fk poray repartidas)
                .HasConstraintName("horario_ruta_id_fkey");

            entity.HasOne(d => d.Vehiculo).WithMany(p => p.Horarios)
                .HasForeignKey(d => d.VehiculoId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("horario_vehiculo_id_fkey");
        });

        modelBuilder.Entity<Menu>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("menus_pkey");

            entity.ToTable("menus");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Icono)
                .HasMaxLength(50)
                .HasColumnName("icono");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
            entity.Property(e => e.Orden)
                .HasDefaultValue(0)
                .HasColumnName("orden");
            entity.Property(e => e.PadreId).HasColumnName("padre_id");
            entity.Property(e => e.RutaAccion)
                .HasMaxLength(100)
                .HasColumnName("ruta_accion");
            entity.Property(e => e.Tipo)
                .HasMaxLength(20)
                .HasColumnName("tipo");

            entity.HasOne(d => d.Padre).WithMany(p => p.InversePadre)
                .HasForeignKey(d => d.PadreId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("menus_padre_id_fkey");
        });

        modelBuilder.Entity<PuntoVentum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("punto_venta_pkey");

            entity.ToTable("punto_venta");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Direccion)
                .HasMaxLength(100)
                .HasColumnName("direccion");
            entity.Property(e => e.EsPuntoVenta)
                .HasDefaultValue(true)
                .HasColumnName("es_punto_venta");
            entity.Property(e => e.Nombre)
                .HasMaxLength(30)
                .HasColumnName("nombre");
            entity.Property(e => e.Telefono)
                .HasMaxLength(50)
                .HasColumnName("telefono");
            entity.Property(e => e.VisiblePasajes)
                .HasDefaultValue(true)
                .HasColumnName("visible_pasajes");
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("rol_pkey");

            entity.ToTable("rol");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Estado).HasColumnName("estado");
            entity.Property(e => e.Nombre)
                .HasMaxLength(20)
                .HasColumnName("nombre");

            entity.HasMany(d => d.Menus).WithMany(p => p.Rols)
                .UsingEntity<Dictionary<string, object>>(
                    "RolMenu",
                    r => r.HasOne<Menu>().WithMany()
                        .HasForeignKey("MenuId")
                        .HasConstraintName("rol_menu_menu_id_fkey"),
                    l => l.HasOne<Rol>().WithMany()
                        .HasForeignKey("RolId")
                        .HasConstraintName("rol_menu_rol_id_fkey"),
                    j =>
                    {
                        j.HasKey("RolId", "MenuId").HasName("rol_menu_pkey");
                        j.ToTable("rol_menu");
                        j.IndexerProperty<int>("RolId").HasColumnName("rol_id");
                        j.IndexerProperty<int>("MenuId").HasColumnName("menu_id");
                    });
        });

        modelBuilder.Entity<Rutum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ruta_pkey");

            entity.ToTable("ruta");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DestinoId).HasColumnName("destino_id");
            entity.Property(e => e.Dias)
                .HasMaxLength(50)
                .HasColumnName("dias");
            entity.Property(e => e.Estado)
                .HasDefaultValue(true)
                .HasColumnName("estado");
            entity.Property(e => e.OrigenId).HasColumnName("origen_id");
            entity.Property(e => e.Tarifa).HasColumnName("tarifa");

            entity.HasOne(d => d.Destino).WithMany(p => p.RutumDestinos)
                .HasForeignKey(d => d.DestinoId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("ruta_destino_id_fkey");

            entity.HasOne(d => d.Origen).WithMany(p => p.RutumOrigens)
                .HasForeignKey(d => d.OrigenId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("ruta_origen_id_fkey");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("usuario_pkey");

            entity.ToTable("usuario");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Acceso)
                .HasDefaultValue(true)
                .HasColumnName("acceso");
            entity.Property(e => e.Clave)
                .HasMaxLength(100)
                .HasColumnName("clave");
            entity.Property(e => e.Estado).HasColumnName("estado");
            entity.Property(e => e.PuntoVentaId).HasColumnName("punto_venta_id");
            entity.Property(e => e.RolId).HasColumnName("rol_id");
            entity.Property(e => e.UltimoAcceso)
                .HasMaxLength(50)
                .HasColumnName("ultimo_acceso");
            entity.Property(e => e.Usuario1)
                .HasMaxLength(50)
                .HasColumnName("usuario");

            entity.HasOne(d => d.PuntoVenta).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.PuntoVentaId)
                .HasConstraintName("fk_usuario_punto_venta");

            entity.HasOne(d => d.Rol).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.RolId)
                .HasConstraintName("usuario_rol_id_fkey");
        });

        modelBuilder.Entity<Vehiculo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("vehiculo_pkey");

            entity.ToTable("vehiculo");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Aseguradora)
                .HasMaxLength(50)
                .HasColumnName("aseguradora");
            entity.Property(e => e.AsientosId).HasColumnName("asientos_id");
            entity.Property(e => e.Color)
                .HasMaxLength(20)
                .HasColumnName("color");
            entity.Property(e => e.ConductorId).HasColumnName("conductor_id");
            entity.Property(e => e.Estado)
                .HasDefaultValue(true)
                .HasColumnName("estado");
            entity.Property(e => e.Marca)
                .HasMaxLength(20)
                .HasColumnName("marca");
            entity.Property(e => e.Modelo)
                .HasMaxLength(20)
                .HasColumnName("modelo");
            entity.Property(e => e.Movil)
                .HasMaxLength(10)
                .HasColumnName("movil");
            entity.Property(e => e.Placa)
                .HasMaxLength(10)
                .HasColumnName("placa");
            entity.Property(e => e.PropietarioId).HasColumnName("propietario_id");
            entity.Property(e => e.Soat)
                .HasMaxLength(20)
                .HasColumnName("soat");
            entity.Property(e => e.Tipo)
                .HasMaxLength(20)
                .HasColumnName("tipo");

            entity.HasOne(d => d.Asientos).WithMany(p => p.Vehiculos)
                .HasForeignKey(d => d.AsientosId)
                .HasConstraintName("fk_vehiculo_asientos");

            entity.HasOne(d => d.Conductor).WithMany(p => p.VehiculoConductors)
                .HasForeignKey(d => d.ConductorId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("vehiculo_conductor_id_fkey");

            entity.HasOne(d => d.Propietario).WithMany(p => p.VehiculoPropietarios)
                .HasForeignKey(d => d.PropietarioId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("vehiculo_propietario_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
