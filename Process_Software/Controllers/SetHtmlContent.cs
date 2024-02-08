using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Process_Software.Models;
using System.Collections.Generic;

namespace Process_Software.Controllers
{
    public class SetHtmlContent : BaseController
    {
        //GetHtmlContent ใช้ในการส่ง Email เมื่อสร้างเสร็จ
        public string GetHtmlContent(Work work)
        {
            var works = db.Work.Where(w => !w.IsDelete).ToList();
            User? user = GetSessionUser();
            ViewBag.UserName = user.Name;
            var UserName = ViewBag.UserName;

            string response = "<div style=\"width:80%;margin:20px auto;background-color:#f0f8ff;border-radius:10px;padding:20px;text-align:center;\">";
            response += "<h1 style=\"color:#333;\">Project: " + work.Project + "</h1>";
            response += "<img src=\"https://scontent.fbkk12-1.fna.fbcdn.net/v/t39.30808-6/306047967_763344048339876_5159997812934413183_n.png?_nc_cat=101&ccb=1-7&_nc_sid=efb6e6&_nc_eui2=AeGze_LgjDeJYaOlFhoaAVGUe-ezJ_DlQJF757Mn8OVAkRCkvo-sG8rOr_xILQqRkNaUMLiI9DSVYVwjShmrmfjN&_nc_ohc=hyGhWX7ZO4sAX9cdV5G&_nc_ht=scontent.fbkk12-1.fna&oh=00_AfCzdbMdnhZcG4vgPpPyKiyAonmrpG0B9LSOul5CYBlNRw&oe=65BE5CEF\" style=\"max-width:100%;border-radius:10px;margin:15px 0;\" />";

            if (work.DueDate != null)
            {
                string formattedDueDate = ((DateTime)work.DueDate).ToString("dd/MM/yyyy");
                response += "<h2 style=\"color:#333;\">Due Date: " + formattedDueDate + "</h2>";
            }
            else
            {
                response += "<h2 style=\"color:#333;\">Due Date: <span style=\"color:red;\">ไม่มีกำหนดการ</span></h2>";
            }
            response += "<h3 style=\"color:#333;\">Remark: " + work.Remark + "</h3>";
            response += "<a href=\"https://www.facebook.com/profile.php?id=100014450434050\" style=\"display:block;color:#007bff;text-decoration:none;margin:10px 0;\">Please Add Friend by clicking the link</a>";
            response += "<div style=\"margin-top:20px;\"><h1 style=\"color:#333;\">Create by:</h1></div>";
            response += "<h2 style=\"color:#555;margin:0;\">" + UserName + "</h2>";
            response += "</div>";

            return response;
        }
        //GetHtmlContent ใช้ในการส่ง Email เมื่อมีการแก้ไข
        public string GetHtmlContentEdit(Work work)
        {
            var worklogdb = db.WorkLog.Where(s => s.WorkID == work.ID).Include(s => s.Status).Include(prolog => prolog.ProviderLog).ThenInclude(u => u.User).ToList();
            ICollection<WorkLog> items = worklogdb;
            IEnumerable<WorkLog> lasttwolog = items.TakeLast(2);

            var lastworklog = lasttwolog.ToList();
            int i = 0;

            User? user = GetSessionUser();
            ViewBag.UserName = user.Name;
            var UserName = ViewBag.UserName;

            string response = "<div style=\"width:80%;margin:20px auto;background-color:#f0f8ff;border-radius:10px;padding:20px;text-align:center;\">";

            if (lastworklog != null && lastworklog.Count == 2)
            {
                response += "<h1 style=\"color:#333;\">There is a correction.</h1>";

                if (lastworklog[i].WorkID != lastworklog[i + 1].WorkID)
                {
                    response += "<h2 style=\"color:#333;\">Update by: " + lastworklog[i + 1].LogContent + "</h2>";
                }

                if (lastworklog[i].Project != lastworklog[i + 1].Project)
                {
                    response += "<h1 style=\"color:#333;\">Project: " + lastworklog[i].Project + " => " + lastworklog[i + 1].Project + "</h1>";
                }
                response += "<img src=\"https://scontent.fbkk12-1.fna.fbcdn.net/v/t39.30808-6/306047967_763344048339876_5159997812934413183_n.png?_nc_cat=101&ccb=1-7&_nc_sid=efb6e6&_nc_eui2=AeGze_LgjDeJYaOlFhoaAVGUe-ezJ_DlQJF757Mn8OVAkRCkvo-sG8rOr_xILQqRkNaUMLiI9DSVYVwjShmrmfjN&_nc_ohc=hyGhWX7ZO4sAX9cdV5G&_nc_ht=scontent.fbkk12-1.fna&oh=00_AfCzdbMdnhZcG4vgPpPyKiyAonmrpG0B9LSOul5CYBlNRw&oe=65BE5CEF\" style=\"max-width:100%;border-radius:10px;margin:15px 0;\" />";
                if (lastworklog[i].Name != lastworklog[i + 1].Name)
                {
                    response += "<p><strong>Name:</strong> " + lastworklog[i].Name + " => " + lastworklog[i + 1].Name + "</p>";
                }
                if (lastworklog[i].DueDate != lastworklog[i + 1].DueDate)
                {
                    string formattedDueDate1 = ((DateTime)lastworklog[i].DueDate).ToString("dd/MM/yyyy");
                    string formattedDueDate2 = ((DateTime)lastworklog[i + 1].DueDate).ToString("dd/MM/yyyy");

                    response += "<p><strong>Due Date:</strong> " + formattedDueDate1 + " => " + formattedDueDate2 + "</p>";
                }

                if (lastworklog[i].StatusID != lastworklog[i + 1].StatusID)
                {
                    response += "<p><strong>Status:</strong> " + lastworklog[i].Status.StatusName + " => " + lastworklog[i + 1].Status.StatusName + "</p>";
                }
                if (lastworklog[i].ProviderLog != null && lastworklog[i + 1].ProviderLog != null)
                {
                    var userIDsToFind = lastworklog[i].ProviderLog
                        .Where(s => s.IsDelete == false)
                        .Select(item => item.UserID)
                        .ToList();

                    var oldUsers = db.User
                        .Where(user => userIDsToFind.Contains(user.ID))
                        .Select(item => item.Name)
                        .ToList();

                    var newUserIds = lastworklog[i + 1].ProviderLog
                        .Where(s => s.IsDelete == false)
                        .Select(item => item.UserID)
                        .ToList();

                    var newUser = db.User
                        .Where(user => newUserIds.Contains(user.ID))
                        .Select(item => item.Name)
                        .ToList();

                    response += "<p><strong>Provider:</strong> " + string.Join(", ", oldUsers) + " => " + string.Join(", ", newUser) + "</p>";
                }



                if (lastworklog[i].Remark != lastworklog[i + 1].Remark)
                {
                    response += "<p><strong>Remark:</strong> " + lastworklog[i].Remark + " => " + lastworklog[i + 1].Remark + "</p>";
                }
            }

            response += "<a href=\"https://www.facebook.com/profile.php?id=100014450434050\" style=\"display:block;color:#007bff;text-decoration:none;margin:10px 0;\">Please Add Friend by clicking the link</a>";
            response += "<div style=\"margin-top:20px;\"><h1 style=\"color:#333;\">Create by:</h1></div>";
            response += "<h2 style=\"color:#555;margin:0;\">" + UserName + "</h2>";
            response += "</div>";

            return response;
        }

    }
}
