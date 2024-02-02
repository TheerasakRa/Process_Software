using Microsoft.AspNetCore.Mvc;
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
            return View();
        }

        // ฟังก์ชัน Login ทำหน้าที่ตรวจสอบข้อมูลล็อกอินและเปลี่ยนเส้นทางหน้าเว็บ
        [HttpPost]
        public IActionResult Login(User user)
        {
            try
            {
                // ทำการเข้ารหัสรหัสผ่านและตรวจสอบกับฐานข้อมูล
                var checkdb = db.User.Where(s => s.Email == user.Email).FirstOrDefault();

                if (checkdb != null && HashingHelpers.VerifyHashedPassword(checkdb.Password, user.Password))
                {
                    // ถ้าพบข้อมูลผู้ใช้ในฐานข้อมูล
                    if (checkdb != null)
                    {
                        // กำหนดค่าใน GlobalVariable สำหรับให้ระบบทราบว่ามีการล็อกอิน
                        GlobalVariable.SetUserEmail(checkdb.Email);
                        GlobalVariable.SetUserID(checkdb.ID);

                        // แสดงข้อความแจ้งเตือนและเปลี่ยนเส้นทางไปยังหน้า Work/Index
                        TempData["AlertMessage"] = "Login successful";
                        return RedirectToAction("Index", "Work");
                    }
                }
            }
            catch (Exception ex)
            {
                // กรณีเกิดข้อผิดพลาดในระหว่างการล็อกอิน
                ModelState.AddModelError("", "Error. Try again!");
                Console.WriteLine("Err: " + ex);
                return View();
            }

            // กรณีไม่พบข้อมูลผู้ใช้หรือรหัสผ่านไม่ถูกต้อง
            ModelState.AddModelError("", "Wrong Email or Password");
            return View();
        }

        // ฟังก์ชัน Register ทำหน้าที่แสดงหน้าลงทะเบียน
        public IActionResult Register(int? id)
        {
            return View();
        }

        // ฟังก์ชัน Register ทำหน้าที่บันทึกข้อมูลผู้ใช้ลงในฐานข้อมูล
        [HttpPost]
        public IActionResult Register(User user)
        {
            try
            {
                // ตรวจสอบข้อมูลที่กรอกในฟอร์มการลงทะเบียน
                if (user.Name == null)
                {
                    ViewBag.FailName = "Please enter your name.";
                    return View(user);
                }

                // ตรวจสอบว่าอีเมลนี้ถูกใช้งานแล้วหรือไม่
                if (db.User.Any(u => u.Email == user.Email))
                {
                    ViewBag.FailMes = "Email is already registered.";
                    return View("Register", user);
                }

                // บันทึกข้อมูลผู้ใช้ลงในฐานข้อมูล
                user.Insert(db);
                db.SaveChanges();

                // กำหนดค่าใน GlobalVariable สำหรับให้ระบบทราบว่ามีการล็อกอิน
                GlobalVariable.SetUserEmail(user.Email);
                GlobalVariable.SetUserID(user.ID);

                // แสดงข้อความแจ้งเตือนและเปลี่ยนเส้นทางไปยังหน้า Work/Index
                TempData["AlertMessage"] = "Successfully registered";
                return RedirectToAction("Index", "Work");
            }
            catch (Exception ex)
            {
                // กรณีเกิดข้อผิดพลาดในระหว่างการลงทะเบียน
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
                // ลบคุกกี้ที่บันทึกการล็อกอิน
                Response.Cookies.Delete("UserEmail");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Err: " + ex);
            }
            
            // ล้างข้อมูล GlobalVariable และเปลี่ยนเส้นทางไปยังหน้า Login
            GlobalVariable.ClearGlobalVariable();
            return RedirectToAction("Login");
        }

        // ฟังก์ชัน Profile ทำหน้าที่แสดงหน้าโปรไฟล์ของผู้ใช้
        public IActionResult Profile(int? id)
        {
            // ค้นหาข้อมูลผู้ใช้จาก ID ที่ระบุ
            var x = db.User.Find(id);
            
            // เรียกใช้ฟังก์ชัน ViewbagData เพื่อกำหนดค่าใน ViewBag
            ViewbagData();

            // แสดงหน้าโปรไฟล์
            return View(x);
        }

        // ฟังก์ชัน Profile ทำหน้าที่บันทึกข้อมูลโปรไฟล์ลงในฐานข้อมูล
        [HttpPost]
        public IActionResult Profile(User user)
        {
            // อัพเดทข้อมูลผู้ใช้ในฐานข้อมูล
            user.Update(db);
            db.SaveChanges();
            
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
