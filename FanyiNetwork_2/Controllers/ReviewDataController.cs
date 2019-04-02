using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Marten;
using System.Security.Claims;
using FanyiNetwork.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FanyiNetwork.Controllers
{

    [Authorize]
    public class ReviewDataController : Controller
    {
        private readonly IDocumentStore _documentStore;
        public ReviewDataController(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }
        public IActionResult Index()
        {
            ViewBag.Title = "回顾 - 凡易课导部管理系统";
            ViewData["active-name"] = "3";
            return View();
        }

        //新建回顾资料
        [HttpPost]
        public IActionResult add([FromBody]ClassOrder model)
        {
            if (model == null) return BadRequest();
            if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "课导部主管" || x.Value == "课导部" || x.Value == "经理办")))
            {
                using (var session = _documentStore.LightweightSession())
                {
                    model.ordertape = "回顾资料";
                    model.status = "进行中";
                    model.cs = int.Parse(User.FindFirst(ClaimTypes.Sid).Value);
                    model.teacher = int.Parse(User.FindFirst(ClaimTypes.Sid).Value);
                    model.addTime = DateTime.Now;
                    session.Store<ClassOrder>(model);
                    session.SaveChanges();
                }
            }
            else
            {
                return BadRequest("你没有权限执行该操作!");
            }
            return Ok();
        }
    }
}
