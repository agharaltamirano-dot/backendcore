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

    public virtual DbSet<Cliente> Clientes { get; set; }

    public virtual DbSet<Conductor> Conductors { get; set; }

    public virtual DbSet<Destino> Destinos { get; set; }

    public virtual DbSet<DistribucionAsiento> DistribucionAsientos { get; set; }

    public virtual DbSet<Encomiendum> Encomienda { get; set; }

    public virtual DbSet<Envio> Envios { get; set; }

    public virtual DbSet<Horario> Horarios { get; set; }

    public virtual DbSet<Menu> Menus { get; set; }

    public virtual DbSet<Pasaje> Pasajes { get; set; }

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
            entity.HasKey(e => e.Id).HasName("asiento_pkey");

            entity.ToTable("asiento");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Columna).HasColumnName("columna");
            entity.Property(e => e.DistribucionId).HasColumnName("distribucion_id");
            entity.Property(e => e.Estado).HasColumnName("estado");
            entity.Property(e => e.Fila).HasColumnName("fila");
            entity.Property(e => e.Numero).HasColumnName("numero");

            entity.HasOne(d => d.Distribucion).WithMany(p => p.Asientos)
                .HasForeignKey(d => d.DistribucionId)
                .HasConstraintName("asiento_distribucion_id_fkey");
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cliente_pkey");

            entity.ToTable("cliente");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Ci)
                .HasMaxLength(20)
                .HasColumnName("ci");
            entity.Property(e => e.Estado)
                .HasDefaultValue(true)
                .HasColumnName("estado");
            entity.Property(e => e.NombreCompleto)
                .HasMaxLength(50)
                .HasColumnName("nombre_completo");
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .HasColumnName("telefono");
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
            entity.Property(e => e.FotoLicencia)
                .HasMaxLength(100)
                .HasColumnName("foto_licencia");
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

        modelBuilder.Entity<Destino>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("destino_pkey");

            entity.ToTable("destino");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EsOrigen).HasColumnName("es_origen");
            entity.Property(e => e.Orden).HasColumnName("orden");
            entity.Property(e => e.PuntoVentaId).HasColumnName("punto_venta_id");
            entity.Property(e => e.RutaId).HasColumnName("ruta_id");
            entity.Property(e => e.Tarifa)
                .HasDefaultValue(50.0)
                .HasColumnName("tarifa");

            entity.HasOne(d => d.PuntoVenta).WithMany(p => p.Destinos)
                .HasForeignKey(d => d.PuntoVentaId)
                .HasConstraintName("destino_punto_venta_id_fkey");

            entity.HasOne(d => d.Ruta).WithMany(p => p.Destinos)
                .HasForeignKey(d => d.RutaId)
                .HasConstraintName("destino_ruta_id_fkey");
        });

        modelBuilder.Entity<DistribucionAsiento>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("distribucion_asiento_pkey");

            entity.ToTable("distribucion_asiento");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Estado).HasColumnName("estado");
            entity.Property(e => e.Nombre)
                .HasMaxLength(20)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Encomiendum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("encomienda_pkey");

            entity.ToTable("encomienda");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ClienteConsignatarioId).HasColumnName("cliente_consignatario_id");
            entity.Property(e => e.ClienteRemitenteId).HasColumnName("cliente_remitente_id");
            entity.Property(e => e.Contenido)
                .HasMaxLength(150)
                .HasColumnName("contenido");
            entity.Property(e => e.Destino)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Tarija'::character varying")
                .HasColumnName("destino");
            entity.Property(e => e.Estado).HasColumnName("estado");
            entity.Property(e => e.FechaEntrega)
                .HasMaxLength(20)
                .HasColumnName("fecha_entrega");
            entity.Property(e => e.FechaRecepcion)
                .HasMaxLength(20)
                .HasColumnName("fecha_recepcion");
            entity.Property(e => e.Monto).HasColumnName("monto");
            entity.Property(e => e.Numero)
                .HasMaxLength(20)
                .HasColumnName("numero");
            entity.Property(e => e.Pagado)
                .HasDefaultValue(true)
                .HasColumnName("pagado");
            entity.Property(e => e.UsuarioAnulaId).HasColumnName("usuario_anula_id");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.ClienteConsignatario).WithMany(p => p.EncomiendumClienteConsignatarios)
                .HasForeignKey(d => d.ClienteConsignatarioId)
                .HasConstraintName("encomienda_cliente_consignatario_id_fkey");

            entity.HasOne(d => d.ClienteRemitente).WithMany(p => p.EncomiendumClienteRemitentes)
                .HasForeignKey(d => d.ClienteRemitenteId)
                .HasConstraintName("encomienda_cliente_remitente_id_fkey");

            entity.HasOne(d => d.UsuarioAnula).WithMany(p => p.EncomiendumUsuarioAnulas)
                .HasForeignKey(d => d.UsuarioAnulaId)
                .HasConstraintName("encomienda_usuario_anula_id_fkey");

            entity.HasOne(d => d.Usuario).WithMany(p => p.EncomiendumUsuarios)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("encomienda_usuario_id_fkey");
        });

        modelBuilder.Entity<Envio>(entity =>
{
    entity.HasKey(e => e.Id).HasName("envio_pkey");

    entity.ToTable("envio");

    entity.Property(e => e.Id).HasColumnName("id");
    entity.Property(e => e.ConductorId).HasColumnName("conductor_id");
    entity.Property(e => e.EncomiendaId).HasColumnName("encomienda_id");
    entity.Property(e => e.Fecha).HasMaxLength(20).HasColumnName("fecha");
    entity.Property(e => e.HorarioId).HasColumnName("horario_id");

    entity.HasOne(d => d.Conductor).WithMany(p => p.Envios)
        .HasForeignKey(d => d.ConductorId)
        .HasConstraintName("envio_conductor_id_fkey");

    entity.HasOne(d => d.Encomienda).WithMany(p => p.Envios) // <-- correcto
        .HasForeignKey(d => d.EncomiendaId)
        .HasConstraintName("envio_encomienda_id_fkey");

    entity.HasOne(d => d.Horario).WithMany(p => p.Envios)
        .HasForeignKey(d => d.HorarioId)
        .HasConstraintName("envio_horario_id_fkey");
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
                .HasConstraintName("horario_ruta_id_fkey");

            entity.HasOne(d => d.Vehiculo).WithMany(p => p.Horarios)
                .HasForeignKey(d => d.VehiculoId)
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

        modelBuilder.Entity<Pasaje>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pasaje_pkey");

            entity.ToTable("pasaje");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AsientoId).HasColumnName("asiento_id");
            entity.Property(e => e.ClienteId).HasColumnName("cliente_id");
            entity.Property(e => e.Destino)
                .HasMaxLength(50)
                .HasColumnName("destino");
            entity.Property(e => e.Estado)
                .HasDefaultValue(true)
                .HasColumnName("estado");
            entity.Property(e => e.FechaHora)
                .HasMaxLength(20)
                .HasColumnName("fecha_hora");
            entity.Property(e => e.HorarioId).HasColumnName("horario_id");
            entity.Property(e => e.Monto).HasColumnName("monto");
            entity.Property(e => e.Movil)
                .HasMaxLength(20)
                .HasColumnName("movil");
            entity.Property(e => e.Reserva)
                .HasDefaultValue(false)
                .HasColumnName("reserva");
            entity.Property(e => e.UsuarioAnulaId).HasColumnName("usuario_anula_id");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Asiento).WithMany(p => p.Pasajes)
                .HasForeignKey(d => d.AsientoId)
                .HasConstraintName("fk_pasaje_asiento");

            entity.HasOne(d => d.Cliente).WithMany(p => p.Pasajes)
                .HasForeignKey(d => d.ClienteId)
                .HasConstraintName("pasaje_cliente_id_fkey");

            entity.HasOne(d => d.Horario).WithMany(p => p.Pasajes)
                .HasForeignKey(d => d.HorarioId)
                .HasConstraintName("pasaje_horario_id_fkey");

            entity.HasOne(d => d.UsuarioAnula).WithMany(p => p.PasajeUsuarioAnulas)
                .HasForeignKey(d => d.UsuarioAnulaId)
                .HasConstraintName("pasaje_usuario_anula_id_fkey");

            entity.HasOne(d => d.Usuario).WithMany(p => p.PasajeUsuarios)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("pasaje_usuario_id_fkey");
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
            entity.Property(e => e.Dias)
                .HasMaxLength(50)
                .HasColumnName("dias");
            entity.Property(e => e.Estado)
                .HasDefaultValue(true)
                .HasColumnName("estado");
            entity.Property(e => e.Tarifa).HasColumnName("tarifa");
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
            entity.Property(e => e.Activo).HasColumnName("activo");
            entity.Property(e => e.Aseguradora)
                .HasMaxLength(50)
                .HasColumnName("aseguradora");
            entity.Property(e => e.Color)
                .HasMaxLength(20)
                .HasColumnName("color");
            entity.Property(e => e.ConductorId).HasColumnName("conductor_id");
            entity.Property(e => e.DistribucionId).HasColumnName("distribucion_id");
            entity.Property(e => e.Estado)
                .HasDefaultValue(true)
                .HasColumnName("estado");
            entity.Property(e => e.Foto)
                .HasMaxLength(100)
                .HasColumnName("foto");
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

            entity.HasOne(d => d.Conductor).WithMany(p => p.VehiculoConductors)
                .HasForeignKey(d => d.ConductorId)
                .HasConstraintName("vehiculo_conductor_id_fkey");

            entity.HasOne(d => d.Distribucion).WithMany(p => p.Vehiculos)
                .HasForeignKey(d => d.DistribucionId)
                .HasConstraintName("fk_vehiculo_distribucion");

            entity.HasOne(d => d.Propietario).WithMany(p => p.VehiculoPropietarios)
                .HasForeignKey(d => d.PropietarioId)
                .HasConstraintName("vehiculo_propietario_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
