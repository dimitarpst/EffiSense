using System.ComponentModel.DataAnnotations;

public class Appliance
{
    public int ApplianceId { get; set; }

    [Required(ErrorMessage = "Home selection is required.")]
    public int HomeId { get; set; }

    [Required(ErrorMessage = "Appliance name is required.")]
    [StringLength(100, ErrorMessage = "Appliance name cannot be longer than 100 characters.")]
    [Display(Name = "Appliance Name")]
    public string Name { get; set; }

    [StringLength(100, ErrorMessage = "Brand name cannot be longer than 100 characters.")]
    public string? Brand { get; set; }

    [Required(ErrorMessage = "Power rating is required.")]
    [StringLength(50, ErrorMessage = "Power rating cannot be longer than 50 characters.")]
    [Display(Name = "Power Rating (e.g., 1500W, 2kWh/day)")]
    public string PowerRating { get; set; }

    [StringLength(20, ErrorMessage = "Efficiency rating cannot be longer than 20 characters.")]
    [Display(Name = "Efficiency Rating")]
    public string? EfficiencyRating { get; set; }

    [DataType(DataType.MultilineText)]
    [StringLength(2000, ErrorMessage = "Notes cannot be longer than 2000 characters.")]
    public string? Notes { get; set; }

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Display(Name = "Purchase Date")]
    public DateTime? PurchaseDate { get; set; }

    [StringLength(50, ErrorMessage = "Icon class cannot be longer than 50 characters.")]
    [Display(Name = "Icon Class (Font Awesome)")]
    public string? IconClass { get; set; }

    [Required] 
    public DateTime LastModified { get; set; } 

    public virtual Home? Home { get; set; }
}
