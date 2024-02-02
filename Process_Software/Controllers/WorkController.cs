using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using Process_Software.Common;
using Process_Software.Models;
using System.Net;
using System.Net.Mail;
using Process_Software.Helpter;
using Process_Software.Service;

namespace Process_Software.Controllers
{
    public class WorkController : BaseController
    {
        // ประกาศตัวแปร ใช้ในการส่งอีเมล
        private IEmailService emailService;

        // กำหนด Dependency Injection สำหรับ IEmailService ในคอนสตรัคเตอร์
        public WorkController(IEmailService emailService)
        {
            this.emailService = emailService;
            //ClassA classA = new ClassA();
            //var a = classA.MyProperty;
        }
        // GET: Work - หน้าหลักที่แสดงรายการงาน
        public IActionResult Index(int? id)
        {
            // ล้างตัวแปรสถิตและกำหนดค่าเริ่มต้นของ dropdown lists
            ClearStatic();
            InitDataStatic();

            // ดึงข้อมูลผู้ใช้จาก session
            User? user = GetSessionUser();
            // หากไม่มีผู้ใช้เข้าสู่ระบบ ให้ redirect ไปที่หน้า Login
            if (user == null) return RedirectToAction("Login", "Home");

            // ดึงรายการงานและกำหนด ViewBag data
            var work = GetWork();
            ViewbagData();

            // แสดงหน้า Index พร้อมรายการงาน
            return View(work);
        }
        //Manage - หน้าใช้สำหรับสร้างรายการงาน
        public IActionResult Manage(int? id)
        {
            // ล้างตัวแปรสถิตและกำหนดค่าเริ่มต้นของ dropdown lists
            ClearStatic();
            InitDataStatic();

            // กำหนดค่า WorkID จากพารามิเตอร์
            _WorkID = id;

            // ดึงข้อมูลผู้ใช้จาก session
            User? user = GetSessionUser();
            // หากไม่มีผู้ใช้เข้าสู่ระบบ ให้ redirect ไปที่หน้า Login
            if (user == null) return RedirectToAction("Login", "Home");

            // ดึงรายการงานและกำหนด ViewBag data
            var workList = GetWork();
            Work work = new Work() { IsDelete = false, CreateDate = DateTime.Now, CreateBy = user.ID };
            Work works = new Work();
            works.CreateDate = DateTime.Now;
            works.CreateBy = GlobalVariable.GetUserID();
            workList.Add(works);
            ViewbagData();
            return View(workList);
        }
        // Manage ฟังก์ชันสำหรับสร้างรายการงาน
        [HttpPost]
        public IActionResult Manage(Work work)
        {
            // กำหนดค่าเริ่มต้นและค่าสถานะ "Waiting" จากฐานข้อมูล
            int _WorkID = 0;
            var defaultStatus = db.Status.FirstOrDefault(s => s.StatusName == work.DefaultStatus);
            var waitingStatus = db.Status.FirstOrDefault(s => s.StatusName == work.WaitingStatus);

            // กำหนดสถานะขึ้นอยู่กับ DueDate
            // ถ้าไม่เลือก ให้กำหนดค่าเป็น defaultStatus
            if (work.DueDate == null && defaultStatus != null)
            {
                work.StatusID = defaultStatus.ID;
            }
            // ถ้าเลือก ให้กำหนดค่าเป็น waitingStatus
            else if (work.DueDate != null && waitingStatus != null)
            {
                work.StatusID = waitingStatus.ID;
            }
            else
            {
                work.StatusID = null;
            }
            // ถ้า id เป็น 0 ให้เพิ่มข้อมูลลง database
            if (work.ID == 0)
            {

                work.Insert(db);

                work.Provider = new List<Provider>();
                //ถ้าเลือกทั้งหมด
                if (work.IsSelectAllItem == true)
                {
                    foreach (var item in db.User)
                    {
                        Provider provider = new Provider()
                        {
                            UserID = item.ID,
                            //WorkID = work.ID,
                        };
                        provider.Insert(db);
                        work.Provider.Add(provider);
                    }
                }
                else
                {
                    // ถ้าไม่ได้เลือกทุก Provider
                    if (work.ProvidersID == null)
                    {
                        foreach (var item in work.Provider)
                        {
                            Provider provider = new Provider();
                            provider.Insert(db = null);
                            work.Provider.Add(provider);
                        }
                    }
                    else
                    {
                        //ถ้าเลือกแค่บางตัว คั่นด้วย ,
                        var providerlist = work.ProvidersID.Split(',');

                        foreach (var item in providerlist)
                        {
                            Provider provider = new Provider()
                            {
                                UserID = Convert.ToInt32(item)
                            };
                            provider.Insert(db);
                            //db.Provider.Add(provider);
                            work.Provider.Add(provider);
                        }
                    }

                }

            }
            db.Work.Add(work);
            

            //เรียกใช้ function ProcessProviders เพื่อ กำหนดค่า Providers
            ProcessProviders(work);
            //เรียกใช้ function ProcessProviders เพื่อ บันทึก Log
            SaveLog(work, _WorkID);
            //เรียกใช้ function ProcessProviders เพื่อ ส่ง Mail
            SendMail(work, _WorkID);
            // บันทึกการเปลี่ยนแปลงลงในฐานข้อมูล
            db.SaveChanges();
            // Redirect ไปที่หน้า Index
            return RedirectToAction("Index");
        }
        // Edit ฟังก์ชันสำหรับแก้ไขรายการงาน
        public IActionResult Edit(int? id)
        {
            // เคลียร์ข้อมูลที่เก็บไว้ในตัวแปรแบบ static
            ClearStatic();
            // กำหนดค่าเริ่มต้นให้ตัวแปรแบบ static
            InitDataStatic();
            // ดึงข้อมูลงานทั้งหมด
            var work = GetWork();
            // ดึงข้อมูลงานที่ต้องการแก้ไข
            var workEditItem = work.Where(s => s.ID == id).First();
            // ตรวจสอบว่ามี Provider หรือไม่
            if (workEditItem.Provider != null)
            {
                // วนลูปเพื่อตรวจสอบ Provider และทำการตั้งค่า Selected ให้กับ UserProviserDropdownList
                foreach (var i in workEditItem.Provider.Where(s => s.IsDelete != true))
                {
                    foreach (var user in _UserProviserDropdownList)
                    {
                        if (i.UserID.ToString() == user.Value)
                        {
                            user.Selected = true;
                            continue;
                        }

                    }

                }
            }
            // ตรวจสอบว่างานที่ต้องการแก้ไขมีค่าหรือไม่ และมี DueDate ที่กำหนด
            if (workEditItem != null && workEditItem.DueDate.HasValue)
            {
                // กำหนดค่า DueDate เป็นวันที่เท่ากับเวลาเริ่มต้นของวัน
                workEditItem.DueDate = workEditItem.DueDate.Value.Date;
                // กำหนดค่า FormattedDueDate เพื่อนำไปแสดงผลใน View
                var formattedDueDate = workEditItem.DueDate.Value.ToString("dd-MM-yyyy");
                ViewBag.FormattedDueDate = formattedDueDate;
            }
            ;
            // ค้นหาข้อมูลงานตาม ID
            var works = db.Work.Find(id);
            // กำหนดค่า WorkIDs ให้กับ ViewBag
            ViewBag.WorkIDs = works.ID;
            // เรียกใช้ฟังก์ชัน ViewbagData เพื่อนำข้อมูลไปใช้ใน View
            ViewbagData();
            // ส่งข้อมูลงานไปยัง View
            return View(work);
        }
        // Edit หน้าใช้สำหรับแก้ไขรายการงาน
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Work work)
        {
            // กำหนดค่า WorkID มีค่า 1
            int _WorkID = 1;

            //ถ้าไม่มีการแก้ไข
            if (work.IsSelectAllItem == null && work.ProvidersID == null)
            {
                // ดึงข้อมูล Work ที่ต้องการแก้ไข
                var work1 = db.Work.Where(s => s.ID == work.ID).Include(w => w.Provider).FirstOrDefault();

                // ตรวจสอบว่าข้อมูลไม่เปลี่ยนแปลง
                if (work1 != null &&
                    work1.Project == work.Project &&
                    work1.Name == work.Name &&
                    work1.DueDate == work.DueDate &&
                    work1.StatusID == work.StatusID &&
                    work1.Remark == work.Remark &&
                    work1.Provider.Count == work.Provider.Count
                )
                {
                    // ตรวจสอบค่า IsDelete ของ Provider แต่ละรายการ
                    bool isCheckValue = false;
                    for (int i = 0; i < work.Provider.Count; i++)
                    {
                        if (work1.Provider.ToList()[i].IsDelete != work.Provider.ToList()[i].IsDelete)
                        {
                            isCheckValue = true;
                            break
                                ;
                        }
                    }
                    // ถ้าไม่มีการเปลี่ยนแปลงค่า IsDelete ของ Provider ใด ๆ ก็ redirect ไปที่ Index
                    if (!isCheckValue)
                    {
                        return RedirectToAction("Index");
                    }
                }
            }
            //เคลียร์ Tracker หรือ ID ที่ถูก Track อยู่
            db.ChangeTracker.Clear();
            // เรียกใช้ฟังก์ชัน InsertValue และ ProcessProviders
            work.InsertValue(db);
            ProcessProviders(work);
            // กำหนดสถานะของข้อมูลเป็น Modified และบันทึกข้อมูล
            db.Entry(work).State = EntityState.Modified;
            db.SaveChanges();
            // เรียกใช้ฟังก์ชัน SaveLog และ SendMail
            SaveLog(work, _WorkID);
            SendMail(work, _WorkID);
            return RedirectToAction("Index");
        }
        // History - จัดการเกี่ยวกับการแสดงของ History หรือ Log
        public IActionResult History(int? id)
        {
            // เรียกใช้งาน ClearStatic และ InitDataStatic สำหรับเคลียร์ข้อมูลและกำหนดค่าเริ่มต้นที่ใช้ร่วมกัน
            ClearStatic();
            InitDataStatic();
            // เรียกใช้งาน ViewbagData สำหรับกำหนดค่าใน ViewBag
            ViewbagData();
            // ดึงข้อมูลงาน (Work) พร้อม WorkLog, ProviderLog, User และ Status ที่มี ID ตรงกับที่รับมา
            var work = db.Work
                        .Include(s => s.WorkLog).ThenInclude(s => s.ProviderLog).ThenInclude(s => s.User)
                        .Include(s => s.WorkLog).ThenInclude(s => s.Status)
                        .FirstOrDefault(s => s.ID == id);
            // กำหนดค่าใน ViewBag.HistoryID
            ViewBag.HistoryID = id;
            if (id == null)
            {
                return NotFound();
            }
            if (work == null)
            {
                return NoContent();
            }
            // ดึงข้อมูล Work ทั้งหมด
            var workDBList = GetWork();
            // ตรวจสอบว่า WorkLog มีน้อยกว่า 2 รายการ ให้แสดง ชื่อProjectและhas no changed
            if (work.WorkLog.Count < 2)
            {
                ViewBag.HistoryText = "Project " + work.Project + " has no changed";
            }
            // วนลูปเพื่อกำหนดค่า nextWorklog ใน WorkLog แต่ละรายการ
            for (int i = 1; i < work.WorkLog.Count; i++)
            {
                WorkLog workLogNext = GetWorkLogNextByIndex(i, work);
                work.WorkLog.ToList()[i - 1].nextWorklog = workLogNext;
                var a = db.WorkLog.Where(s => s.WorkID == id).ToList();
                ViewBag.ID = id;
                // วนลูปเพื่อกำหนดค่า LogContent ใน WorkLog แต่ละรายการ
                for (int w = 0; w < a.Count() - 1; w++)
                {
                    a[w].LogContent = "";

                    if (a[w] != a.LastOrDefault())
                    {
                        var updateBy = db.User.Where(s => s.ID == a[w + 1].UpdateBy).FirstOrDefault();
                        ViewBag.UpdateBy = a[w].LogContent += "Update by: " + updateBy?.Name;
                    }
                }
            }
            // กลับด้านลำดับของ WorkLog
            var worklogs = work.WorkLog.ToList();
            worklogs.Reverse();
            work.WorkLog = worklogs;
            // อัพเดทข้อมูล Work ในรายการ Work ทั้งหมด
            workDBList[workDBList.FindIndex(s => s.ID == id)] = work;

            // ส่งข้อมูล workDBList ไปยัง View
            return View(workDBList);
        }
        // SaveLog - บันทึก Log
        private void SaveLog(Work work, int id)
        {
            //ถ้า id = 0 บันทึก Log ของ Manage
            if (id == 0)
            {
                // ดึงข้อมูล WorkLog ที่เกี่ยวข้องกับ Work ที่มี ID เท่ากับ ID ของ Work ที่รับมา
                var workLogDBList = db.WorkLog.Where(s => s.WorkID == work.ID).Include(s => s.ProviderLog).ToList();

                WorkLog workLog = new WorkLog();
                WorkLog rawLog = new WorkLog();
                try
                {
                    rawLog = db.WorkLog.Where(b => b.WorkID == work.ID).ToList().Last();
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
                workLog.Insert(db, work);
                workLog.ProviderLog = new List<ProviderLog>();

                // ตรวจสอบ Provider ที่เกี่ยวข้องกับ Work ถ้าเป็น null
                if (work.Provider != null)
                {
                    foreach (var i in work.Provider)
                    {
                        ProviderLog providerLog = new ProviderLog();
                        providerLog.UserID = i.UserID;
                        providerLog.Insert(db, workLog);
                        workLog.ProviderLog.Add(providerLog);
                    }
                }
                // ตรวจสอบ Provider ที่เกี่ยวข้องกับ Work ถ้าไม่เป็น null
                else
                {
                    foreach (var i in workLog.ProviderLog)
                    {
                        ProviderLog providerLog = new ProviderLog();
                        providerLog.UserID = i.UserID;
                        providerLog.Insert(db, workLog);
                        workLog.ProviderLog.Add(providerLog);
                    }
                }
                // เพิ่ม WorkLog เข้าสู่ DbContext และบันทึกข้อมูล
                db.WorkLog.Add(workLog);
                try
                {
                    db.SaveChanges();
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
                var workLogDBList = db.WorkLog.Where(s => s.WorkID == work.ID).Include(s => s.ProviderLog).ToList();

                WorkLog workLog = new WorkLog();

                workLog.Insert(db, work);
                workLog.No = workLogDBList.Last().No + 1;
                workLog.ProviderLog = new List<ProviderLog>();
                if (work.Provider != null)
                {
                    foreach (var i in work.Provider)
                    {
                        ProviderLog providerLog = new ProviderLog();
                        providerLog.UserID = i.UserID;
                        providerLog.IsDelete = i.IsDelete;
                        providerLog.Insert(db, workLog);
                        workLog.ProviderLog.Add(providerLog);
                        db.ProviderLog.Add(providerLog);
                    }
                }
                else
                {
                    foreach (var i in workLog.ProviderLog)
                    {
                        ProviderLog providerLog = new ProviderLog();
                        providerLog.UserID = i.UserID;
                        providerLog.IsDelete = false;
                        providerLog.Insert(db, workLog);
                        workLog.ProviderLog.Add(providerLog);
                        db.ProviderLog.Add(providerLog);
                    }
                }
                db.WorkLog.Add(workLog);

                try
                {
                    db.SaveChanges();
                }
                catch (DbUpdateConcurrencyException e)
                {
                    Console.WriteLine(e);
                }
            }

        }
        // SendMail - ส่งอีเมล
        private void SendMail(Work work, int id)
        {
            try
            {
                // สร้างอ็อบเจ็กต์ Mailrequest
                Mailrequest mailrequest = new Mailrequest();

                var EmailUser = GlobalVariable.GetUserEmail();

                var providerEmails = GetProviderEmails(work);
                providerEmails.Add(EmailUser);
                // ตรวจสอบว่ามีอย่างน้อยหนึ่งอีเมลในรายการ
                if (providerEmails.Any())
                {
                    // กำหนดค่าใน Mailrequest
                    mailrequest.ToEmails = providerEmails;
                    mailrequest.Subject = "Operator";
                    // ตรวจสอบ ID เพื่อกำหนดเนื้อหาของอีเมลจัดการ Manage หรือ Create
                    if (id == 0)
                    {
                        SetHtmlContent htmlContentProvider = new SetHtmlContent();
                        mailrequest.Body = htmlContentProvider.GetHtmlContent(work);
                    }
                    // ตรวจสอบ ID เพื่อกำหนดเนื้อหาของอีเมลจัดการ Edit
                    else
                    {
                        SetHtmlContent htmlContentProvider = new SetHtmlContent();
                        mailrequest.Body = htmlContentProvider.GetHtmlContentEdit(work);
                    }
                    // เรียกใช้งาน emailService เพื่อส่งอีเมล
                    emailService.SendEmail(mailrequest);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private List<string> GetProviderEmails(Work work)
        {
            // ใช้ LINQ เพื่อดึงอีเมลของUserที่เกี่ยวข้องกับงานที่กำหนด
            var providerEmails = db.Provider
                .Where(provider => provider.WorkID == work.ID && !provider.IsDelete)
                .Join(db.User,
                      provider => provider.UserID,
                      user => user.ID,
                      (provider, user) => user.Email)
                .ToList();

            return providerEmails;
        }
        
        //GetWorkLogNextByIndex ใช้สำหรับดึงข้อมูล WorkLog
        private WorkLog GetWorkLogNextByIndex(int index, Work workForLog)
        {
            // สร้าง WorkLog ใหม่โดยกำหนดค่าจาก WorkLog ที่ตำแหน่ง index
            WorkLog tempWorkLogNext = new WorkLog()
            {
                Project = workForLog.WorkLog.ToList()[index].Project,
                Name = workForLog.WorkLog.ToList()[index].Name,
                DueDate = workForLog.WorkLog.ToList()[index].DueDate,
                Status = workForLog.WorkLog.ToList()[index].Status,
                StatusID = workForLog.WorkLog.ToList()[index].StatusID,
                Remark = workForLog.WorkLog.ToList()[index].Remark,
                CreateBy = workForLog.WorkLog.ToList()[index].CreateBy,
                ProviderLog = workForLog.WorkLog.ToList()[index].ProviderLog,
                UpdateDate = workForLog.WorkLog.ToList()[index].UpdateDate
            };
            return tempWorkLogNext;
        }
        //กำหนดค่าให้กับ ตัวแปร static
        private void InitDataStatic()
        {
            // กำหนดค่าใน _ProviderDropdownList จากข้อมูล Status ในฐานข้อมูล
            foreach (var i in db.Status.ToList())
            {
                _ProviderDropdownList.Add(new SelectListItem() { Text = i.StatusName, Value = i.ID.ToString() });
            }
            // กำหนดค่าใน _UserProviserDropdownList จากข้อมูล User ในฐานข้อมูล
            foreach (var i in db.User.ToList())
            {
                _UserProviserDropdownList.Add(new SelectListItem() { Text = i.Name, Value = i.ID.ToString() });
            }
        }

        //ClearStatic เมื่อเรียกใช้จะทำการล้างข้อมูลที่มีอยู๋ของ static
        private void ClearStatic()
        {
            _WorkID = null;
            _ProviderDropdownList.Clear();
            _UserProviserDropdownList.Clear();
        }
        
        // ProcessProviders - ทำการจัดการการเลือกProvider
        private void ProcessProviders(Work work)
        {
            bool isCheck = false;
            if (work.ProvidersID != null || work.IsSelectAllItem == true)
            {
                isCheck = true;
            }
            if (isCheck)
            {
                if (work.IsSelectAllItem == true)
                {
                    work.ProvidersID = "";
                    var dbProvider = db.User.ToList();
                    foreach (var item in dbProvider)
                    {
                        if (item == dbProvider.Last())
                        {
                            work.ProvidersID += item.ID;
                        }
                        else
                        {
                            work.ProvidersID += item.ID + ",";
                        }
                    }
                }

                List<string> providerids = work.ProvidersID.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                foreach (var item in work.Provider)
                {
                    bool isCheackValue = false;
                    foreach (var i in providerids)
                    {
                        if (Int32.TryParse(i, out int id))
                        {
                            if (item.UserID == id)
                            {
                                isCheackValue = true;
                                if (item.IsDelete)
                                {
                                    db.Entry(item).State = EntityState.Modified;
                                    item.UpdateDate = DateTime.Now;
                                    item.IsDelete = false;
                                    item.UpdateBy = work.UpdateBy;
                                    item.CreateBy = work.CreateBy;
                                }
                                providerids.Remove(i);
                                break;
                            }
                        }
                    }
                    if (isCheackValue == false)
                    {
                        db.Entry(item).State = EntityState.Modified;
                        item.IsDelete = true;
                        item.UpdateDate = DateTime.Now;
                    }
                }
                if (providerids.Count > 0)
                {
                    foreach (var item in providerids)
                    {
                        if (Int32.TryParse(item, out int id))
                        {
                            Provider provider = new Provider();
                            provider.UserID = id;
                            provider.Insert(db);
                            work.Provider.Add(provider);
                        }
                    }
                }
            }
        }

        [HttpPost]
        private bool WorkExists(int id)
        {
            return db.Work.Any(e => e.ID == id);
        }
    }
}