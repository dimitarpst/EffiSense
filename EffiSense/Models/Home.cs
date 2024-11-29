public class Home
{
    public int HomeId { get; set; }
    public string UserId { get; set; } 
    public int Size { get; set; }
    public string HeatingType { get; set; }
    public int NumberOfAppliances { get; set; }

//   Полета за площ на дома(за по-точно изчисление на енергийното потребление).
//  Поле за средна температура в дома(което може да бъде полезно при прогнозиране на разходите за отопление/охлаждане).
//  Полета за сезонни корекции в разходите за енергия.

    public virtual ApplicationUser User { get; set; }
    public virtual ICollection<Appliance> Appliances { get; set; }

}


