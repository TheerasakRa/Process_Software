namespace Process_Software.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public partial class WorkLog
    {
        public int ID { get; set; }
        public int? WorkID { get; set; }
        public int No { get; set; }
        public string? Project { get; set; }
        public string? Name { get; set; }
        public DateTime? DueDate { get; set; }
        public int? StatusID { get; set; }
        public string? Remark { get; set; }
        public int? CreateBy { get; set; }
        public DateTime? CreateDate { get; set; }
        public int? UpdateBy { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
        public DateTime? UpdateDate { get; set; }
        public bool IsDelete { get; set; }

        public virtual Work Work { get; set; }
        public virtual Status Status { get; set; }
        public virtual ICollection<ProviderLog>? ProviderLog { get; set; }

    }
}
