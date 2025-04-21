namespace ScaleManager.Models;

public class Ministry
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<UserMinistry> UserMinistries { get; set; }
    public ICollection<ScaleDay> ScaleDays { get; set; }
    // Outras propriedades, como LeaderId
}
