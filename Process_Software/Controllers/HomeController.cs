using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Tls;
using Process_Software.Common;
using Process_Software.Models;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Process_Software.Controllers
{
    public class HomeController : BaseController
    {
        // ประกาศตัวแปรสำหรับเชื่อมต่อกับฐานข้อมูล
        //private Process_Software_Context db = new Process_Software_Context();

        // ฟังก์ชัน Index ทำหน้าที่ตรวจสอบว่ามีการล็อกอินเข้าระบบหรือไม่
        public IActionResult Index()
        {
            // ถ้าไม่มีการล็อกอินให้ redirect ไปยังหน้า Login
            if (GlobalVariable.GetUserEmail == null)
                return RedirectToAction("Login", "Home");

            // ถ้ามีการล็อกอินให้แสดงหน้า Index
            return View();
        }

        // ฟังก์ชัน Login ทำหน้าที่ตรวจสอบการล็อกอินของผู้ใช้
        public IActionResult Login()
        {
            if(GlobalVariable.GetUserEmail() != null)
            {
                return RedirectToAction("Index", "Work", new { FilltersProvidersID = HttpContext.Session.GetInt32("UserID") });
            }
            return View();
        }

        // ฟังก์ชัน Login ทำหน้าที่ตรวจสอบข้อมูลล็อกอินและเปลี่ยนเส้นทางหน้าเว็บ
        [HttpPost]
        public async Task<IActionResult> Login(User user)
        {
            try
            {
                var checkdb = await db.User.Where(s => s.Email == user.Email).FirstOrDefaultAsync();

                if (checkdb != null && await HashingHelpers.VerifyHashedPasswordAsync(checkdb.Password, user.Password))
                {
                    HttpContext.Session.SetString("UserEmail", checkdb.Email);
                    HttpContext.Session.SetInt32("UserID", checkdb.ID);
                    HttpContext.Session.SetString("Default", "Operator");
                    GlobalVariable.SetUserEmail(checkdb.Email);
                    GlobalVariable.SetUserID(checkdb.ID);
                    TempData["AlertMessage"] = "Login successful";
                    return RedirectToAction("Index", "Work", new { FilltersProvidersID = HttpContext.Session.GetInt32("UserID")});
                }
                else
                {
                    ViewBag.FailEmailPass = "Wrong Email or Password";
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error. Try again!");
                Console.WriteLine("Err: " + ex);
                return View();
            }
            return View();
        }

        // ฟังก์ชัน Register ทำหน้าที่แสดงหน้าลงทะเบียน
        public IActionResult Register(int? id)
        {
            if(HttpContext.Session.GetString("UserEmail") != null)
            {
                return RedirectToAction("Index", "Work", new { FilltersProvidersID = HttpContext.Session.GetInt32("UserID") });
            }
            return View();
        }

        // ฟังก์ชัน Register ทำหน้าที่บันทึกข้อมูลผู้ใช้ลงในฐานข้อมูล
        [HttpPost]
        public async Task<IActionResult> Register(User user)
        {
            try
            {
                if (user.Name == null)
                {
                    ViewBag.FailName = "Please enter your name.";
                    return View(user);
                }

                if (await db.User.AnyAsync(u => u.Email == user.Email))
                {
                    ViewBag.FailMes = "Email is already registered.";
                    return View("Register", user);
                }

                await user.InsertAsync(db);
                await db.SaveChangesAsync();

                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetInt32("UserID", user.ID);
                HttpContext.Session.SetString("Default", "Operator");
                GlobalVariable.SetUserEmail(user.Email);
                GlobalVariable.SetUserID(user.ID);
                TempData["AlertMessage"] = "Successfully registered";
                return RedirectToAction("Index", "Work");
            }
            catch (Exception ex)
            {
                ViewBag.FailMes = "Error during registration. Please try again.";
                Console.WriteLine("Err: " + ex);
                return View("Register", user);
            }
        }

        // ฟังก์ชัน Logout ทำหน้าที่ลบข้อมูลเซสชันและเปลี่ยนเส้นทางไปยังหน้า Login
        public IActionResult Logout()
        {
            try
            {
                GlobalVariable.ClearGlobalVariable();
                HttpContext.Session.Clear();
                // ลบคุกกี้ที่บันทึกการล็อกอิน
                Response.Cookies.Delete("UserEmail");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Err: " + ex);
            }

            // ล้างข้อมูล GlobalVariable และเปลี่ยนเส้นทางไปยังหน้า Login
            HttpContext.Session.Clear();
            GlobalVariable.ClearGlobalVariable();
            return RedirectToAction("Login");
        }

        // ฟังก์ชัน Profile ทำหน้าที่แสดงหน้าโปรไฟล์ของผู้ใช้
        public async Task<IActionResult> Profile(int? id)
        {
            // ค้นหาข้อมูลผู้ใช้จาก ID ที่ระบุ
            var x = await db.User.FindAsync(id);

            // เรียกใช้ฟังก์ชัน ViewbagData เพื่อกำหนดค่าใน ViewBag
            ViewbagData();

            // แสดงหน้าโปรไฟล์
            return View(x);
        }

        // ฟังก์ชัน Profile ทำหน้าที่บันทึกข้อมูลโปรไฟล์ลงในฐานข้อมูล
        [HttpPost]
        public async Task<IActionResult> Profile(User user)
        {
            // อัพเดทข้อมูลผู้ใช้ในฐานข้อมูล
            user.Update(db);
            await db.SaveChangesAsync();

            // เปลี่ยนเส้นทางไปยังหน้า Work/Index
            return RedirectToAction("Index", "Work");
        }

        // ฟังก์ชัน Privacy ทำหน้าที่แสดงหน้า Privacy Policy
        public IActionResult Privacy()
        {
            return View();
        }

        // ฟังก์ชัน GetSessionUser ทำหน้าที่ดึงข้อมูลผู้ใช้จากระบบเซสชัน

        // ฟังก์ชัน Error ทำหน้าที่แสดงหน้า Error
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
