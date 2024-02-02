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
            return View();
        }

        // �ѧ��ѹ Login ��˹�ҷ���Ǩ�ͺ��������͡�Թ�������¹��鹷ҧ˹�����
        [HttpPost]
        public IActionResult Login(User user)
        {
            try
            {
                // �ӡ������������ʼ�ҹ��е�Ǩ�ͺ�Ѻ�ҹ������
                var checkdb = db.User.Where(s => s.Email == user.Email).FirstOrDefault();

                if (checkdb != null && HashingHelpers.VerifyHashedPassword(checkdb.Password, user.Password))
                {
                    // ��Ҿ������ż����㹰ҹ������
                    if (checkdb != null)
                    {
                        // ��˹����� GlobalVariable ����Ѻ����к���Һ����ա����͡�Թ
                        GlobalVariable.SetUserEmail(checkdb.Email);
                        GlobalVariable.SetUserID(checkdb.ID);

                        // �ʴ���ͤ�������͹�������¹��鹷ҧ��ѧ˹�� Work/Index
                        TempData["AlertMessage"] = "Login successful";
                        return RedirectToAction("Index", "Work");
                    }
                }
            }
            catch (Exception ex)
            {
                // �ó��Դ��ͼԴ��Ҵ������ҧ�����͡�Թ
                ModelState.AddModelError("", "Error. Try again!");
                Console.WriteLine("Err: " + ex);
                return View();
            }

            // �ó���辺�����ż�����������ʼ�ҹ���١��ͧ
            ModelState.AddModelError("", "Wrong Email or Password");
            return View();
        }

        // �ѧ��ѹ Register ��˹�ҷ���ʴ�˹��ŧ����¹
        public IActionResult Register(int? id)
        {
            return View();
        }

        // �ѧ��ѹ Register ��˹�ҷ��ѹ�֡�����ż����ŧ㹰ҹ������
        [HttpPost]
        public IActionResult Register(User user)
        {
            try
            {
                // ��Ǩ�ͺ�����ŷ���͡㹿�������ŧ����¹
                if (user.Name == null)
                {
                    ViewBag.FailName = "Please enter your name.";
                    return View(user);
                }

                // ��Ǩ�ͺ�������Ź��١��ҹ�����������
                if (db.User.Any(u => u.Email == user.Email))
                {
                    ViewBag.FailMes = "Email is already registered.";
                    return View("Register", user);
                }

                // �ѹ�֡�����ż����ŧ㹰ҹ������
                user.Insert(db);
                db.SaveChanges();

                // ��˹����� GlobalVariable ����Ѻ����к���Һ����ա����͡�Թ
                GlobalVariable.SetUserEmail(user.Email);
                GlobalVariable.SetUserID(user.ID);

                // �ʴ���ͤ�������͹�������¹��鹷ҧ��ѧ˹�� Work/Index
                TempData["AlertMessage"] = "Successfully registered";
                return RedirectToAction("Index", "Work");
            }
            catch (Exception ex)
            {
                // �ó��Դ��ͼԴ��Ҵ������ҧ���ŧ����¹
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
                // ź�ء�����ѹ�֡�����͡�Թ
                Response.Cookies.Delete("UserEmail");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Err: " + ex);
            }
            
            // ��ҧ������ GlobalVariable �������¹��鹷ҧ��ѧ˹�� Login
            GlobalVariable.ClearGlobalVariable();
            return RedirectToAction("Login");
        }

        // �ѧ��ѹ Profile ��˹�ҷ���ʴ�˹�������ͧ�����
        public IActionResult Profile(int? id)
        {
            // ���Ң����ż����ҡ ID ����к�
            var x = db.User.Find(id);
            
            // ���¡��ѧ��ѹ ViewbagData ���͡�˹����� ViewBag
            ViewbagData();

            // �ʴ�˹�������
            return View(x);
        }

        // �ѧ��ѹ Profile ��˹�ҷ��ѹ�֡�����������ŧ㹰ҹ������
        [HttpPost]
        public IActionResult Profile(User user)
        {
            // �Ѿഷ�����ż����㹰ҹ������
            user.Update(db);
            db.SaveChanges();
            
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
