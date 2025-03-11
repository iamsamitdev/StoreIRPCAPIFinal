using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using StoreIRPCAPI.Models;

namespace StoreIRPCAPI.Controllers;

// [ApiExplorerSettings(IgnoreApi = true)] // กำหนดให้ Controller นี้ไม่แสดงใน Swagger
[ApiController]
[Route("api/[controller]")]
public class AuthenticateController(
    UserManager<IdentityUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IConfiguration configuration) : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager = userManager;
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;
    private readonly IConfiguration _configuration = configuration;

    // สร้าง Role ทั้งหมดถ้ายังไม่มี
    private async Task EnsureRolesCreated()
    {
        string[] roles = [UserRoles.User, UserRoles.Manager, UserRoles.Admin];
        
        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    // สร้าง User และเพิ่ม Role
    private async Task<IActionResult> RegisterUserWithRole(RegisterModel model, string role)
    {
        // ตรวจสอบว่ามี username นี้ในระบบแล้วหรือไม่
        var userExist = await _userManager.FindByNameAsync(model.Username);
        if (userExist != null)
        {
            return StatusCode(
                StatusCodes.Status400BadRequest, 
                new Response { 
                    Status = "Error", 
                    Message = "User already exists!" 
                }
            );
        }

        // สร้าง user ใหม่
        IdentityUser user = new()
        {
            Email = model.Email,
            SecurityStamp = Guid.NewGuid().ToString(),
            UserName = model.Username
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        // ถ้าสร้างไม่สำเร็จ
        if (!result.Succeeded)
        {
            return StatusCode(
                StatusCodes.Status400BadRequest, 
                new Response { 
                    Status = "Error", 
                    Message = "User creation failed! Please check user details and try again." 
                }
            );
        }

        // สร้าง Role ทั้งหมดถ้ายังไม่มี
        await EnsureRolesCreated();

        // เพิ่ม User ลงใน Role
        await _userManager.AddToRoleAsync(user, role);

        // สร้าง user สำเร็จ
        return Ok(new Response { 
            Status = "Success", 
            Message = "User created successfully!" 
        });
    }

    // [ApiExplorerSettings(IgnoreApi = true)] 
    // Register for normal user
    // POST: api/authenticate/register-user
    [HttpPost]
    [Route("register-user")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        return await RegisterUserWithRole(model, UserRoles.User);
    }

    // Register for manager
    // POST: api/authenticate/register-manager
    [HttpPost]
    [Route("register-manager")]
    public async Task<IActionResult> RegisterManager([FromBody] RegisterModel model)
    {   
        return await RegisterUserWithRole(model, UserRoles.Manager);
    }

    // Register for admin
    // POST: api/authenticate/register-admin
    [HttpPost]
    [Route("register-admin")]
    public async Task<IActionResult> RegisterAdmin([FromBody] RegisterModel model)
    {
        return await RegisterUserWithRole(model, UserRoles.Admin);
    }

    // Login
    // POST: api/authenticate/login
    [HttpPost]
    [Route("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        if (model.Username == null || model.Password == null)
        {
            return BadRequest(new Response { 
                Status = "Error", 
                Message = "Username and password are required" 
            });
        }

        var user = await _userManager.FindByNameAsync(model.Username);

        // ถ้า login สำเร็จ
        if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
        {
            var userRoles = await _userManager.GetRolesAsync(user);

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var token = GetToken(authClaims);

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo
            });
        }

        // ถ้า login ไม่สำเร็จ
        return Unauthorized(new Response { 
            Status = "Error", 
            Message = "Invalid username or password" 
        });
    }

    // Method for generating JWT token
    private JwtSecurityToken GetToken(List<Claim> authClaims)
    {
        var jwtSecret = _configuration["JWT:Secret"];
        if (string.IsNullOrEmpty(jwtSecret))
        {
            throw new InvalidOperationException("JWT:Secret is not configured");
        }

        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

        var token = new JwtSecurityToken(
            issuer: _configuration["JWT:ValidIssuer"],
            audience: _configuration["JWT:ValidAudience"],
            expires: DateTime.Now.AddHours(3), // หมดอายุใน 3 ชั่วโมง
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );
        return token; 
    }
}