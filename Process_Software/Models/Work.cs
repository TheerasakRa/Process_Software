
namespace Process_Software.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public partial class Work
    {
        public Work()
        {
            this.Provider = new HashSet<Provider>();
            this.WorkLog = new HashSet<WorkLog>();

        }
        public int ID { get; set; }
        public int? HeaderID { get; set; }
        public string? Project { get; set; }
        public string? Name { get; set; }
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }
        [Required]
        public int? StatusID { get; set; }
        public string? Remark { get; set; }
        public int? CreateBy { get; set; }
        [DataType(DataType.Date)]

        public DateTime? CreateDate { get; set; }
        public int? UpdateBy { get; set; }
        [DataType(DataType.Date)]

        public DateTime? UpdateDate { get; set; }
        public bool IsDelete { get; set; }

        public virtual Status Status { get; set; }
        public virtual ICollection<Provider> Provider { get; set; }
        public virtual ICollection<WorkLog> WorkLog { get; set; }

    }
}
