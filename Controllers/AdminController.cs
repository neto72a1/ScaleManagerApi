using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScaleManager.Data;
using ScaleManager.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ScaleManager.Controllers
{
    [Authorize(Roles = "Admin")] // Apenas usuários com a role "Admin" podem acessar
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userManager.Users.Select(u => new
            {
                u.Id,
                u.UserName,
                u.Email
            }).ToListAsync();
            return Ok(users);
        }

        [HttpGet("ministries")]
        public async Task<IActionResult> GetMinistries()
        {
            var ministries = await _context.Ministries.Select(m => new { m.Id, m.Name }).ToListAsync();
            return Ok(ministries);
        }

        [HttpPost("ministries")]
        public async Task<IActionResult> CreateMinistry([FromBody] CreateMinistryViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingMinistry = await _context.Ministries.FirstOrDefaultAsync(m => m.Name == model.Name);
                if (existingMinistry != null)
                {
                    return BadRequest("Já existe um ministério com este nome.");
                }

                var newMinistry = new Ministry { Name = model.Name };
                _context.Ministries.Add(newMinistry);
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Ministério criado com sucesso.", MinistryId = newMinistry.Id });
            }
            return BadRequest(ModelState);
        }

        [HttpPost("assign-leader")]
        public async Task<IActionResult> AssignLeader([FromBody] AssignLeaderViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                var ministry = await _context.Ministries.FindAsync(model.MinistryId);

                if (user == null)
                {
                    return NotFound($"Usuário com ID '{model.UserId}' não encontrado.");
                }

                if (ministry == null)
                {
                    return NotFound($"Ministério com ID '{model.MinistryId}' não encontrado.");
                }

                // Verificar se o usuário já é líder deste ministério
                var existingAssignment = await _context.UserMinistries
                    .FirstOrDefaultAsync(um => um.UserId == model.UserId && um.MinistryId == model.MinistryId);

                if (existingAssignment != null)
                {
                    return BadRequest("Este usuário já é líder deste ministério.");
                }

                var userMinistry = new UserMinistry { UserId = model.UserId, MinistryId = model.MinistryId };
                _context.UserMinistries.Add(userMinistry);
                await _context.SaveChangesAsync();

                // Adicionar o usuário à role "leader" (se ainda não estiver)
                if (!await _userManager.IsInRoleAsync(user, "leader"))
                {
                    await _userManager.AddToRoleAsync(user, "leader");
                }

                return Ok(new { Message = "Líder de ministério atribuído com sucesso." });
            }
            return BadRequest(ModelState);
        }

        [HttpGet("leaders/{ministryId}")]
        public async Task<IActionResult> GetMinistryLeaders(int ministryId)
        {
            var ministry = await _context.Ministries
                .Include(m => m.UserMinistries)
                    .ThenInclude(um => um.User)
                .FirstOrDefaultAsync(m => m.Id == ministryId);

            if (ministry == null)
            {
                return NotFound($"Ministério com ID '{ministryId}' não encontrado.");
            }

            var leaders = ministry.UserMinistries
                .Where(um => um.User != null)
                .Select(um => new { um.User.Id, um.User.UserName, um.User.Email })
                .ToList();

            return Ok(leaders);
        }

        [HttpPost("remove-leader")]
        public async Task<IActionResult> RemoveLeader([FromBody] AssignLeaderViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                var ministry = await _context.Ministries.FindAsync(model.MinistryId);

                if (user == null)
                {
                    return NotFound($"Usuário com ID '{model.UserId}' não encontrado.");
                }

                if (ministry == null)
                {
                    return NotFound($"Ministério com ID '{model.MinistryId}' não encontrado.");
                }

                var existingAssignment = await _context.UserMinistries
                    .FirstOrDefaultAsync(um => um.UserId == model.UserId && um.MinistryId == model.MinistryId);

                if (existingAssignment == null)
                {
                    return BadRequest("Este usuário não é líder deste ministério.");
                }

                _context.UserMinistries.Remove(existingAssignment);
                await _context.SaveChangesAsync();

                // Verificar se o usuário ainda é líder de outros ministérios
                var isStillLeader = await _context.UserMinistries
                    .AnyAsync(um => um.UserId == model.UserId);

                // Remover o usuário da role "leader" se não for líder de mais nenhum ministério
                if (!isStillLeader && await _userManager.IsInRoleAsync(user, "leader"))
                {
                    await _userManager.RemoveFromRoleAsync(user, "leader");
                }

                return Ok(new { Message = "Líder de ministério removido com sucesso." });
            }
            return BadRequest(ModelState);
        }
    }

   
}
