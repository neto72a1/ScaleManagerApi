using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System;

namespace ScaleManager.Models;

public class ApplicationUser : IdentityUser
{
    public string Name { get; set; }
    public string Phone { get; set; }
    public DateTime? Birthday { get; set; }
    public List<MinistryAssignment> Ministries { get; set; } = new List<MinistryAssignment>();
    public List<string> Roles { get; set; } = new List<string>();
}

public class MinistryAssignment
{
    public int Id { get; set; } // Adicione um ID se precisar no banco de dados
    public string Ministry { get; set; }
    public List<string> Functions { get; set; } = new List<string>();
    public string ApplicationUserId { get; set; } // Foreign key para ApplicationUser
    public ApplicationUser ApplicationUser { get; set; }
}
