using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Process_Software.Models;
using System.Collections.Generic;  // Import this namespace for List<T>

namespace Process_Software.Controllers
{
    public class BaseController : Controller
    {
        public Process_Software_Context db = new Process_Software_Context();

        public List<SelectListItem> _StatusDropdownList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> _UserProviserDropdownList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> _WorkProjectDropdownList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> _FilterProvider { get; set; } = new List<SelectListItem>();
        public int? _WorkID;
        public User? GetSessionUser()
        {
            string getemail = HttpContext.Session.GetString("UserEmail");
            // ตรวจสอบว่ามีการล็อกอินหรือไม่
            if (getemail != null)
            {
                // ดึงอีเมลผู้ใช้จากระบบเซสชัน
                string? userSession = getemail.ToString();
                // ค้นหาข้อมูลผู้ใช้จากอีเมลในฐานข้อมูล
                User? user = db.User.Where(s => s.Email == userSession).FirstOrDefault();
                return user;
            }
            else
            {
                return null;
            }
        }
        public void ViewbagData()
        {
            User? user = GetSessionUser();
            ViewBag.UserName = user.Name;
            ViewBag.UserValue = db.User.ToList();
            ViewBag.StatusDropdownList = _StatusDropdownList;
            ViewBag.UserProviserDropdownList = _UserProviserDropdownList;
            ViewBag.WorkProjectDropdownList = _WorkProjectDropdownList;
            ViewBag.FilterProvider = _FilterProvider;
        }
        public void ViewbagDataIndex()
        {
            ViewBag.UserValue = db.User.ToList();
            ViewBag.StatusDropdownList = _StatusDropdownList;
            ViewBag.UserProviserDropdownList = _UserProviserDropdownList;
            ViewBag.WorkProjectDropdownList = _WorkProjectDropdownList;
            ViewBag.FilterProvider = _FilterProvider;
        }
        // GetWork ดึงข้อมูลงานทั้งหมดที่ไม่ถูกลบทิ้งพร้อมข้อมูลที่เกี่ยวข้อง
        public List<Work> GetWork()
        {
            return db.Work.Where(s => s.IsDelete == false)
               .Include(s => s.Provider).ThenInclude(inc => inc.User)
               .Include(s => s.Status)
               .ToList();
        }
        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}
