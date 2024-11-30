public class Usage
{
    public int UsageId { get; set; }
    public string UserId { get; set; }
    public virtual ApplicationUser User { get; set; }
    public int ApplianceId { get; set; }
    public virtual Appliance Appliance { get; set; }
    public DateTime Date { get; set; } 
    public DateTime Time { get; set; } 
    public double EnergyUsed { get; set; }
    public int UsageFrequency { get; set; } 
}
