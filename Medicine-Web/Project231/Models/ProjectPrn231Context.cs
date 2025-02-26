using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Project231.Models;

public partial class ProjectPrn231Context : DbContext
{
  private readonly IConfiguration _configuration;

    public ProjectPrn231Context(DbContextOptions<ProjectPrn231Context> options, IConfiguration configuration)
        : base(options)
    {
        _configuration = configuration;
    }


    public ProjectPrn231Context(DbContextOptions<ProjectPrn231Context> options)
        : base(options)
    {
    }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<Medicine> Medicines { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = _configuration.GetConnectionString("MyDb");
            optionsBuilder.UseSqlServer(connectionString);
        }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Cart__3214EC27E87E3BEB");

            entity.ToTable("Cart");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Discount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MedicineId).HasColumnName("MedicineID");
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Medicine).WithMany(p => p.Carts)
                .HasForeignKey(d => d.MedicineId)
                .HasConstraintName("FK_Cart_Medicines");

            entity.HasOne(d => d.User).WithMany(p => p.Carts)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Cart_Users");
        });

        modelBuilder.Entity<Medicine>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Medicine__3214EC2741E9744D");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Discount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ExpDate).HasColumnType("datetime");
            entity.Property(e => e.ImageUrl).HasColumnType("text");
            entity.Property(e => e.Manufacturer).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Order__3214EC2752DDD0C1");

            entity.ToTable("Order");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.OrderDate).HasColumnType("datetime");
            entity.Property(e => e.OrderNo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.OrderTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Order_Users");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OrderIte__3214EC276D367985");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Discount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MedicineId).HasColumnName("MedicineID");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Medicine).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.MedicineId)
                .HasConstraintName("FK__OrderItem__Medic__2D27B809");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__OrderItem__Order__2C3393D0");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC27C726FFF4");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Fund).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Password)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Type)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
