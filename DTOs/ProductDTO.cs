using System.ComponentModel.DataAnnotations;

namespace StoreIRPCAPI.DTOs;

/// <summary>
/// DTO สำหรับการเพิ่มและอัปเดตสินค้า
/// </summary>
public class ProductDTO
{
    [Required]
    public int? CategoryId { get; set; }
    
    [Required]
    public required string ProductName { get; set; }
    
    [Required]
    public decimal? UnitPrice { get; set; }
    
    [Required]
    public int? UnitInStock { get; set; }
} 