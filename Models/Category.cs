using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace StoreIRPCAPI.Models;

[Table("categories")]
public partial class Category
{
    [Column("category_id")]
    public int CategoryId { get; set; }

    [Column("category_name")]
    public string CategoryName { get; set; } = null!;

    [Column("category_status")]
    public int CategoryStatus { get; set; }

    [JsonIgnore]
    public virtual ICollection<Product> Products { get; set; } = [];
}
