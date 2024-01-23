namespace Process_Software.Models
{
    public class ProviderLog
    {
        public int ID { get; set; }
        public int? WorkLogID { get; set; }
        public int? UserID { get; set; }
        public int? CreateBy { get; set; }
        public DateTime? CreateDate { get; set; }
        public int? UpdateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
        public bool IsDelete { get; set; }

        public virtual WorkLog WorkLog { get; set; }
        public virtual User User { get; set; }

    }
}
