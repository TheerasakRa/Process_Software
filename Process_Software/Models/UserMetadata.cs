using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Process_Software.Models
{
    public class UserMetadata
    {
        [Required]
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

    }
    [MetadataType(typeof(UserMetadata))]
    public partial class User
    {
        [Display(Name = "Remember me")]
        [NotMapped]
        public bool RememberMe { get; set; }

        [Required]

        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        [DataType(DataType.Password)]
        [NotMapped]
        public string ConfirmPassword { get; set; }

    }
}
