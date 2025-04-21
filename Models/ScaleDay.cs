using System;
using System.Collections.Generic;

namespace ScaleManager.Models;

public class ScaleDay
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int MinistryId { get; set; }
    public Ministry Ministry { get; set; }
    public ICollection<Scale> Scales { get; set; }
    // Outras propriedades, como Time (para diferenciar manhã/noite no domingo)
}