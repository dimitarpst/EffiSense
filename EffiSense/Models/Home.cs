public class Home
{
    public int HomeId { get; set; }
    public string UserId { get; set; } 
    public int Size { get; set; }
    public string HeatingType { get; set; }
    public int NumberOfAppliances { get; set; }

    public virtual ApplicationUser User { get; set; }
    public virtual ICollection<Appliance> Appliances { get; set; }

}


