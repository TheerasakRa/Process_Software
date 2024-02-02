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
        public string? Password { get; set; }
        public string? Email { get; set; }
        public string? LineID { get; set; }
        public string? Role { get; set; }
        public int? CreateBy { get; set; }
        public DateTime? CreateDate { get; set; }
        public int? UpdateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
        public bool IsDelete { get; set; }


        public virtual ICollection<Provider> Provider { get; set; }
        public virtual ICollection<ProviderLog> ProviderLog { get; set; }
    }
}
