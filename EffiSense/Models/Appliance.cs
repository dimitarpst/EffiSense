public class Appliance
{
    public int ApplianceId { get; set; }
    public int HomeId { get; set; }
    public string Name { get; set; }
    public double EnergyConsumption { get; set; }
    public bool IsActive { get; set; }

    public virtual Home Home { get; set; }
}
