using Microsoft.AspNetCore.Mvc;
using Process_Software.Common;
using Process_Software.Models;
using System.Diagnostics;

namespace Process_Software.Controllers
{
    public class HomeController : Controller
    {
        private Process_Software_Context db = new Process_Software_Context();

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString(ProcessDB.SessionName.UserSession.ToString()) == null) return RedirectToAction("Login", "Home");
            return View();
        }
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Login(User user)
        {
            try
            {
                var myUser = db.User.Where(s => s.Email == user.Email && s.Password == user.Password).FirstOrDefault();
                if (myUser != null)
                {
                    HttpContext.Session.SetString(ProcessDB.SessionName.UserSession.ToString(), myUser.Email);
                    HttpContext.Session.SetString(ProcessDB.SessionName.UserID.ToString(), myUser.ID.ToString()); 
                    if (user.RememberMe)
                    {
                        Response.Cookies.Append("UserEmail", myUser.Email, new CookieOptions
                        {
                            Expires = DateTime.Now.AddDays(30)
                        });
                    }
                    TempData["AlertMassage"] = "Login successful";

                    return RedirectToAction("Index", "Work");
                }
            }

            catch (Exception ex)
            {
                ViewBag.FailMes = "Error Try Again!";
                Console.WriteLine("Err: " + ex);
                return View();
            }

            ViewBag.FailMes = "Wrong Email or Password";
            return View();
        }
        public IActionResult Register(int? id)
        {
            return View();
        }
        [HttpPost]
        public IActionResult Register(User user)
        {
            try
            {
                if(user.Name == null)
                {
                    ViewBag.FailName = "Please enter your name.";
                    return View(user);
                }
                if (db.User.Any(u => u.Email == user.Email))
                {
                    ViewBag.FailMes = "Email is already registered.";
                    return View("Register", user);
                }
                user.CreateDate = DateTime.Now;
                user.UpdateDate = DateTime.Now;
                user.IsDelete = false;
                db.User.Add(user);
                db.SaveChanges();

                HttpContext.Session.SetString(ProcessDB.SessionName.UserSession.ToString(), user.Email);
                HttpContext.Session.SetString(ProcessDB.SessionName.UserID.ToString(), user.ID.ToString());

                TempData["AlertMassage"] = "Successfully registered";

                return RedirectToAction("Index", "Work");

            }
            catch (Exception ex)
            {
                ViewBag.FailMes = "Error during registration. Please try again.";
                Console.WriteLine("Err: " + ex);
                return View("Register", user);
            }

            return View("Register", user);
        }

        public IActionResult Logout()
        {
            try
            {
                HttpContext.Session.LoadAsync();
                HttpContext.Session.Remove(ProcessDB.SessionName.UserSession.ToString());

                // Remove the persistent cookie
                Response.Cookies.Delete("UserEmail");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Err: " + ex);
            }
            return RedirectToAction("Login");
        }
        public IActionResult Profile(int? id)
        {
            ViewbagData();
            
            var x = db.User.Find(id);
            return View(x);
        }
        [HttpPost]
        public IActionResult Profile(User user)
        {
            if(user.CreateDate == null)
            {
                user.CreateDate = DateTime.Now;
            }
            user.UpdateDate = DateTime.Now;
            db.User.Update(user);
            db.SaveChanges();
            return RedirectToAction("Index","Work");
        }
        private void ViewbagData()
        {
            User? user = GetSessionUser();
            ViewBag.UserName = user.Name;
        }
        public IActionResult Privacy()
        {
            return View();
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
                return null;
            }
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
