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
        public bool IsEqual(Work workCompare, bool skipProvider = false)
        {
            bool ProviderResult = skipProvider;

            if (this.Provider != null && workCompare != null && ProviderResult == false)
            {
                List<Provider> originalProvider = this.Provider.ToList();
                List<Provider> compareProvider = workCompare.Provider.ToList();
                if (originalProvider.Count == compareProvider.Count)
                {
                    for (int i = 0; i < originalProvider.Count; i++)
                    {
                        //! Loop all Provider if not Equal will be false
                        ProviderResult = originalProvider.ToList()[i].IsEqual(compareProvider.ToList()[i]);
                        if (!ProviderResult) break;
                    }
                }
            }
            if (!ProviderResult) return false;

            var originalProperties = this.GetType().GetProperties();
            var compareProperties = workCompare.GetType().GetProperties();

            foreach (var originalProperty in originalProperties)
            {
                if (originalProperty.Name == "Provider") continue;
                if (originalProperty.Name == "UpdateDate") continue;
                if (originalProperty.Name == "WorkLog") continue;

                var compareProperty = compareProperties.FirstOrDefault(p => p.Name == originalProperty.Name);

                if (compareProperty == null)
                {
                    return false;
                }

                if (originalProperty.Name == "CreateDate")
                {
                    var original = originalProperty.GetValue(this);
                    var compare = compareProperty.GetValue(workCompare);
                    DateTime? originalDateTime = original as DateTime?;
                    DateTime? compareDateTime = compare as DateTime?;
                    if (originalDateTime != null && compareDateTime != null)
                    {
                        if (originalDateTime.Value.Hour != compareDateTime.Value.Hour)
                        {
                            return false;
                        }
                        if (originalDateTime.Value.Minute != compareDateTime.Value.Minute)
                        {
                            return false;
                        }
                        if (originalDateTime.Value.Second != compareDateTime.Value.Second)
                        {
                            return false;
                        }
                    }
                    continue;
                }

                if (!object.Equals(originalProperty.GetValue(this), compareProperty.GetValue(workCompare)))
                {
                    return false;
                }
            }
            return true;
        }
    }
}

