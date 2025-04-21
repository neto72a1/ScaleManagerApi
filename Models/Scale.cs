using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using ScaleManager.Models;

namespace ScaleManager.Models;

public class Scale
{
    public ScaleDay ScaleDay { get; set; }
    public int ScaleDayId { get; set; } // Adicione a chave estrangeira
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Team { get; set; }
    public List<ApplicationUser> Members { get; set; } = new List<ApplicationUser>();
}
