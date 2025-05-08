using System.ComponentModel.DataAnnotations;

public class Home
{
    public int HomeId { get; set; }

    [Required]
    public string UserId { get; set; }

    [Required(ErrorMessage = "House name is required.")]
    [StringLength(100)]
    [Display(Name = "Home Nickname")]
    public string HouseName { get; set; }

    [Required(ErrorMessage = "Size is required.")]
    [Range(1, 10000, ErrorMessage = "Size must be between 1 and 10,000 sq meters.")]
    [Display(Name = "Size (sq. m)")]
    public int Size { get; set; }

    [StringLength(50)]
    [Display(Name = "Heating Type")]
    public string? HeatingType { get; set; } 

    [StringLength(100)]
    public string? Location { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(50)]
    [Display(Name = "Building Type")]
    public string? BuildingType { get; set; } 

    [StringLength(50)]
    [Display(Name = "Insulation Level")]
    public string? InsulationLevel { get; set; }

    [Range(1800, 2100, ErrorMessage = "Please enter a valid year.")] 
    [Display(Name = "Year Built")]
    public int? YearBuilt { get; set; }

    [DataType(DataType.MultilineText)]
    [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters.")]
    public string? Description { get; set; }

    [Required]
    public DateTime LastModified { get; set; }

    public virtual ApplicationUser? User { get; set; }
    public virtual ICollection<Appliance>? Appliances { get; set; }
}
