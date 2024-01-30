using Microsoft.AspNetCore.Mvc;

namespace Process_Software.Models
{
    public class ProviderMetadata
    {
    }
    [ModelMetadataType(typeof(ProviderMetadata))]
    public partial class Provider
    {
        public bool IsEqual(Provider providerCompare)
        {
            var originalProperties = this.GetType().GetProperties();
            var compareProperties = providerCompare.GetType().GetProperties();

            // Iterate over each property in the original model
            foreach (var originalProperty in originalProperties)
            {
                //! Skip Property
                if (originalProperty.Name == "UpdateDate") continue;

                // Get the corresponding property in the copied model
                var compareProperty = compareProperties.FirstOrDefault(p => p.Name == originalProperty.Name);

                // If the property doesn't exist in the copied model, the models are not the same
                if (compareProperty == null)
                {
                    return false;
                }

                //! DateTime Inequality Check if Equal
                if (originalProperty.Name == "CreateDate")
                {
                    var original = originalProperty.GetValue(this);
                    var compare = compareProperty.GetValue(providerCompare);
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
                // Compare the values of the properties
                if (!object.Equals(originalProperty.GetValue(this), compareProperty.GetValue(providerCompare)))
                {
                    return false;
                }
            }

            // If all properties are equal, the models are the same
            return true;
        }
    }
}
