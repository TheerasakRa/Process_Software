using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Process_Software.Common;
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
        [Required]
        public int? StatusID { get; set; }
        [Display(Name = "Remark")]
        public string? Remark { get; set; }
        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime? DueDate { get; set; }
        [Display(Name = "Create Date")]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}")]
        [DataType(DataType.Date)]
        public DateTime CreateDate { get; set; }
        [Display(Name = "Update Date")]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}")]
        [DataType(DataType.Date)]
        public DateTime? UpdateDate { get; set; }
    }
    [ModelMetadataType(typeof(WorkMetadata))]
    public partial class Work
    {
        [NotMapped]
        public string ProvidersID { get; set; }
        [NotMapped]
        public bool? IsSelectAllItem { get; set; }
        [NotMapped]
        public bool? IsSelectEdit { get; set; }
        [NotMapped]
        public bool IsFound { get; set; }
        [NotMapped]

        public string DefaultStatus
        {
            get { return "Waiting Plan"; }
        }
        [NotMapped]

        public string WaitingStatus
        {
            get
            {
                return "Waiting";
            }
        }

        public void Insert(Process_Software_Context dbContext)
        {
            Status defaultStatus = dbContext.Status.FirstOrDefault(s => s.StatusName == this.DefaultStatus);
            Status waitingStatus = dbContext.Status.FirstOrDefault(s => s.StatusName == this.WaitingStatus);

            // กำหนดสถานะขึ้นอยู่กับ DueDate
            // ถ้าไม่เลือก ให้กำหนดค่าเป็น defaultStatus
            if (this.DueDate == null && defaultStatus != null)
            {
                this.StatusID = defaultStatus.ID;
            }
            // ถ้าเลือก ให้กำหนดค่าเป็น waitingStatus
            else if (this.DueDate != null && waitingStatus != null)
            {
                this.StatusID = waitingStatus.ID;
            }
            else
            {
                this.StatusID = null;
            }
            if (this.ID == 0)
            {

                this.CreateDate = DateTime.Now;
                this.UpdateDate = DateTime.Now;
                this.IsDelete = false;
                this.CreateBy = GlobalVariable.GetUserID();
                this.UpdateBy = GlobalVariable.GetUserID();

                this.Provider = new List<Provider>();
                //ถ้าเลือกทั้งหมด
                if (this.IsSelectAllItem == true)
                {
                    foreach (User user in dbContext.User)
                    {
                        Provider provider = new Provider()
                        {
                            UserID = user.ID,
                            //WorkID = work.ID,
                        };
                        provider.Insert(dbContext);
                        this.Provider.Add(provider);
                    }
                }
                else
                {
                    // ถ้าไม่ได้เลือกทุก Provider
                    if (this.ProvidersID == null)
                    {
                        foreach (Provider item in this.Provider)
                        {
                            Provider provider = new Provider();
                            provider.Insert(dbContext);
                            this.Provider.Add(provider);
                        }
                    }
                    else
                    {
                        //ถ้าเลือกแค่บางตัว คั่นด้วย ,
                        string[] providerlist = this.ProvidersID.Split(',');

                        foreach (string item in providerlist)
                        {
                            Provider provider = new Provider()
                            {
                                UserID = Convert.ToInt32(item)
                            };
                            provider.Insert(dbContext);
                            //db.Provider.Add(provider);
                            this.Provider.Add(provider);
                        }
                    }

                }

            }
            this.ProcessProviders(dbContext);
            dbContext.Work.Add(this);
        }

        public void Update(Process_Software_Context dbContext)
        {
            this.InsertValue(dbContext);
            this.ProcessProviders(dbContext);
            dbContext.Entry(this).State = EntityState.Modified;
            this.UpdateDate = DateTime.Now;
            this.UpdateBy = GlobalVariable.GetUserID();
            Work existingEntity = dbContext.Work.Find(this.ID);
            dbContext.Entry(existingEntity).CurrentValues.SetValues(this);
        }
        public void Delete(Process_Software_Context dbContext)
        {
            Work data = dbContext.Work.Find(this.ID);

            data.UpdateBy = GlobalVariable.GetUserID();
            data.UpdateDate = DateTime.Now;
            data.IsDelete = true;
            dbContext.Entry(data).State = EntityState.Modified;
        }
        public void InsertValue(Process_Software_Context dbContext)
        {
            this.UpdateDate = DateTime.Now;
            this.CreateDate = DateTime.Now;
            this.UpdateBy = GlobalVariable.GetUserID();
        }
        public void SaveLog(Process_Software_Context dbContext, int id)
        {
            //ถ้า id = 0 บันทึก Log ของ Manage
            if (id == 0)
            {
                // ดึงข้อมูล WorkLog ที่เกี่ยวข้องกับ Work ที่มี ID เท่ากับ ID ของ Work ที่รับมา
                List<WorkLog> workLogDBList = dbContext.WorkLog.Where(s => s.WorkID == this.ID).Include(s => s.ProviderLog).ToList();

                WorkLog workLog = new WorkLog();
                WorkLog rawLog = new WorkLog();
                try
                {
                    rawLog = dbContext.WorkLog.Where(b => b.WorkID == this.ID).ToList().Last();
                }
                catch
                {
                    rawLog = null;
                }
                if (rawLog == null)
                {
                    workLog.No = 1;
                }
                else
                {
                    workLog.No = rawLog.No + 1;
                }
                workLog.Insert(dbContext, this);
                workLog.ProviderLog = new List<ProviderLog>();

                // ตรวจสอบ Provider ที่เกี่ยวข้องกับ Work ถ้าเป็น null
                if (this.Provider != null)
                {
                    foreach (Provider i in this.Provider)
                    {
                        ProviderLog providerLog = new ProviderLog();
                        providerLog.UserID = i.UserID;
                        providerLog.Insert(dbContext, workLog);
                        workLog.ProviderLog.Add(providerLog);
                    }
                }
                // ตรวจสอบ Provider ที่เกี่ยวข้องกับ Work ถ้าไม่เป็น null
                else
                {
                    foreach (ProviderLog i in workLog.ProviderLog)
                    {
                        ProviderLog providerLog = new ProviderLog();
                        providerLog.UserID = i.UserID;
                        providerLog.Insert(dbContext, workLog);
                        workLog.ProviderLog.Add(providerLog);
                    }
                }
                // เพิ่ม WorkLog เข้าสู่ DbContext และบันทึกข้อมูล
                dbContext.WorkLog.Add(workLog);
                try
                {
                    dbContext.SaveChanges();
                }
                catch (DbUpdateConcurrencyException e)
                {
                    Console.WriteLine(e);
                }
            }
            //ถ้า id != 0 บันทึก Log ของ Edit
            else
            {
                // กรณี ID ไม่เป็น 0 (หมายถึงการแก้ไข Work ที่มี ID ให้กำหนด Log)
                List<WorkLog> workLogDBList = dbContext.WorkLog.Where(s => s.WorkID == this.ID).Include(s => s.ProviderLog).ToList();

                WorkLog workLog = new WorkLog();

                workLog.Insert(dbContext, this);
                workLog.No = workLogDBList.Last().No + 1;
                workLog.ProviderLog = new List<ProviderLog>();
                if (this.Provider != null)
                {
                    foreach (Provider i in this.Provider)
                    {
                        ProviderLog providerLog = new ProviderLog();
                        providerLog.UserID = i.UserID;
                        providerLog.IsDelete = i.IsDelete;
                        providerLog.Insert(dbContext, workLog);
                        workLog.ProviderLog.Add(providerLog);
                        dbContext.ProviderLog.Add(providerLog);
                    }
                }
                else
                {
                    foreach (ProviderLog i in workLog.ProviderLog)
                    {
                        ProviderLog providerLog = new ProviderLog();
                        providerLog.UserID = i.UserID;
                        providerLog.IsDelete = false;
                        providerLog.Insert(dbContext, workLog);
                        workLog.ProviderLog.Add(providerLog);
                        dbContext.ProviderLog.Add(providerLog);
                    }
                }
                dbContext.WorkLog.Add(workLog);

                try
                {
                    dbContext.SaveChanges();
                }
                catch (DbUpdateConcurrencyException e)
                {
                    Console.WriteLine(e);
                }
            }
        }
        public bool CompareProvider(Process_Software_Context dbContext, int id)
        {
            //ถ้าไม่มีการแก้ไข
            if (this.IsSelectAllItem == null && this.ProvidersID == null)
            {
                // ดึงข้อมูล Work ที่ต้องการแก้ไข
                Work work1 = dbContext.Work.Where(s => s.ID == this.ID).Include(w => w.Provider).FirstOrDefault();

                // ตรวจสอบว่าข้อมูลไม่เปลี่ยนแปลง
                if (work1 != null &&
                    work1.Project == this.Project &&
                    work1.Name == this.Name &&
                    work1.DueDate == this.DueDate &&
                    work1.StatusID == this.StatusID &&
                    work1.Remark == this.Remark &&
                    work1.Provider.Count == this.Provider.Count
                )
                {
                    // ตรวจสอบค่า IsDelete ของ Provider แต่ละรายการ
                    bool isCheckValue = false;
                    for (int i = 0; i < this.Provider.Count; i++)
                    {
                        if (work1.Provider.ToList()[i].IsDelete != this.Provider.ToList()[i].IsDelete)
                        {
                            isCheckValue = true;
                            break
                                ;
                        }
                    }
                    // ถ้าไม่มีการเปลี่ยนแปลงค่า IsDelete ของ Provider ใด ๆ ก็ redirect ไปที่ Index
                    if (!isCheckValue)
                    {
                        return false;
                    }
                }
            }
            dbContext.ChangeTracker.Clear();
            return true;
            //เคลียร์ Tracker หรือ ID ที่ถูก Track อยู่
            
        }
        public void ProcessProviders(Process_Software_Context dbContext)
        {
            bool isCheck = false;
            if (this.ProvidersID != null || this.IsSelectAllItem == true)
            {
                isCheck = true;
            }
            if (isCheck)
            {
                if (this.IsSelectAllItem == true)
                {
                    this.ProvidersID = "";
                    var dbProvider = dbContext.User.ToList();
                    foreach (User item in dbProvider)
                    {
                        if (item == dbProvider.Last())
                        {
                            this.ProvidersID += item.ID;
                        }
                        else
                        {
                            this.ProvidersID += item.ID + ",";
                        }
                    }
                }

                List<string> providerids = this.ProvidersID.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                foreach (Provider item in this.Provider)
                {
                    bool isCheackValue = false;
                    foreach (string i in providerids)
                    {
                        if (Int32.TryParse(i, out int id))
                        {
                            if (item.UserID == id)
                            {
                                isCheackValue = true;
                                if (item.IsDelete)
                                {
                                    dbContext.Entry(item).State = EntityState.Modified;
                                    item.UpdateDate = DateTime.Now;
                                    item.IsDelete = false;
                                    item.UpdateBy = this.UpdateBy;
                                    item.CreateBy = this.CreateBy;
                                }
                                providerids.Remove(i);
                                break;
                            }
                        }
                    }
                    if (isCheackValue == false)
                    {
                        dbContext.Entry(item).State = EntityState.Modified;
                        item.IsDelete = true;
                        item.UpdateDate = DateTime.Now;
                    }
                }
                if (providerids.Count > 0)
                {
                    foreach (string item in providerids)
                    {
                        if (Int32.TryParse(item, out int id))
                        {
                            Provider provider = new Provider();
                            provider.UserID = id;
                            provider.Insert(dbContext);
                            this.Provider.Add(provider);
                        }
                    }
                }
            }
        }

    }
}

