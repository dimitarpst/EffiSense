namespace EffiSense.Models;
public static class EnergyHelper
{
    public static double CalculateEnergy(IEnumerable<Appliance> appliances)
    {
        return appliances.Where(a => a.IsActive).Sum(a => a.EnergyConsumption);
    }
}
