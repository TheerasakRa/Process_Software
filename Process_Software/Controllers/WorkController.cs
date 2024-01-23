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
using Process_Software.Models;

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
        // GET: Work
        public IActionResult Index(int? id)
        {
            var work = GetWork();

            return View(work);
        }
        public IActionResult Manage(int? id)
        {
            ClearStatic();

            _WorkID = id;

            var workList = GetWork();
            Work works = new Work()
            {
                CreateDate = DateTime.Now,
                UpdateDate = DateTime.Now,
                IsDelete = false,
                IsSelectAllItem = false,
                CreateBy = db.User.First().ID,
            };
            workList.Add(works);
            InitDataStatic();
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
                work.CreateBy = db.User.First().ID;
                work.UpdateBy = work.CreateBy;
                work.CreateDate = DateTime.Now;
                if (work.IsSelectAllItem == true)
                {
                    foreach (var item in db.User)
                    {
                        Provider provider = new Provider()
                        {
                            UpdateDate = DateTime.Now,
                            CreateDate = DateTime.Now,
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

            return RedirectToAction("Index");
        }

        public IActionResult History(int? id)
        {
            //var works = GetWork();
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
                return View(workDBList);
            }

            for (int i = 1; i < work.WorkLog.Count; i++)
            {
                WorkLog workLogNext = GetWorkLogNextByIndex(i, work);
                work.WorkLog.ToList()[i - 1].nextWorklog = workLogNext;
            }

            workDBList[workDBList.FindIndex(s => s.ID == id)] = work;
            return View(workDBList);
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
                ProviderLog = workForLog.WorkLog.ToList()[index].ProviderLog
            };
            return tempWorkLogNext;
        }
        private void InitDataStatic()
        {

            foreach (var i in db.Status)
            {
                SelectListItem selectListItem = new SelectListItem() { Text = i.StatusName, Value = i.ID.ToString() };
                _ProviderDropdownList.Add(selectListItem);
            }

            foreach (var i in db.User)
            {
                SelectListItem selectListItem = new SelectListItem() { Text = i.Name, Value = i.ID.ToString() };
                _UserProviserDropdownList.Add(selectListItem);
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
            ViewBag.UserValue = db.User.ToList();
            ViewBag.StatusDropdownList = _ProviderDropdownList;
            ViewBag.UserProviserDropdownList = _UserProviserDropdownList;

        }

        public List<Work> GetWork()
        {
            var work = db.Work.Include(m => m.Status).Include(s => s.Provider).Include(w => w.WorkLog).ToList();

            foreach (var item in work)
            {
                foreach (var item2 in item.Provider)
                {
                    item2.User = db.User.Find(item2.UserID);
                }
            }

            return work;
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

            work.CreateDate = DateTime.Now;
            work.CreateBy = db.User.First().ID;
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
            CultureInfo cultureTHInfo = new CultureInfo("th-TH");
            work.DueDate = Convert.ToDateTime(work.DueDate, cultureTHInfo);
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
                        CreateBy = i.CreateBy,
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
                        CreateBy = i.CreateBy,
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


        private bool WorkExists(int id)
        {
            return db.Work.Any(e => e.ID == id);
        }
    }
}
