using ScaleManager.Data;
using ScaleManager.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ScaleManager.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context; // Inject ApplicationDbContext

    public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration, ApplicationDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _context = context;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                Name = model.Name,
                Phone = model.Phone,
                Birthday = model.Birthday
            };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                if (model.Ministries != null && model.Ministries.Any())
                {
                    foreach (var ministryInfo in model.Ministries)
                    {
                        user.Ministries.Add(new MinistryAssignment
                        {
                            Ministry = ministryInfo.Ministry,
                            Functions = ministryInfo.Functions
                        });
                    }
                    await _context.SaveChangesAsync(); // Save the ministries
                }

                return StatusCode(201, new { Message = "Registro realizado com sucesso." });
            }
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
        }
        return BadRequest(ModelState);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, isPersistent: false, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                var roles = await _userManager.GetRolesAsync(user);
                var token = GenerateJwtToken(user, roles);
                return Ok(new { Token = token, UserId = user.Id, Roles = roles });
            }
            return Unauthorized(new { Message = "Credenciais inválidas." });
        }
        return BadRequest(ModelState);
    }

    private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
    {
        var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim("phone", user.Phone ?? ""), // Adicione o telefone como um claim
                new Claim("birthday", user.Birthday?.ToString("yyyy-MM-dd") ?? "") // Adicione o aniversário como um claim
            };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.Now.AddMinutes(int.Parse(_configuration["JwtSettings:ExpirationMinutes"]));

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class RegisterViewModel
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public DateTime? Birthday { get; set; }
    public string Password { get; set; }
    public List<MinistryInfo> Ministries { get; set; }
}

public class MinistryInfo
{
    public string Ministry { get; set; }
    public List<string> Functions { get; set; }
}

public class LoginViewModel
{
    public string Email { get; set; }
    public string Password { get; set; }
}


