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
    public class WorkController : Controller
    {
        private Process_Software_Context db = new Process_Software_Context();

        static int? _WorkID;
        static List<SelectListItem> _ProviderDropdownList = new List<SelectListItem>();
        static List<SelectListItem> _UserProviserDropdownList = new List<SelectListItem>();
        string DefaultStatus = "Waiting Plan";
        string WaitingStatus = "Waiting";
        private readonly IEmailService emailService;
        public WorkController(IEmailService emailService)
        {
            this.emailService = emailService;

        }
        // GET: Work
        public IActionResult Index(int? id)
        {
            ClearStatic();
            InitDataStatic();

            User? user = GetSessionUser();

            if (user == null) return RedirectToAction("Login", "Home");

            var work = GetWork();
            ViewbagData();

            return View(work);
        }

        private User? GetSessionUser()
        {
            if (HttpContext.Session.GetString(ProcessDB.SessionName.UserSession.ToString()) != null)
            {
                string? userSession = HttpContext.Session.GetString(ProcessDB.SessionName.UserSession.ToString());
                User? user = db.User.Where(s => s.Email == userSession).FirstOrDefault();
                return user;
            }
            else
            {
                //! Else return null
                return null;
            }
        }

        public IActionResult Manage(int? id)
        {
            ClearStatic();
            InitDataStatic();

            _WorkID = id;

            User? user = GetSessionUser();

            if (user == null) return RedirectToAction("Login", "Home");
            var workList = GetWork();
            Work work = new Work() { IsDelete = false, CreateDate = DateTime.Now, CreateBy = user.ID };
            Work works = new Work()
            {
                CreateDate = DateTime.Now,
                UpdateDate = DateTime.Now,
                IsDelete = false,
                IsSelectAllItem = false,
                CreateBy = int.Parse(HttpContext.Session.GetString(ProcessDB.SessionName.UserID.ToString())),
                UpdateBy = int.Parse(HttpContext.Session.GetString(ProcessDB.SessionName.UserID.ToString()))
            };
            workList.Add(works);
            ViewbagData();
            return View(workList);
        }

        [HttpPost]
        public IActionResult Manage(Work work)
        {
            var defaultStatus = db.Status.FirstOrDefault(s => s.StatusName == DefaultStatus);
            var waitingStatus = db.Status.FirstOrDefault(s => s.StatusName == WaitingStatus);

            if (work.DueDate == null && defaultStatus != null)
            {
                work.StatusID = defaultStatus.ID;
            }
            else if (work.DueDate != null && waitingStatus != null)
            {
                work.StatusID = waitingStatus.ID;
            }
            else
            {
                work.StatusID = null;
            }
            if (work.ID == 0)
            {

                work.Provider = new List<Provider>();
                work.CreateBy = int.Parse(HttpContext.Session.GetString(ProcessDB.SessionName.UserID.ToString()));
                work.UpdateBy = int.Parse(HttpContext.Session.GetString(ProcessDB.SessionName.UserID.ToString()));
                work.CreateDate = DateTime.Now;
                if (work.IsSelectAllItem == true)
                {
                    foreach (var item in db.User)
                    {
                        Provider provider = new Provider()
                        {
                            UpdateDate = DateTime.Now,
                            CreateDate = DateTime.Now,
                            CreateBy = work.CreateBy,
                            UpdateBy = work.UpdateBy,
                            IsDelete = false,
                            UserID = item.ID


                        };
                        db.Provider.Add(provider);
                        work.Provider.Add(provider);
                    }
                }
                else
                {
                    if (work.ProvidersID == null)
                    {
                        foreach (var item in work.Provider)
                        {
                            Provider provider = new Provider()
                            {
                                UpdateDate = null,
                                CreateDate = null,
                                UserID = null,
                                WorkID = null,
                                CreateBy = null,
                                UpdateBy = null,
                            };
                            db.Provider.Add(provider);
                            work.Provider.Add(provider);
                        }
                    }
                    else
                    {
                        var providerlist = work.ProvidersID.Split(',');

                        foreach (var item in providerlist)
                        {
                            Provider provider = new Provider()
                            {
                                UpdateDate = DateTime.Now,
                                CreateDate = DateTime.Now,
                                IsDelete = false,
                                CreateBy = work.CreateBy,
                                UpdateBy = work.UpdateBy,
                                UserID = Convert.ToInt32(item)
                            };
                            db.Provider.Add(provider);
                            work.Provider.Add(provider);
                        }
                    }

                }

            }

            db.Work.Add(work);

            db.SaveChanges();

            WorkLog workLog = new WorkLog()
            {
                UpdateDate = DateTime.Now,
                CreateDate = DateTime.Now,
                IsDelete = false,
                CreateBy = work.CreateBy,
                UpdateBy = work.UpdateBy,
                Remark = work.Remark,
                DueDate = work.DueDate,
                Name = work.Name,
                Project = work.Project,
                StatusID = work.StatusID,
                Status = work.Status,
                WorkID = work.ID,
                Work = work
            };
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

            db.WorkLog.Add(workLog);

            db.SaveChanges();
            foreach (var item in work.Provider)
            {
                ProviderLog providerLog = new ProviderLog()
                {
                    UpdateDate = workLog.CreateDate,
                    CreateDate = workLog.UpdateDate,
                    IsDelete = false,
                    CreateBy = workLog.CreateBy,
                    UpdateBy = workLog.UpdateBy,
                    UserID = item.UserID,
                    WorkLogID = workLog.ID,
                    WorkLog = workLog,

                };
                db.ProviderLog.Add(providerLog);
                db.SaveChanges();
            }
            SendMail(work);

            return RedirectToAction("Index");
        }
        public IActionResult History(int? id)
        {
            ClearStatic();
            InitDataStatic();
            ViewbagData();

            var work = db.Work
                        .Include(s => s.WorkLog).ThenInclude(s => s.ProviderLog).ThenInclude(s => s.User)
                        .Include(s => s.WorkLog).ThenInclude(s => s.Status)
                        .FirstOrDefault(s => s.ID == id);

            ViewBag.HistoryID = id;
            if (id == null)
            {
                return NotFound();
            }
            if (work == null)
            {
                return NoContent();
            }
            var workDBList = GetWork();
            if (work.WorkLog.Count < 2)
            {
                ViewBag.HistoryText = "Project " + work.Project + " has no changed";
            }

            for (int i = 1; i < work.WorkLog.Count; i++)
            {
                WorkLog workLogNext = GetWorkLogNextByIndex(i, work);
                work.WorkLog.ToList()[i - 1].nextWorklog = workLogNext;
                var a = db.WorkLog.Where(s => s.WorkID == id).ToList();
                ViewBag.ID = id;

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

            var worklogs = work.WorkLog.ToList();
            worklogs.Reverse();
            work.WorkLog = worklogs;
            workDBList[workDBList.FindIndex(s => s.ID == id)] = work;


            return View(workDBList);
        }
        private void SendMail(Work work)
        {
            try
            {
                Mailrequest mailrequest = new Mailrequest();
                var EmailUser = HttpContext.Session.GetString(ProcessDB.SessionName.UserSession.ToString());

                var providerEmails = GetProviderEmails();
                providerEmails.Add(EmailUser);

                if (providerEmails.Any())
                {
                    mailrequest.ToEmails = providerEmails;
                    mailrequest.Subject = "Operator";
                    mailrequest.Body = GetHtmlContent(work);
                    emailService.SendEmail(mailrequest);
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                throw;
            }
        }
        private List<string> GetProviderEmails()
        {
            var providerEmails = db.Provider
                .Where(provider => !provider.IsDelete)
                .Join(db.User,
                      provider => provider.UserID,
                      user => user.ID,
                      (provider, user) => user.Email)
                .ToList();

            return providerEmails;
        }
        private string GetHtmlContent(Work work)
        {
            var works = db.Work.Where(w => !w.IsDelete).ToList();
            User? user = GetSessionUser();

            ViewBag.UserName = user.Name;

            string response = "<div style=\"width:80%;margin:20px auto;background-color:#f0f8ff;border-radius:10px;padding:20px;text-align:center;\">";
            response += "<h1 style=\"color:#333;\">Project: " + work.Project + "</h1>";
            response += "<img src=\"https://img.freepik.com/free-photo/painting-mountain-lake-with-mountain-background_188544-9126.jpg?w=1060&t=st=1706598619~exp=1706599219~hmac=3e18aafdb40c14f2e2d4df6f02a3d8b9aff5fa26a6009054418bc40db3ccb011\" style=\"max-width:100%;border-radius:10px;margin:15px 0;\" />";

            if (work.DueDate != null)
            {
                response += "<h2 style=\"color:#333;\">Due Date: " + work.DueDate + "</h2>";
            }
            else
            {
                response += "<h2 style=\"color:#333;\">Due Date: <span style=\"color:red;\">ไม่มีกำหนดการ</span></h2>";
            }

            response += "<a href=\"https://www.facebook.com/profile.php?id=100014450434050\" style=\"display:block;color:#007bff;text-decoration:none;margin:10px 0;\">Please Add Friend by clicking the link</a>";
            response += "<div style=\"margin-top:20px;\"><h1 style=\"color:#333;\">Contact us:</h1></div>";
            response += "<p style=\"color:#555;margin:0;\">" + ViewBag.UserName + "</p>";
            response += "</div>";

            return response;
        }



        private WorkLog GetWorkLogNextByIndex(int index, Work workForLog)
        {
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
        private void InitDataStatic()
        {
            foreach (var i in db.Status.ToList())
            {
                _ProviderDropdownList.Add(new SelectListItem() { Text = i.StatusName, Value = i.ID.ToString() });
            }
            foreach (var i in db.User.ToList())
            {
                _UserProviserDropdownList.Add(new SelectListItem() { Text = i.Name, Value = i.ID.ToString() });
            }
        }
        private void ClearStatic()
        {
            _WorkID = null;
            _ProviderDropdownList.Clear();
            _UserProviserDropdownList.Clear();
        }
        private void ViewbagData()
        {
            User? user = GetSessionUser();

            ViewBag.UserName = user.Name;
            ViewBag.UserValue = db.User.ToList();
            ViewBag.StatusDropdownList = _ProviderDropdownList;
            ViewBag.UserProviserDropdownList = _UserProviserDropdownList;

        }

        public List<Work> GetWork()
        {
            return db.Work.Where(s => s.IsDelete == false)
               .Include(s => s.Provider).ThenInclude(inc => inc.User)
               .Include(s => s.Status)
               .ToList();
        }
        public IActionResult Edit(int? id)
        {
            ClearStatic();
            InitDataStatic();

            var work = GetWork();
            var workEditItem = work.Where(s => s.ID == id).First();
            if (workEditItem.Provider != null)
            {
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
            if (workEditItem != null && workEditItem.DueDate.HasValue)
            {
                workEditItem.DueDate = workEditItem.DueDate.Value.Date;
                var formattedDueDate = workEditItem.DueDate.Value.ToString("dd-MM-yyyy");
                ViewBag.FormattedDueDate = formattedDueDate;
            }
            ;

            var works = db.Work.Find(id);

            ViewBag.WorkIDs = works.ID;
            ViewbagData();
            return View(work);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Work work)
        {
            var work1 = db.Work.Where(s => s.ID == work.ID).Include(w => w.Provider).FirstOrDefault();

            if (work1 != null &&
                work1.Project == work.Project &&
                work1.Name == work.Name &&
                work1.DueDate == work.DueDate &&
                work1.StatusID == work.StatusID &&
                work1.Remark == work.Remark &&
                work1.Provider.Count == work.Provider.Count
            )
            {
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
                if (!isCheckValue)
                {
                    return RedirectToAction("Index");
                }
            }
            db.ChangeTracker.Clear();
            work.UpdateBy = int.Parse(HttpContext.Session.GetString(ProcessDB.SessionName.UserID.ToString()));
            work.CreateDate = DateTime.Now;
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

                List<string> providerids = work.ProvidersID.Split(',').ToList();
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
                            Provider provider = new Provider()
                            {
                                CreateBy = work.CreateBy,
                                UpdateBy = work.UpdateBy,
                                CreateDate = DateTime.Now,
                                UpdateDate = DateTime.Now,
                                IsDelete = false,
                                UserID = id
                            };
                            db.Provider.Add(provider);
                            work.Provider.Add(provider);
                        }
                    }
                }
            }

            db.Entry(work).State = EntityState.Modified;
            db.SaveChanges();
            work.UpdateDate = DateTime.Now;

            var workLogDBList = db.WorkLog.Where(s => s.WorkID == work.ID).Include(s => s.ProviderLog).ToList();

            WorkLog workLog = new WorkLog()
            {
                WorkID = work.ID,
                Project = work.Project,
                Name = work.Name,
                No = workLogDBList.Last().No + 1,
                DueDate = work.DueDate,
                StatusID = work.StatusID,
                Remark = work.Remark,
                CreateBy = work.CreateBy,
                UpdateBy = work.UpdateBy,
                CreateDate = DateTime.Now,
                UpdateDate = DateTime.Now,
                IsDelete = work.IsDelete
            };
            workLog.ProviderLog = new List<ProviderLog>();
            if (work.Provider != null)
            {
                foreach (var i in work.Provider)
                {
                    ProviderLog providerLog = new ProviderLog()
                    {
                        UserID = i.UserID,
                        CreateBy = workLog.CreateBy,
                        UpdateBy = workLog.UpdateBy,
                        CreateDate = DateTime.Now,
                        UpdateDate = DateTime.Now,
                        IsDelete = i.IsDelete
                    };
                    workLog.ProviderLog.Add(providerLog);
                    db.ProviderLog.Add(providerLog);
                }
            }
            else
            {
                foreach (var i in workLog.ProviderLog)
                {
                    ProviderLog providerLog = new ProviderLog()
                    {
                        UserID = i.UserID,
                        CreateBy = workLog.CreateBy,
                        UpdateBy = workLog.UpdateBy,
                        CreateDate = DateTime.Now,
                        UpdateDate = DateTime.Now,
                        IsDelete = false
                    };
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
            return RedirectToAction("Index");
        }
        [HttpPost]
        private bool WorkExists(int id)
        {
            return db.Work.Any(e => e.ID == id);
        }
    }
}
