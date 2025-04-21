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
using System.ComponentModel.DataAnnotations.Schema;

namespace ScaleManager.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ScaleController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ScaleController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpPost]
    [Authorize(Roles = "leader")]
    public async Task<IActionResult> CreateScale([FromBody] CreateScaleViewModel model)
    {
        if (model == null)
        {
            return BadRequest("Invalid model data.");
        }

        if (model.MemberIds == null || model.MemberIds.Count == 0)
        {
            return BadRequest("At least one member ID is required.");
        }

        if (model.Date == default(DateTime))
        {
            return BadRequest("Date is required.");
        }

        if (string.IsNullOrEmpty(model.Team))
        {
            return BadRequest("Team is required.");
        }

        // Ensure the ScaleDay exists and is for the correct Ministry.
        var scaleDay = await _context.ScaleDays
            .FirstOrDefaultAsync(sd => sd.Date.Date == model.Date.Date && sd.MinistryId == model.MinistryId);

        if (scaleDay == null)
        {
            return BadRequest("ScaleDay does not exist for the provided date and ministry.");
        }

        // Check for conflicts
        foreach (var memberId in model.MemberIds)
        {
            var existingScale = await _context.Scale
                .AnyAsync(s => s.Date.Date == model.Date.Date && s.Members.Any(m => m.Id == memberId));

            if (existingScale)
            {
                return BadRequest($"User with ID '{memberId}' is already assigned to a scale on this date.");
            }
        }

        var members = await _context.Users.Where(u => model.MemberIds.Contains(u.Id)).ToListAsync();
        var scale = new Scale
        {
            Date = model.Date.Date,
            Team = model.Team,
            Members = members,
            ScaleDay = scaleDay // Associate with ScaleDay
        };

        _context.Scale.Add(scale);
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Escala criada com sucesso." });
    }

    [HttpGet("date/{date}")]
    public async Task<IActionResult> GetScaleByDate(DateTime date)
    {
        var scale = await _context.Scale
            .Where(s => s.Date.Date == date.Date)
            .Include(s => s.Members)
            .Include(s => s.ScaleDay) // Include ScaleDay
            .ToListAsync();

        var scaleByMinistry = scale.Where(s => s.ScaleDay != null && s.ScaleDay.Ministry != null)
            .GroupBy(s => s.ScaleDay.Ministry.Name) // Group by Ministry Name
            .ToDictionary(g => g.Key, g => g.SelectMany(s => s.Members).Select(m => m.Name).Distinct().ToList());

        return Ok(scaleByMinistry);
    }

    [HttpGet("user")]
    public async Task<IActionResult> GetUserScheduledDays()
    {
        var userId = _userManager.GetUserId(User);
        var scheduledDays = await _context.Scale
            .Include(s => s.ScaleDay) // Include ScaleDay
            .Where(s => s.Members.Any(m => m.Id == userId))
            .Select(s => new
            {
                Date = s.Date.ToString("yyyy-MM-dd"),
                Ministry = s.ScaleDay != null && s.ScaleDay.Ministry != null ? s.ScaleDay.Ministry.Name : null, // Include Ministry Name
                //Time = s.ScaleDay.Time // Include Time of day.
            })
            .Distinct()
            .ToListAsync();

        return Ok(scheduledDays);
    }
}

public class CreateScaleViewModel
{
    public DateTime Date { get; set; }
    public string Team { get; set; }
    public List<string> MemberIds { get; set; }
    public int MinistryId { get; set; }
    //public string? Time {get; set;} //Added Time of day
}
