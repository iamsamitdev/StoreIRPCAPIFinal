using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using StoreIRPCAPI.Converters;

namespace StoreIRPCAPI.Models;

[Table("products")]
public partial class Product
{
    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("category_id")]
    public int? CategoryId { get; set; }

    [Column("product_name")]
    public string? ProductName { get; set; }

    [Column("unit_price")]
    public decimal? UnitPrice { get; set; }

    [Column("product_picture")]
    public string? ProductPicture { get; set; }

    [Column("unit_in_stock")]
    public int? UnitInStock { get; set; }

    [Column("created_date")]
    [JsonConverter(typeof(DateTimeWithoutTimeZoneConverter))]
    public DateTime? CreatedDate { get; set; }

    [Column("modified_date")]
    [JsonConverter(typeof(DateTimeWithoutTimeZoneConverter))]
    public DateTime? ModifiedDate { get; set; }

    [JsonIgnore]
    public virtual Category? Category { get; set; }
}
