using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Process_Software.Models;
using System.ComponentModel.DataAnnotations;

namespace Process_Software.Models
{
    public partial class User
    {
        [ValidateNever]
        public int ID { get; set; }
        public string? Name { get; set; }
        [Required]
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }
        [Required]
        [Display(Name = "Email")]
        [DataType(DataType.EmailAddress)]
        public string? Email { get; set; }
        public string? LineID { get; set; }
        public string? Role { get; set; }
        public int? CreateBy { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime? CreateDate { get; set; }
        public int? UpdateBy { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime? UpdateDate { get; set; }
        public bool IsDelete { get; set; }


        public virtual ICollection<Provider> Provider { get; set; }
        public virtual ICollection<ProviderLog> ProviderLog { get; set; }
    }
}
