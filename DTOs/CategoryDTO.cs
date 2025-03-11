using System.ComponentModel.DataAnnotations;

namespace StoreIRPCAPI.DTOs;

public class CategoryDTO
{
    [Required]
    public required string CategoryName { get; set; }
    
    [Required]
    public int CategoryStatus { get; set; }
} 