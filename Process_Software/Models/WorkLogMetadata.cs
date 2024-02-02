using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Composition;

namespace Process_Software.Models
{
    public class WorkLogMetadata
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
    public partial class WorkLog
    {
        [NotMapped]
        public string? LogContent
        {
            get; set;
        }
        [NotMapped]
        public WorkLog nextWorklog { get; set; }
        public void Insert(Process_Software_Context dbContext , Work work)
        {
            this.CreateDate = DateTime.Now;
            this.UpdateDate = DateTime.Now;
            this.IsDelete = work.IsDelete;
            this.CreateBy = GlobalVariable.GetUserID();
            this.UpdateBy = GlobalVariable.GetUserID();
            this.Remark = work.Remark;
            this.DueDate = work.DueDate;
            this.Name = work.Name;
            this.Project = work.Project;
            this.StatusID = work.StatusID;
            this.Status = work.Status;
            this.WorkID = work.ID;
            this.Work = work;
            //dbContext.WorkLog.Add(this);
        }
    }
}