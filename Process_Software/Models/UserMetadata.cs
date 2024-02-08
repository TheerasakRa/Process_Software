using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;
using Process_Software.Models;

namespace Process_Software.Models
{
    public class UserMetadata
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
        [Display(Name = "LineID")]
        public string? LineID { get; set; }
        [Display(Name = "Role")]
        public string? Role { get; set; }
        public int? CreateBy { get; set; }

        [Display(Name = "Create Date")]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
        [DataType(DataType.DateTime)]
        public DateTime? CreateDate { get; set; }
        public int? UpdateBy { get; set; }

        [Display(Name = "Update Date")]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
        [DataType(DataType.DateTime)]
        public DateTime? UpdateDate { get; set; }
        public bool IsDelete { get; set; }
        

    }
    [ModelMetadataType(typeof(UserMetadata))]
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

        [NotMapped]
        public string TempSessionToken { get; set; }
        public async Task InsertAsync(Process_Software_Context dbContext)
        {
            this.Password = await HashingHelpers.HashPasswordAsync(this.Password);
            this.CreateDate = DateTime.Now;
            this.UpdateDate = DateTime.Now;
            this.IsDelete = false;
            dbContext.User.Add(this);
        }

        public async Task Update(Process_Software_Context dbContext)
        {
            this.Password = await HashingHelpers.HashPasswordAsync(this.Password);
            this.UpdateDate = DateTime.Now;
            var existingEntity = dbContext.User.Find(this.ID);
            dbContext.Entry(existingEntity).CurrentValues.SetValues(this);
        }

        public void Delete(Process_Software_Context dbContext)
        {
            var data = dbContext.User.Find(this.ID);

            data.UpdateBy = GlobalVariable.GetUserID();
            data.UpdateDate = DateTime.Now;
            data.IsDelete = true;
            dbContext.Entry(data).State = EntityState.Modified;
        }
    }
}
