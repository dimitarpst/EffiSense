using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public bool IsSimulationEnabled { get; set; } = false;
    public int SelectedSimulationInterval { get; set; } = 0;
}
