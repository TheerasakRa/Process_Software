using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Process_Software.Models
{
    public class ProviderMetadata
    {
    }
    [ModelMetadataType(typeof(ProviderMetadata))]
    public partial class Provider
    {
        public void Insert(Process_Software_Context dbContext)
        {
            this.CreateDate = DateTime.Now;
            this.UpdateDate = DateTime.Now;
            this.IsDelete = false;
            this.CreateBy = GlobalVariable.GetUserID();
            this.UpdateBy = GlobalVariable.GetUserID();
            dbContext.Provider.Add(this);
        }
        public void Update(Process_Software_Context dbContext)
        {
            if (this.CreateDate == null)
            {
                this.CreateDate = DateTime.Now;
            }
            this.UpdateDate = DateTime.Now;
            var existingEntity = dbContext.Provider.Find(this.ID);
            dbContext.Entry(existingEntity).CurrentValues.SetValues(this);
        }
        public void Delete(Process_Software_Context dbContext)
        {
            var data = dbContext.Provider.Find(this.ID);

            data.UpdateBy = GlobalVariable.GetUserID();
            data.UpdateDate = DateTime.Now;
            data.IsDelete = true;
            dbContext.Entry(data).State = EntityState.Modified;
        }
    }
}
