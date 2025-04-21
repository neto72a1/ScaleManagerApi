using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScaleManager.Models;

public class UserAvailability
{
    public int Id { get; set; }
    [ForeignKey("User")]
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
    public DateTime Date { get; set; }
    public bool IsAvailable { get; set; } = true;
}
