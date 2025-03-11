using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreIRPCAPI.Data;
using StoreIRPCAPI.DTOs;
using StoreIRPCAPI.Models;

namespace StoreIRPCAPI.Controllers;

// [AllowAnonymous] // กำหนดให้สามารถเข้าถึง API ทั้งหมดได้
// [Authorize] // กำหนดให้ต้องมีการ Login ก่อนเข้าถึง API ทั้งหมด
// [Authorize(Roles = $"{UserRoles.Manager},{UserRoles.Admin}")] // กำหนดให้ Manager และ Admin เท่านั้นที่สามารถเข้าถึง API นี้ได้
[ApiController]
[Route("api/[controller]")]
public class CategoryController(ApplicationDbContext context) : ControllerBase
{
    // สร้าง Object ของ ApplicationDbContext
    private readonly ApplicationDbContext _context = context;

    // ฟังก์ชันสำหรับการดึงข้อมูล Category ทั้งหมด
    // GET: /api/Category
    [HttpGet]
    public ActionResult<Category> GetCategories([FromQuery] string? name)
    {
        // สร้าง query สำหรับดึงข้อมูลจากตาราง Categories
        var query = _context.Categories.AsQueryable();

        // ถ้ามีการส่งชื่อหมวดหมู่มาค้นหา
        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(c => EF.Functions.ILike(c.CategoryName, $"%{name}%"));
        }

        // เรียงลำดับตาม CategoryId จากมากไปน้อย
        query = query.OrderByDescending(c => c.CategoryId);

        // ส่งข้อมูลกลับไปให้ผู้ใช้งาน
        return Ok(query);
    }

    // ฟังก์ชันสำหรับการดึงข้อมูล Category ตาม id
    // GET: /api/Category/{id}
    [HttpGet("{id}")]
    public ActionResult<Category> GetCategory(int id)
    {
        // ดึงข้อมูลจากตาราง Categories ตาม id
        var category = _context.Categories.FirstOrDefault(c => c.CategoryId == id);

        // ถ้าไม่พบข้อมูลจะแสดงข้อความ Not Found
        if (category == null)
        {
            return NotFound();
        }

        // ส่งข้อมูลกลับไปให้ผู้ใช้งาน
        return Ok(category);
    }

    // ฟังก์ชันสำหรับการเพิ่มข้อมูล Category
    // POST: /api/Category
    [HttpPost]
    public ActionResult<Category> CreateCategory(CategoryDTO categoryDTO)
    {
        // สร้าง object category จาก categoryDTO
        var category = new Category
        {
            CategoryName = categoryDTO.CategoryName,
            CategoryStatus = categoryDTO.CategoryStatus
        };

        // เพิ่มข้อมูลลงในตาราง Categories
        _context.Categories.Add(category);
        _context.SaveChanges();

        // ส่งข้อมูลกลับไปให้ผู้ใช้
        return Ok(category);
    }

    // ฟังก์ชันสำหรับการแก้ไขข้อมูล Category
    // PUT: /api/Category/{id}
    [HttpPut("{id}")]
    public ActionResult<Category> UpdateCategory(int id, CategoryDTO categoryDTO)
    {
        // ดึงข้อมูล Category ตาม id
        var existingCategory = _context.Categories.FirstOrDefault(c => c.CategoryId == id);

        // ถ้าไม่พบข้อมูลจะแสดงข้อความ Not Found
        if (existingCategory == null)
        {
            return NotFound();
        }

        // แก้ไขข้อมูล Category
        existingCategory.CategoryName = categoryDTO.CategoryName;
        existingCategory.CategoryStatus = categoryDTO.CategoryStatus;

        // บันทึกข้อมูล
        _context.SaveChanges();

        // ส่งข้อมูลกลับไปให้ผู้ใช้
        return Ok(existingCategory);
    }

    // ฟังก์ชันสำหรับการลบข้อมูล Category
    // DELETE: /api/Category/{id}
    [HttpDelete("{id}")]
    public ActionResult<Category> DeleteCategory(int id)
    {
        // ดึงข้อมูล Category ตาม id
        var category = _context.Categories.FirstOrDefault(c => c.CategoryId == id);

        // ถ้าไม่พบข้อมูลจะแสดงข้อความ Not Found
        if (category == null)
        {
            return NotFound();
        }

        // ลบข้อมูล
        _context.Categories.Remove(category);
        _context.SaveChanges();

        // ส่งข้อมูลกลับไปให้ผู้ใช้
        return Ok(category);
    }
} 