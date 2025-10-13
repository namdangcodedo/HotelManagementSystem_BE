using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AppBackend.BusinessObjects.Models
{
    [Table("AccountRole")]
    public class AccountRole
    {
        [Required]
        [ForeignKey("Account")]
        public int AccountId { get; set; }
        public virtual Account Account { get; set; } = null!;

        [Required]
        [ForeignKey("Role")]
        public int RoleId { get; set; }
        public virtual Role Role { get; set; } = null!;
    }
}
