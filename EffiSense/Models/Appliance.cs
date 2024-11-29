public class Appliance
{
    public int ApplianceId { get; set; }
    public int HomeId { get; set; }
    public string Name { get; set; }
    public double EnergyConsumption { get; set; }
    public bool IsActive { get; set; }
//Полета за възраст на уреда(за да се оцени енергийната ефективност на старите уреди).
//Полета за модел на уреда и енергийна ефективност.
//Полета за историческо потребление на енергия (за анализ и прогнозиране).
//Поле за състояние на уреда(дали е в добро състояние, дали е включен или изключен и др.).

    public virtual Home Home { get; set; }

}
