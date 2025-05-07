using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Usage
{
    public int UsageId { get; set; }

    [Required]
    public string UserId { get; set; }

    [Required(ErrorMessage = "Appliance selection is required.")]
    [Display(Name = "Appliance")]
    public int ApplianceId { get; set; }

    [Required(ErrorMessage = "Date of usage is required.")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime Date { get; set; }

    [Required(ErrorMessage = "Time of usage is required.")]
    [DataType(DataType.Time)]
    [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
    public DateTime Time { get; set; }

    [Required(ErrorMessage = "Energy used is required.")]
    [Range(0.01, 10000.00, ErrorMessage = "Energy used must be between 0.01 and 10,000 kWh.")]
    [Display(Name = "Energy Used (kWh)")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal EnergyUsed { get; set; }

    [Required(ErrorMessage = "Usage frequency is required.")]
    [Range(1, 5, ErrorMessage = "Frequency must be between 1 (Rarely) and 5 (Always).")]
    [Display(Name = "Usage Frequency")]
    public int UsageFrequency { get; set; }

    [DataType(DataType.MultilineText)]
    [StringLength(300, ErrorMessage = "Context notes cannot be longer than 300 characters.")]
    [Display(Name = "Context/Notes")]
    public string? ContextNotes { get; set; }

    [StringLength(50)]
    [Display(Name = "Icon Class (Optional)")]
    public string? IconClass { get; set; }

    public virtual ApplicationUser? User { get; set; }
    public virtual Appliance? Appliance { get; set; }
}
