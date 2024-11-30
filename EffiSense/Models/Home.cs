public class Home
{
    public int HomeId { get; set; }
    public string UserId { get; set; }
    public string HouseName { get; set; }
    public int Size { get; set; }
    public string HeatingType { get; set; } 
    public string Location { get; set; } 
    public string Address { get; set; } 
    public string BuildingType { get; set; }
    public double InsulationLevel { get; set; }
    public virtual ApplicationUser User { get; set; }
    public virtual ICollection<Appliance> Appliances { get; set; }
}