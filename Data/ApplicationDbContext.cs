using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StoreIRPCAPI.Models;

namespace StoreIRPCAPI.Data;

public partial class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // เพิ่มส่วนนี้เข้าไป
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("categories_pkey");

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CategoryName).HasMaxLength(128).HasColumnName("category_name");
            entity.Property(e => e.CategoryStatus).HasColumnName("category_status");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("products_pkey");

            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.ProductName).HasMaxLength(50).HasColumnName("product_name");
            entity.Property(e => e.UnitPrice).HasPrecision(18).HasColumnName("unit_price");
            entity.Property(e => e.ProductPicture).HasMaxLength(1024).HasColumnName("product_picture");
            entity.Property(e => e.UnitInStock).HasColumnName("unit_in_stock");
            entity.Property(e => e.CreatedDate).HasColumnType("timestamp without time zone").HasColumnName("created_date");
            entity.Property(e => e.ModifiedDate).HasColumnType("timestamp without time zone").HasColumnName("modified_date");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("products_category_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
