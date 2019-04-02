using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Marten;
using FanyiNetwork.Models;
using System.Security.Claims;

namespace FanyiNetwork.Controllers
{
    public class FileController : Controller
    {
        private IHostingEnvironment _environment;
        private readonly IDocumentStore _documentStore;
        public FileController(IHostingEnvironment environment, IDocumentStore documentStore)
        {
            _environment = environment;
            _documentStore = documentStore;
        }

        //上传图片
        [HttpPost]
        public async Task<IActionResult> PostModuleImg(IFormCollection collection, string orderId, string moduleId, string type,int contentid )
        {
                         
            if (orderId == null || orderId == "" || moduleId == null || moduleId == "" || type == null || type == ""|| contentid==0) return BadRequest();
            int uid = int.Parse(User.FindFirst(ClaimTypes.Sid).Value);
            bool issubmit = false;
            var files = collection.Files;
            long size = files.Sum(f => f.Length);
            var filePath = CheckDirectory(orderId, moduleId, type);
            foreach (var formFile in files)
            {
                if (formFile.Length > 0)
                {
                    string suffix = formFile.FileName.Substring(formFile.FileName.LastIndexOf("."));
                    var number = new Random().Next(10000, 99999);
                    string filename = orderId + moduleId + type + number + suffix;
                    string pathImg = Path.Combine(filePath, filename);
                    try
                    {
                        using (var stream = new FileStream(pathImg, FileMode.CreateNew))
                        {
                            await formFile.CopyToAsync(stream);
                        }
                    }
                    catch (IOException e)
                    {
                        return BadRequest("该文件已存在！请重命名后重新上传");
                    }
                    using (var session = _documentStore.LightweightSession())
                    {

                        if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "课导部主管" || x.Value == "经理办")))
                        {
                            issubmit = true;
                        }
                        else if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "课导部")))
                        {
                            var teacher = session.Query<ClassOrder>().SingleOrDefault(x => x.id == int.Parse(orderId)).teacher;
                            if (teacher == uid)
                            {
                                issubmit = true;
                            }
                            else
                            {
                                return BadRequest("你不是该课程的负责助教");
                            }
                        }
                        if (issubmit)
                        {
                            var model = session.Query<ModulesContent>().SingleOrDefault(x=>x.id==contentid);
                            if (model.contents == null || model.contents == "")
                            {
                                model.contents = "/uploads/" + orderId + "/" + moduleId + "/" + type + "/" + filename;
                            }
                            else
                            {
                                model.contents += "|" + "/uploads/" + orderId + "/" + moduleId + "/" + type + "/" + filename;
                            }
                            session.Store<ModulesContent>(model);
                            session.SaveChanges();
                        }
                        else
                        {
                            return BadRequest("你没有权限执行该操作!");
                        }
                    }
                }
            }
            return Ok(new { count = files.Count, size, filePath });
        }

        private string CheckDirectory(string orderid, string moduleid, string type)
        {
            var filePath = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(filePath)) Directory.CreateDirectory(filePath);

            filePath = Path.Combine(filePath, orderid);
            if (!Directory.Exists(filePath)) Directory.CreateDirectory(filePath);

            filePath = Path.Combine(filePath, moduleid);
            if (!Directory.Exists(filePath)) Directory.CreateDirectory(filePath);

            var attachmentPath = Path.Combine(filePath, type);
            if (!Directory.Exists(attachmentPath)) Directory.CreateDirectory(attachmentPath);

            return attachmentPath;
        }
    }
}
