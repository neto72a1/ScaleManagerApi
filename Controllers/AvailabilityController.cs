using ScaleManager.Data;
using ScaleManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScaleManager.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class AvailabilityController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AvailabilityController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpPost("general")]
    [Authorize(Roles = "leader")]
    public async Task<IActionResult> SetGeneralAvailability([FromBody] SetGeneralAvailabilityRequest request)
    {
        if (request?.Dates == null || !request.MinistryId.HasValue)
        {
            return BadRequest("Datas e MinistryId são obrigatórios.");
        }

        var ministry = await _context.Ministries.FindAsync(request.MinistryId.Value);
        if (ministry == null)
        {
            return NotFound($"Ministério com ID {request.MinistryId.Value} não encontrado.");
        }

        foreach (var date in request.Dates)
        {
            var existing = await _context.ScaleDays.FirstOrDefaultAsync(g =>
                g.Date.Date == date.Date && g.MinistryId == request.MinistryId.Value);

            if (existing == null)
            {
                _context.ScaleDays.Add(new ScaleDay { Date = date.Date, MinistryId = request.MinistryId.Value });
            }
        }
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Disponibilidade geral definida com sucesso para o ministério." });
    }

    [HttpGet("general")]
    public async Task<IActionResult> GetGeneralAvailability([FromQuery] int? ministryId)
    {
        if (!ministryId.HasValue)
        {
            return BadRequest("O parâmetro ministryId é obrigatório.");
        }

        var availability = await _context.ScaleDays
            .Where(g => g.MinistryId == ministryId.Value)
            .OrderBy(g => g.Date)
            .Select(g => g.Date.ToString("yyyy-MM-dd"))
            .ToListAsync();

        return Ok(availability);
    }

    [HttpPost("user")]
    public async Task<IActionResult> SetUserAvailability([FromBody] SetUserAvailabilityRequest request)
    {
        if (request?.Dates == null)
        {
            return BadRequest("As datas são obrigatórias.");
        }

        var userId = _userManager.GetUserId(User);
        var userMinistries = await _context.UserMinistries
            .Where(um => um.UserId == userId)
            .Select(um => um.MinistryId)
            .ToListAsync();

        var allowedScaleDays = await _context.ScaleDays
            .Where(sd => userMinistries.Contains(sd.MinistryId) && request.Dates.Select(d => d.Date).Contains(sd.Date.Date))
            .Select(sd => sd.Date.Date)
            .ToListAsync();

        foreach (var date in request.Dates)
        {
            if (allowedScaleDays.Contains(date.Date))
            {
                var existing = await _context.UserAvailabilities
                    .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.Date.Date == date.Date);

                if (existing == null)
                {
                    _context.UserAvailabilities.Add(new UserAvailability { UserId = userId, Date = date.Date, IsAvailable = true });
                }
                else
                {
                    existing.IsAvailable = true;
                }
            }
            else
            {
                // Optionally log or handle dates the user tried to select that are not general availability
            }
        }

        // Marcar como não disponível as datas que não foram enviadas na requisição atual
        var previousAvailabilities = await _context.UserAvailabilities
            .Where(ua => ua.UserId == userId && ua.IsAvailable)
            .ToListAsync();

        foreach (var existing in previousAvailabilities)
        {
            if (!request.Dates.Any(d => d.Date == existing.Date.Date))
            {
                existing.IsAvailable = false;
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Sua disponibilidade foi atualizada." });
    }

    [HttpGet("user")]
    public async Task<IActionResult> GetUserAvailability()
    {
        var userId = _userManager.GetUserId(User);
        var availability = await _context.UserAvailabilities
            .Where(ua => ua.UserId == userId && ua.IsAvailable)
            .OrderBy(ua => ua.Date)
            .Select(ua => ua.Date.ToString("yyyy-MM-dd"))
            .ToListAsync();
        return Ok(availability);
    }
}

public class SetGeneralAvailabilityRequest
{
    public List<DateTime> Dates { get; set; }
    public int? MinistryId { get; set; }
}

public class SetUserAvailabilityRequest
{
    public List<DateTime> Dates { get; set; }
}

