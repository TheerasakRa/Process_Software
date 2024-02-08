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
using Microsoft.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

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
        private void FuncChangeMode(string? changeMode)
        {
            if (changeMode == "Operator")
            {
                HttpContext.Session.SetString("Default", "Operator");
                HttpContext.Session.Remove("AssignBy");
                HttpContext.Session.Remove("Project");
                HttpContext.Session.Remove("Status");
            }
            else if (changeMode == "Controller")
            {
                HttpContext.Session.SetString("Default", "Controller");
                HttpContext.Session.Remove("FilterProvider");
                HttpContext.Session.Remove("Project");
                HttpContext.Session.Remove("Status");
            }
        }

        public IActionResult Index(string? AssignBy, string? FilltersProvidersID, bool FilltersIsSelectAllItem, string? Project, string? Status, bool? IsChangePage, string? ChangeMode)
        {
            if (string.IsNullOrEmpty(GlobalVariable.GetUserEmail()))
            {
                return RedirectToAction("Login", "Home");
            }
            SelectProviderFilter(FilltersProvidersID, FilltersIsSelectAllItem);
            InitDataStatic();
            List<Work> works = GetWork();
            works = Filters(works, null, AssignBy, FilltersProvidersID, FilltersIsSelectAllItem, Project, Status, IsChangePage);

            ViewbagData();
            FuncChangeMode(ChangeMode);
            // แสดงหน้า Index พร้อมรายการงาน
            return View(works);
        }

        //Manage - หน้าใช้สำหรับสร้างรายการงาน
        public IActionResult Manage(int id, string? AssignBy, string? FilltersProvidersID, bool FilltersIsSelectAllItem, string? Project, string? Status, bool IsChangePage, string? ChangeMode)
        {
            // ล้างตัวแปรสถิตและกำหนดค่าเริ่มต้นของ dropdown lists
            ClearStatic();
            InitDataStatic();
            SelectProviderFilter(FilltersProvidersID, FilltersIsSelectAllItem);
            // กำหนดค่า WorkID จากพารามิเตอร์
            _WorkID = id;

            // ดึงข้อมูลผู้ใช้จาก session
            User? user = GetSessionUser();
            // หากไม่มีผู้ใช้เข้าสู่ระบบ ให้ redirect ไปที่หน้า Login
            if (user == null) return RedirectToAction("Login", "Home");

            // ดึงรายการงานและกำหนด ViewBag data
            List<Work> workList = GetWork();
            workList = Filters(workList, null, AssignBy, FilltersProvidersID, FilltersIsSelectAllItem, Project, Status, IsChangePage);
            FuncChangeMode(ChangeMode);
            Work work = new Work() { IsDelete = false, CreateDate = DateTime.Now, CreateBy = user.ID };
            Work works = new Work();
            works.CreateDate = DateTime.Now;
            works.CreateBy = GlobalVariable.GetUserID();
            ViewBag.UserID = works.CreateBy;
            workList.Add(works);
            ViewbagData();
            return View(workList);
        }
        // Manage ฟังก์ชันสำหรับสร้างรายการงาน
        [HttpPost]
        public IActionResult Manage(Work work)
        {
            if (work.ID != 0)
            {
                ModelState.AddModelError("", "Add DueDate!!!");
                return RedirectToAction("Edit", "Work", new { id = work.ID });
            }
            int _WorkID = 0;
            work.Insert(db);
            work.SaveLog(db, _WorkID);
            //SendMail(work, _WorkID);
            db.SaveChanges();
            return RedirectToAction("Index", "Work", new
            {
                AssignBy = HttpContext.Session.GetString("AssignBy"),
                FilltersProvidersID = HttpContext.Session.GetString("FilltersProvidersID"),
                Project = HttpContext.Session.GetString("Project"),
                Status = HttpContext.Session.GetString("Status"),
                IsChangePage = "true"
            });
        }

        // Edit ฟังก์ชันสำหรับแก้ไขรายการงาน
        public IActionResult Edit(int Workid, string? AssignBy, string? FilltersProvidersID, bool FilltersIsSelectAllItem, string? Project, string? Status, bool IsChangePage, string? ChangeMode)
        {
            // เคลียร์ข้อมูลที่เก็บไว้ในตัวแปรแบบ static
            ClearStatic();
            // กำหนดค่าเริ่มต้นให้ตัวแปรแบบ static
            InitDataStatic();
            SelectProviderFilter(FilltersProvidersID, FilltersIsSelectAllItem);

            // ดึงข้อมูลงานทั้งหมด
            List<Work> allWorks = GetWork();

            // ดึงข้อมูลงานที่ต้องการแก้ไข
            Work workEditItem = allWorks.FirstOrDefault(s => s.ID == Workid);

            // ตรวจสอบว่ามี Provider หรือไม่
            if (workEditItem != null && workEditItem.Provider != null)
            {
                // วนลูปเพื่อตรวจสอบ Provider และทำการตั้งค่า Selected ให้กับ UserProviserDropdownList
                foreach (Provider provider in workEditItem.Provider.Where(s => s.IsDelete != true))
                {
                    foreach (SelectListItem user in _UserProviserDropdownList)
                    {
                        if (provider.UserID.ToString() == user.Value)
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
                ViewBag.FormattedDueDate = workEditItem.DueDate.Value.ToString("dd-MM-yyyy");
            }

            // ค้นหาข้อมูลงานตาม ID
            Work foundWork = db.Work.Find(Workid);

            // กำหนดค่า WorkIDs ให้กับ ViewBag
            ViewBag.WorkIDs = foundWork?.ID;

            // เรียกใช้ฟังก์ชัน ViewbagData เพื่อนำข้อมูลไปใช้ใน View
            ViewbagData();

            // กรองข้อมูลงาน
            allWorks = Filters(allWorks, Workid, AssignBy, FilltersProvidersID, FilltersIsSelectAllItem, Project, Status, IsChangePage);

            // ทำการเปลี่ยนโหมด (ถ้ามีการระบุ)
            FuncChangeMode(ChangeMode);

            // ส่งข้อมูลงานไปยัง View
            return View(allWorks);
        }

        // Edit หน้าใช้สำหรับแก้ไขรายการงาน
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Work work)
        {
            // กำหนดค่า WorkID มีค่า 1
            int _WorkID = 1;

            bool Check = work.CompareProvider(db, _WorkID);
            if (Check != false)
            {
                db.ChangeTracker.Clear();
                work.Update(db);
                work.SaveLog(db, _WorkID);
                SendMail(work, _WorkID);
            }
            db.SaveChanges();
            return RedirectToAction("Index", "Work", new
            {
                AssignBy = HttpContext.Session.GetString("AssignBy"),
                FilltersProvidersID = HttpContext.Session.GetString("FilltersProvidersID"),
                Project = HttpContext.Session.GetString("Project"),
                Status = HttpContext.Session.GetString("Status"),
                IsChangePage = "true"
            });
        }
        // History - จัดการเกี่ยวกับการแสดงของ History หรือ Log
        public IActionResult History(int Workid, string? AssignBy, string? FilltersProvidersID, bool FilltersIsSelectAllItem, string? Project, string? Status, bool IsChangePage, string? ChangeMode)
        {
            // เรียกใช้งาน ClearStatic และ InitDataStatic สำหรับเคลียร์ข้อมูลและกำหนดค่าเริ่มต้นที่ใช้ร่วมกัน
            ClearStatic();
            InitDataStatic();
            SelectProviderFilter(FilltersProvidersID, FilltersIsSelectAllItem);
            // เรียกใช้งาน ViewbagData สำหรับกำหนดค่าใน ViewBag
            ViewbagData();
            // ดึงข้อมูลงาน (Work) พร้อม WorkLog, ProviderLog, User และ Status ที่มี ID ตรงกับที่รับมา
            Work work = db.Work
                        .Include(s => s.WorkLog).ThenInclude(s => s.ProviderLog).ThenInclude(s => s.User)
                        .Include(s => s.WorkLog).ThenInclude(s => s.Status)
                        .FirstOrDefault(s => s.ID == Workid);

            // กำหนดค่าใน ViewBag.HistoryID
            ViewBag.HistoryID = Workid;
            if (Workid == null)
            {
                return NotFound();
            }
            if (work == null)
            {
                return NoContent();
            }
            // ดึงข้อมูล Work ทั้งหมด
            List<Work> workDBList = GetWork();

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

                List<WorkLog> workLogs = db.WorkLog.Where(s => s.WorkID == Workid).ToList();
                ViewBag.ID = Workid;

                // วนลูปเพื่อกำหนดค่า LogContent ใน WorkLog แต่ละรายการ
                for (int w = 0; w < workLogs.Count() - 1; w++)
                {
                    workLogs[w].LogContent = "";

                    if (workLogs[w] != workLogs.LastOrDefault())
                    {
                        User updateBy = db.User.Where(s => s.ID == workLogs[w + 1].UpdateBy).FirstOrDefault();
                        ViewBag.UpdateBy = workLogs[w].LogContent = updateBy?.Name;
                    }
                }
            }
            // กลับด้านลำดับของ WorkLog
            List<WorkLog> worklogs = work.WorkLog.ToList();
            worklogs.Reverse();
            work.WorkLog = worklogs;
            // อัพเดทข้อมูล Work ในรายการ Work ทั้งหมด
            workDBList[workDBList.FindIndex(s => s.ID == Workid)] = work;
            workDBList = Filters(workDBList, Workid, AssignBy, FilltersProvidersID, FilltersIsSelectAllItem, Project, Status, IsChangePage);
            FuncChangeMode(ChangeMode);
            // ส่งข้อมูล workDBList ไปยัง View
            return View(workDBList);
        }
        // SendMail - ส่งอีเมล
        private void SendMail(Work work, int id)
        {
            try
            {
                // สร้างอ็อบเจ็กต์ Mailrequest
                Mailrequest mailrequest = new Mailrequest();

                string EmailUser = GlobalVariable.GetUserEmail();

                List<string> providerEmails = GetProviderEmails(work);
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
            catch (Exception)
            {
                throw;
            }
        }

        private List<string> GetProviderEmails(Work work)
        {
            // ใช้ LINQ เพื่อดึงอีเมลของ User ที่เกี่ยวข้องกับงานที่กำหนด
            List<string> providerEmails = db.Provider
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
            foreach (Status i in db.Status.ToList())
            {
                _StatusDropdownList.Add(new SelectListItem() { Text = i.StatusName, Value = i.ID.ToString() });
            }
            // กำหนดค่าใน _UserProviserDropdownList จากข้อมูล User ในฐานข้อมูล
            foreach (User i in db.User.ToList())
            {
                _UserProviserDropdownList.Add(new SelectListItem() { Text = i.Name, Value = i.ID.ToString() });
            }
            List<Work> distinctProjects = db.Work.Where(w => w.Project != null).GroupBy(w => w.Project).Select(g => g.First()).ToList();

            foreach (Work i in distinctProjects)
            {
                if (i != null && i.Project != null)
                {
                    _WorkProjectDropdownList.Add(new SelectListItem() { Text = i.Project, Value = i.Project });
                }
            }

        }

        //ClearStatic เมื่อเรียกใช้จะทำการล้างข้อมูลที่มีอยู๋ของ static
        private void ClearStatic()
        {
            _WorkID = null;
            _StatusDropdownList.Clear();
            _UserProviserDropdownList.Clear();
        }

        private void SelectProviderFilter(string? FilltersProvidersID, bool FilltersIsSelectAllItem)
        {
            if (FilltersIsSelectAllItem)
            {
                List<SelectListItem> UserList = db.User
                            .Select(u => new SelectListItem
                            {
                                Value = u.ID.ToString(),
                                Text = u.Name,
                                Selected = true

                            }).ToList();
                _FilterProvider = UserList;
            }
            else
            {
                if (FilltersProvidersID != null)
                {
                    string[] ListStringsProvider = FilltersProvidersID.Split(',');
                    List<SelectListItem> UserList = db.User
                                        .Select(u => new SelectListItem
                                        {
                                            Value = u.ID.ToString(),
                                            Text = u.Name
                                        }).ToList();
                    //_FilterProvider = UserList;
                    foreach (string i in ListStringsProvider)
                    {
                        foreach (SelectListItem j in UserList)
                        {
                            if (i == j.Value)
                            {
                                j.Selected = true;
                                break;
                            }
                        }
                    }
                    _FilterProvider = UserList;
                }
                else
                {
                    var UserList = db.User.Select(u => new SelectListItem { Value = u.ID.ToString(), Text = u.Name }).ToList();
                    _FilterProvider = UserList;
                }

            }


        }

        [HttpPost]
        private bool WorkExists(int id)
        {
            return db.Work.Any(e => e.ID == id);
        }

        private List<Work> Filters(List<Work> works, int? id, string? AssignBy, string? FilltersProvidersID, bool FilltersIsSelectAllItem, string? Project, string? Status, bool? IsChangePage)
        {
            if (IsChangePage == null && AssignBy == null) { HttpContext.Session.Remove("AssignBy"); }
            if (IsChangePage == null && FilltersProvidersID == null) { HttpContext.Session.Remove("FilltersProvidersID"); }
            if (IsChangePage == null && FilltersIsSelectAllItem == false) { HttpContext.Session.Remove("FilltersIsSelectAllItem"); }
            if (IsChangePage == null && Project == null) { HttpContext.Session.Remove("Project"); }
            if (IsChangePage == null && Status == null) { HttpContext.Session.Remove("Status"); }

            // Filter All works
            if (AssignBy != null)
            {
                works = works
                    .Where(m => m.ID == id || m.CreateBy == int.Parse(AssignBy))
                    .ToList();

                HttpContext.Session.SetString("AssignBy", AssignBy);
            }

            if (Project != null)
            {
                works = works
                    .Where(m => m.ID == id || m.Project == Project)
                    .ToList();
                HttpContext.Session.SetString("Project", Project);
            }

            if (Status != null && int.TryParse(Status, out int statusId))
            {
                works = works
                    .Where(m => m.ID == id || m.StatusID == statusId)
                    .ToList();
                HttpContext.Session.SetString("Status", Status);
            }

            if (FilltersIsSelectAllItem)
            {
                foreach (Work work in works)               {
                    work.IsSelectAllItem = true;
                }
                works = works.Where(m => m.ID == id || m.IsFound == true).ToList();
                HttpContext.Session.SetString("FilltersIsSelectAllItem", true.ToString());
            }
            else if (!string.IsNullOrEmpty(FilltersProvidersID))
            {
                string[] strings = FilltersProvidersID.Split(',');
                foreach (Work work in works)
                {
                    if (work.Provider.Any(w => strings.Contains(w.UserID.ToString()) && !w.IsDelete))
                    {
                        work.IsFound = true;
                    }
                }
                works = works.Where(m => m.ID == id || m.IsFound == true).ToList();
                HttpContext.Session.SetString("FilltersProvidersID", FilltersProvidersID);
            }

            return works;
        }
    }
}