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
        // ��С�ȵ��������Ѻ�������͡Ѻ�ҹ������
        //private Process_Software_Context db = new Process_Software_Context();

        // �ѧ��ѹ Index ��˹�ҷ���Ǩ�ͺ����ա����͡�Թ����к��������
        public IActionResult Index()
        {
            // �������ա����͡�Թ��� redirect ��ѧ˹�� Login
            if (GlobalVariable.GetUserEmail == null)
                return RedirectToAction("Login", "Home");

            // ����ա����͡�Թ����ʴ�˹�� Index
            return View();
        }

        // �ѧ��ѹ Login ��˹�ҷ���Ǩ�ͺ�����͡�Թ�ͧ�����
        public IActionResult Login()
        {
            if(GlobalVariable.GetUserEmail() != null)
            {
                return RedirectToAction("Index", "Work", new { FilltersProvidersID = HttpContext.Session.GetInt32("UserID") });
            }
            return View();
        }

        // �ѧ��ѹ Login ��˹�ҷ���Ǩ�ͺ��������͡�Թ�������¹��鹷ҧ˹�����
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

        // �ѧ��ѹ Register ��˹�ҷ���ʴ�˹��ŧ����¹
        public IActionResult Register(int? id)
        {
            if(HttpContext.Session.GetString("UserEmail") != null)
            {
                return RedirectToAction("Index", "Work", new { FilltersProvidersID = HttpContext.Session.GetInt32("UserID") });
            }
            return View();
        }

        // �ѧ��ѹ Register ��˹�ҷ��ѹ�֡�����ż����ŧ㹰ҹ������
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

        // �ѧ��ѹ Logout ��˹�ҷ��ź�������ʪѹ�������¹��鹷ҧ��ѧ˹�� Login
        public IActionResult Logout()
        {
            try
            {
                GlobalVariable.ClearGlobalVariable();
                HttpContext.Session.Clear();
                // ź�ء�����ѹ�֡�����͡�Թ
                Response.Cookies.Delete("UserEmail");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Err: " + ex);
            }

            // ��ҧ������ GlobalVariable �������¹��鹷ҧ��ѧ˹�� Login
            HttpContext.Session.Clear();
            GlobalVariable.ClearGlobalVariable();
            return RedirectToAction("Login");
        }

        // �ѧ��ѹ Profile ��˹�ҷ���ʴ�˹�������ͧ�����
        public async Task<IActionResult> Profile(int? id)
        {
            // ���Ң����ż����ҡ ID ����к�
            var x = await db.User.FindAsync(id);

            // ���¡��ѧ��ѹ ViewbagData ���͡�˹����� ViewBag
            ViewbagData();

            // �ʴ�˹�������
            return View(x);
        }

        // �ѧ��ѹ Profile ��˹�ҷ��ѹ�֡�����������ŧ㹰ҹ������
        [HttpPost]
        public async Task<IActionResult> Profile(User user)
        {
            // �Ѿഷ�����ż����㹰ҹ������
            user.Update(db);
            await db.SaveChangesAsync();

            // ����¹��鹷ҧ��ѧ˹�� Work/Index
            return RedirectToAction("Index", "Work");
        }

        // �ѧ��ѹ Privacy ��˹�ҷ���ʴ�˹�� Privacy Policy
        public IActionResult Privacy()
        {
            return View();
        }

        // �ѧ��ѹ GetSessionUser ��˹�ҷ��֧�����ż����ҡ�к��ʪѹ

        // �ѧ��ѹ Error ��˹�ҷ���ʴ�˹�� Error
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
