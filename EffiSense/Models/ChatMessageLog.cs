using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EffiSense.Models
{
    public enum MessageSender
    {
        User,
        Bot
    }

    public class ChatMessageLog
    {
        [Key]
        public int ChatMessageLogId { get; set; }

        [Required]
        public string UserId { get; set; } 

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        [Required]
        public string MessageText { get; set; }

        [Required]
        public MessageSender Sender { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }
    }
}