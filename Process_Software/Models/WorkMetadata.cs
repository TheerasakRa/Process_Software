using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Process_Software.Models
{
    public class WorkMetadata
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public int? HeaderID { get; set; }
        public string? Project { get; set; }
        public string? Name { get; set; }
        public int? StatusID { get; set; }
        public string? Remark { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? DueDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
        [DataType(DataType.Date)]
        public DateTime CreateDate { get; set; }
    }
    [MetadataType(typeof(WorkMetadata))]
    public partial class Work
    {
        [NotMapped]
        public string ProvidersID { get; set; }
        [NotMapped]
        public bool? IsSelectAllItem { get; set; }

        [NotMapped]
        public bool? IsSelectEdit { get; set; }
    }
}

