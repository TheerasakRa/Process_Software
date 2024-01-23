namespace Process_Software.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public partial class Status
    {
        public int ID { get; set; }
        public string? StatusName { get; set; }
        public int? CreateBy { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime? CreateDate { get; set; }
        public int? UpdateBy { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime? UpdateDate { get; set; }
        public bool IsDelete { get; set; }

        public virtual ICollection<Work> Work { get; set; }
        public virtual ICollection<WorkLog> WorkLog { get; set; }

    }
}
