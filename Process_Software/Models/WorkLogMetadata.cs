using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Composition;

namespace Process_Software.Models
{
    public class WorkLogMetadata
    {
    }
    [MetadataType(typeof(WorkLogMetadata))]
    public partial class WorkLog
    {
        [NotMapped]
        public string? LogContent
        {
            get; set;
        }
        [NotMapped]
        public WorkLog nextWorklog { get; set; }
    }
}