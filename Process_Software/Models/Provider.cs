namespace Process_Software.Models
{
    using System;
    public partial class Provider
    {
        public int ID { get; set; }
        public int? WorkID { get; set; }
        public int? UserID { get; set; }
        public int? CreateBy { get; set; }
        public DateTime? CreateDate { get; set; }
        public int? UpdateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
        public bool IsDelete { get; set; }

        public virtual Work Work { get; set; }
        public virtual User User { get; set; }

    }
}
