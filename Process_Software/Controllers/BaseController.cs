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

        public List<SelectListItem> _ProviderDropdownList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> _UserProviserDropdownList { get; set; } = new List<SelectListItem>();
        public int? _WorkID;
        protected User? GetSessionUser()
        {
            // ตรวจสอบว่ามีการล็อกอินหรือไม่
            if (GlobalVariable.GetUserEmail != null)
            {
                // ดึงอีเมลผู้ใช้จากระบบเซสชัน
                string? userSession = GlobalVariable.GetUserEmail().ToString();

                // ค้นหาข้อมูลผู้ใช้จากอีเมลในฐานข้อมูล
                User? user = db.User.Where(s => s.Email == userSession).FirstOrDefault();
                return user;
            }
            else
            {
                return null;
            }
        }
        protected void ViewbagData()
        {
            User? user = GetSessionUser();
            ViewBag.UserName = user.Name;
            ViewBag.UserValue = db.User.ToList();
            ViewBag.StatusDropdownList = _ProviderDropdownList;
            ViewBag.UserProviserDropdownList = _UserProviserDropdownList;
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
