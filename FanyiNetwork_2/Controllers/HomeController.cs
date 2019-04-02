using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.AspNetCore.Http;
using Marten;
using FanyiNetwork.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using FanyiNetwork.App_Code;
using Microsoft.AspNetCore.Http.Extensions;
using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace FanyiNetwork.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDocumentStore _documentStore;
        public HomeController(IDocumentStore documentStore)
        {

            _documentStore = documentStore;
        }
        [Authorize]
        public IActionResult Index()
        {
            ViewBag.Title = "总览 - 凡易课导部管理系统";
            ViewData["active-name"] = "1";

            return View();
        }
        public class ClassOrderDetail
        {
            public ClassOrder ClassOrderDetails { get; set; }
            public string clientname { get; set;}
            public User Cs { get; set; }
            public User Teacher { get; set; }
            public ClassInfo classinfo { get; set; }
        }

        //根据助教,客服,订单id或者课程名称,类型查询订单信息
        [HttpGet]
        [Route("api/[controller]")]
        public IActionResult Get(int teacher, int cs, string orderid, string type)
        {
            List<ClassOrder> model;
            List<ClassOrderDetail> list = new List<ClassOrderDetail>();
            List<ClassInfo> ls;
            var uid = int.Parse(User.FindFirst(ClaimTypes.Sid).Value);
            using (var session = _documentStore.LightweightSession())
            {
                model = session.Query<ClassOrder>().Where(x => x.status != "已结课" && x.status != "已删除").ToList();
                if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "课导部")))
                {
                    model = model.Where(x => x.teacher == uid).ToList();
                }
                else if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "客服部")))
                {
                    model = model.Where(x => x.cs == uid).ToList();
                }
                if (teacher != 0)
                {
                    model = model.Where(x => x.teacher == teacher).ToList();
                }
                if (cs != 0)
                {
                    model = model.Where(x => x.cs == cs).ToList();
                }
                if (orderid != null && orderid != "")
                {
                    ls = session.Query<ClassInfo>().ToList();
                    ls = ls.Where(z => z.name.ToLower().Contains(orderid.ToLower())).ToList();
                    model = model.Where(x => x.id.ToString().Contains(orderid) || ls.Any(z => z.id == x.classId)).ToList();
                }
                if (type != null && type != "0")
                {
                    model = model.Where(x => x.ordertape == type).OrderBy(x => x.id).ToList();

                }
                model = model.OrderByDescending(x => x.addTime.ToUniversalTime()).ToList();
                foreach (ClassOrder item in model)
                {

                    list.Add(new ClassOrderDetail()
                    {
                        ClassOrderDetails = item,
                        clientname = session.Query<Client>().SingleOrDefault(x => x.id == item.client)==null?"无": session.Query<Client>().SingleOrDefault(x => x.id == item.client).name,
                        Cs = session.Load<User>(item.cs),
                        Teacher = session.Load<User>(item.teacher),
                        classinfo = session.Load<ClassInfo>(item.classId)
                    });
                }
                return new ObjectResult(list);
            }
        }

        [Authorize]
        public IActionResult People()
        {
            ViewBag.Title = "人员 - 凡易课导部管理系统";
            ViewData["active-name"] = "6";
            return View();

        }

        [Authorize]
        public IActionResult Stats()
        {
            ViewBag.Title = "统计 - 凡易课导部管理系统";
            ViewData["active-name"] = "7";

            return View();
        }

        [Authorize]
        public IActionResult OrderList(int userid)
        {
            ViewBag.Title = "订单列表 - 凡易课导部管理系统";
            ViewData["active-name"] = "7";

            using (var session = _documentStore.LightweightSession())
            {
                ViewData["userId"] = userid;
                ViewData["userName"] = session.Query<User>().FirstOrDefault(x => x.id == userid).name;
            }
            return View();
        }

        [Authorize]
        public IActionResult MyList(string type)
        {
            ViewBag.Title = "我的" + type + " - 凡易课导部管理系统";

            int uid = int.Parse(User.FindFirst(ClaimTypes.Sid).Value);
            using (var session = _documentStore.LightweightSession())
            {
                ViewData["userId"] = uid;
                ViewData["userName"] = session.Query<User>().FirstOrDefault(x => x.id == uid).name;
            }
            return View();
        }

        [Authorize]
        public IActionResult MyClass()
        {
            ViewBag.title = "我添加的课程-凡易课导部管理系统";
            int uid = int.Parse(User.FindFirst(ClaimTypes.Sid).Value);
            using (var session = _documentStore.LightweightSession())
            {
                ViewData["userId"] = uid;
                ViewData["userName"] = session.Query<User>().FirstOrDefault(x => x.id == uid).name;
            }
            return View();
        }
       
    }
}
