using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreIRPCAPI.Data;
using StoreIRPCAPI.DTOs;
using StoreIRPCAPI.Models;

namespace StoreIRPCAPI.Controllers;

// [Authorize] // กำหนดให้ต้องมีการ Login ก่อนเข้าถึง API ทั้งหมด
// [Authorize(Roles = UserRoles.Admin)] // กำหนดให้เฉพาะ Admin เท่านั้นที่สามารถเข้าถึง API นี้ได้
// [Authorize(Roles = $"{UserRoles.Manager},{UserRoles.Admin}")] // กำหนดให้ Manager และ Admin เท่านั้นที่สามารถเข้าถึง API นี้ได้
[ApiController]
[Route("api/[controller]")]
public class ProductController(ApplicationDbContext context, IWebHostEnvironment env) : ControllerBase
{
    // สร้าง Object ของ ApplicationDbContext
    private readonly ApplicationDbContext _context = context;
    
    // IWebHostEnvironment คืออะไร
    // IWebHostEnvironment เป็นอินเทอร์เฟซใน ASP.NET Core ที่ใช้สำหรับดึงข้อมูลเกี่ยวกับสภาพแวดล้อมการโฮสต์เว็บแอปพลิเคชัน
    // ContentRootPath: เส้นทางไปยังโฟลเดอร์รากของเว็บแอปพลิเคชัน
    // WebRootPath: เส้นทางไปยังโฟลเดอร์ wwwroot ของเว็บแอปพลิเคชัน
    private readonly IWebHostEnvironment _env = env;

    [AllowAnonymous] // กำหนดให้สามารถเข้าถึง API ทั้งหมดได้
    // ทดสอบเขียนฟังก์ชันการเชื่อมต่อ database
    // GET: /api/Product/testconnectdb
    [HttpGet("testconnectdb")]
    public void TestConnection()
    {
        // ถ้าเชื่อมต่อได้จะแสดงข้อความ "Connected"
        if (_context.Database.CanConnect())
        {
            Response.WriteAsync("Connected");
        }
        // ถ้าเชื่อมต่อไม่ได้จะแสดงข้อความ "Not Connected"
        else
        {
            Response.WriteAsync("Not Connected");
        }
    }

