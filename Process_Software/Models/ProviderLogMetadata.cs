using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Process_Software.Models
{
    public class ProviderLogMetadata
    {
        [Display(Name = "Create Date")]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}")]
        [DataType(DataType.Date)]
        public DateTime CreateDate { get; set; }
        [Display(Name = "Update Date")]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}")]
        [DataType(DataType.Date)]
        public DateTime? UpdateDate { get; set; }
    }
    [ModelMetadataType(typeof(WorkLogMetadata))]
    public partial class ProviderLog
    {
        public void Insert(Process_Software_Context dbContext, WorkLog workLog)
        {
            this.CreateDate = DateTime.Now;
            this.UpdateDate = DateTime.Now;
            this.CreateBy = GlobalVariable.GetUserID();
            this.UpdateBy = GlobalVariable.GetUserID();
            this.WorkLogID = workLog.ID;
            this.WorkLog = workLog;
            //dbContext.ProviderLog.Add(this);
        }
    }
}
