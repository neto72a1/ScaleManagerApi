namespace ScaleManager.Models;

public class UserMinistry
{
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
    public int MinistryId { get; set; }
    public Ministry Ministry { get; set; }
}