    // ฟังก์ชันสำหรับการดึงข้อมูลสินค้าทั้งหมด
    // GET: /api/Product
    [HttpGet]
    public ActionResult<Product> GetProducts([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string? pname = null, [FromQuery] int? categoryId = null)
    {
        int skip = (page - 1) * limit;

        // แบบเชื่อมกับตารางอื่น products เชื่อมกับ categories
        var query = _context.Products
            .Join(
                _context.Categories,
                p => p.CategoryId,
                c => c.CategoryId,
                (p, c) => new
                {
                    p.ProductId,
                    p.ProductName,
                    p.UnitPrice,
                    p.UnitInStock,
                    p.ProductPicture,
                    p.CreatedDate,
                    p.ModifiedDate,
                    p.CategoryId,
                    c.CategoryName
                }
            );
        
        // ค้นหาตามชื่อสินค้า
        if (!string.IsNullOrEmpty(pname))
        {
            query = query.Where(p => EF.Functions.ILike(p.ProductName!, $"%{pname}%"));
        }

        // กรองตามหมวดหมู่
        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        // นับจำนวนรายการทั้งหมดหลังจากกรอง
        var totalRecords = query.Count();

        // เรียงลำดับและแบ่งหน้า
        var products = query
            .OrderByDescending(p => p.ProductId)
            .Skip(skip)
            .Take(limit)
            .ToList();

        // ส่งข้อมูลกลับไปให้ผู้ใช้งาน
        return Ok(new { Total = totalRecords, Products = products });
    }

    // ฟังก์ชันสำหรับการดึงข้อมูลสินค้าตาม id
    // GET: /api/Product/{id}
    [HttpGet("{id}")]
    public ActionResult<Product> GetProduct(int id)
    {
        // แบบเชื่อมกับตารางอื่น products เชื่อมกับ categories
        var product = _context.Products
            .Join(
                _context.Categories,
                p => p.CategoryId,
                c => c.CategoryId,
                (p, c) => new
                {
                    p.ProductId,
                    p.ProductName,
                    p.UnitPrice,
                    p.UnitInStock,
                    p.ProductPicture,
                    p.CreatedDate,
                    p.ModifiedDate,
                    p.CategoryId,
                    c.CategoryName
                }
            )
            .FirstOrDefault(p => p.ProductId == id);

        // ถ้าไม่พบข้อมูลจะแสดงข้อความ Not Found
        if (product == null)
        {
            return NotFound();
        }

        // ส่งข้อมูลกลับไปให้ผู้ใช้งาน
        return Ok(product);
    }

    // ฟังก์ชันสำหรับการเพิ่มข้อมูลสินค้า
    // POST: /api/Product
    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct([FromForm] ProductDTO productDTO, IFormFile? image)
    {
        // สร้าง object product จาก productDTO
        var product = new Product
        {
            CategoryId = productDTO.CategoryId,
            ProductName = productDTO.ProductName,
            UnitPrice = productDTO.UnitPrice,
            UnitInStock = productDTO.UnitInStock,
            CreatedDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),
            ModifiedDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
        };

        // ตรวจสอบว่ามีการอัพโหลดไฟล์รูปภาพหรือไม่
        if (image != null)
        {
            // กำหนดชื่อไฟล์รูปภาพใหม่
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);

            // บันทึกไฟล์รูปภาพ
            string uploadFolder;
            
            // ตรวจสอบว่า WebRootPath มีค่าหรือไม่
            if (string.IsNullOrEmpty(_env.WebRootPath))
            {
                // ถ้า WebRootPath เป็น null ให้ใช้ ContentRootPath แทน
                uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            }
            else
            {
                uploadFolder = Path.Combine(_env.WebRootPath, "uploads");
            }

            // ตรวจสอบว่าโฟลเดอร์ uploads มีหรือไม่
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            using (var fileStream = new FileStream(Path.Combine(uploadFolder, fileName), FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            // บันทึกชื่อไฟล์รูปภาพลงในฐานข้อมูล
            product.ProductPicture = fileName;
        }
        else
        {
            product.ProductPicture = "noimg.jpg";
        }

        // เพิ่มข้อมูลลงในตาราง Products
        _context.Products.Add(product);
        _context.SaveChanges();

        // ส่งข้อมูลกลับไปให้ผู้ใช้
        return Ok(product);
    }

    // ฟังก์ชันสำหรับการแก้ไขข้อมูลสินค้า
    // PUT: /api/Product/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<Product>> UpdateProduct(int id, [FromForm] ProductDTO productDTO, IFormFile? image)
    {
        // ดึงข้อมูลสินค้าตาม id
        var existingProduct = _context.Products.FirstOrDefault(p => p.ProductId == id);

        // ถ้าไม่พบข้อมูลจะแสดงข้อความ Not Found
        if (existingProduct == null)
        {
            return NotFound();
        }

        // แก้ไขข้อมูลสินค้า
        existingProduct.ProductName = productDTO.ProductName;
        existingProduct.UnitPrice = productDTO.UnitPrice;
        existingProduct.UnitInStock = productDTO.UnitInStock;
        existingProduct.CategoryId = productDTO.CategoryId;
        existingProduct.ModifiedDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

        // ตรวจสอบว่ามีการอัพโหลดไฟล์รูปภาพหรือไม่
        if (image != null)
        {
            // กำหนดชื่อไฟล์รูปภาพใหม่
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);

            // บันทึกไฟล์รูปภาพ
            string uploadFolder;
            
            // ตรวจสอบว่า WebRootPath มีค่าหรือไม่
            if (string.IsNullOrEmpty(_env.WebRootPath))
            {
                // ถ้า WebRootPath เป็น null ให้ใช้ ContentRootPath แทน
                uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            }
            else
            {
                uploadFolder = Path.Combine(_env.WebRootPath, "uploads");
            }

            // ตรวจสอบว่าโฟลเดอร์ uploads มีหรือไม่
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            using (var fileStream = new FileStream(Path.Combine(uploadFolder, fileName), FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            // ลบไฟล์รูปภาพเดิม ถ้ามีการอัพโหลดรูปภาพใหม่ และรูปภาพเดิมไม่ใช่ noimg.jpg
            if (existingProduct.ProductPicture != "noimg.jpg" && existingProduct.ProductPicture != null)
            {
                string oldImagePath = Path.Combine(uploadFolder, existingProduct.ProductPicture);
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            // บันทึกชื่อไฟล์รูปภาพลงในฐานข้อมูล
            existingProduct.ProductPicture = fileName;
        }

        // บันทึกข้อมูล
        _context.SaveChanges();

        // ส่งข้อมูลกลับไปให้ผู้ใช้
        return Ok(existingProduct);
    }

    // ฟังก์ชันสำหรับการลบข้อมูลสินค้า
    // DELETE: /api/Product/{id}
    [HttpDelete("{id}")]
    public ActionResult<Product> DeleteProduct(int id)
    {
        // ดึงข้อมูลสินค้าตาม id
        var product = _context.Products.FirstOrDefault(p => p.ProductId == id);

        // ถ้าไม่พบข้อมูลจะแสดงข้อความ Not Found
        if (product == null)
        {
            return NotFound();
        }

        // ตรวจสอบว่ามีไฟล์รูปภาพหรือไม่
        if (product.ProductPicture != "noimg.jpg" && product.ProductPicture != null)
        {
            string uploadFolder;
            
            // ตรวจสอบว่า WebRootPath มีค่าหรือไม่
            if (string.IsNullOrEmpty(_env.WebRootPath))
            {
                // ถ้า WebRootPath เป็น null ให้ใช้ ContentRootPath แทน
                uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            }
            else
            {
                uploadFolder = Path.Combine(_env.WebRootPath, "uploads");
            }
            
            string imagePath = Path.Combine(uploadFolder, product.ProductPicture);
            
            // ลบไฟล์รูปภาพ
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }
        }

        // ลบข้อมูล
        _context.Products.Remove(product);
        _context.SaveChanges();

        // ส่งข้อมูลกลับไปให้ผู้ใช้
        return Ok(product);
    }
}