public class Appliance
{
    public int ApplianceId { get; set; }
    public int HomeId { get; set; }
    public string Name { get; set; }
    public double EnergyConsumption { get; set; }
    public bool IsActive { get; set; }

    public virtual Home Home { get; set; }
}
public static class EnergyHelper
{
    public static double CalculateEnergy(IEnumerable<Appliance> appliances)
    {
        return appliances.Where(a => a.IsActive).Sum(a => a.EnergyConsumption);
    }
}