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

namespace FanyiNetwork.Controllers
{
    [Authorize]
    public class ClientController : Controller
    {
        private readonly IDocumentStore _documentStore;
        public ClientController(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public IActionResult Index()
        {
            ViewBag.Title = "客户 - 凡易课导部管理系统";
            ViewData["active-name"] = "5";

            return View();
        }
        //根据出生年月计算年龄
        public int GetAgeByBirthdate(DateTime birthdate)
        {
            DateTime now = DateTime.Now;
            int age = now.Year - birthdate.Year;
            if (now.Month < birthdate.Month || (now.Month == birthdate.Month && now.Day < birthdate.Day))
            {
                age--;
            }
            return age < 0 ? 0 : age;
        }

        // POST api/values
        [HttpPost]
        [Route("api/[controller]/add")]
        public IActionResult Post([FromBody]Client model)
        {
            if (model == null) return BadRequest();
            using (var session = _documentStore.LightweightSession())
            {
                if (session.Query<Client>().SingleOrDefault(x => x.name == model.name) != null)
                {
                    return BadRequest("该客户昵称已存在！");
                }
                model.age = GetAgeByBirthdate(model.birthday).ToString();
                session.Store<Client>(model);
                session.SaveChanges();
            }
            return Ok();
        }
        /// <summary>
        /// 根据昵称检索
        /// </summary>
        /// <param name="nick"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/[controller]")]
        public IActionResult Get(string nick, int pagenum, int pagesize)
        {
            listData model = new listData();
            List<Client> models;
            if (pagenum == 0)
                pagenum = 1;
            int pageTotal = 0;
            using (var session = _documentStore.LightweightSession())
            {
                if (nick != null && nick != "")
                {
                    models = session.Query<Client>().OrderBy(x => x.id).Where(x => x.name.ToLower().Contains(nick.ToLower())).Skip(pagesize * (pagenum - 1)).Take(pagesize).ToList();
                }
                else
                {
                    models = session.Query<Client>().OrderBy(x => x.id).Skip(pagesize * (pagenum - 1)).Take(pagesize).ToList();
                }
                foreach (var item in models)
                {
                    item.age = GetAgeByBirthdate(item.birthday).ToString();
                }
                pageTotal = session.Query<Client>().Count();
                model.pageTotal = pageTotal;
                model.list = models;
                return new ObjectResult(model);
            }
        }
        public class listData
        {
            public int pageTotal { get; set; }
            public List<Client> list { get; set; }
        }
        //查询所有列表
        [HttpGet]
        public IActionResult GetList()
        {
            List<Client> models;
            using (var session = _documentStore.LightweightSession())
            {
                models = session.Query<Client>().OrderBy(x => x.id).ToList();
                //foreach (var item in models)
                //{
                //    item.age = GetAgeByBirthdate(item.birthday).ToString();
                //}
                return new ObjectResult(models);

            }
        }
        [HttpGet]
        [Route("api/[controller]/{id}")]
        public IActionResult Get(int id)
        {
            if (id == 0) return BadRequest();

            FanyiNetwork.Models.Client model = new FanyiNetwork.Models.Client();

            using (var session = _documentStore.LightweightSession())
            {
                model = session.Query<FanyiNetwork.Models.Client>().SingleOrDefault(x => x.id == id);
            }

            if (model == null) return BadRequest();

            return new ObjectResult(model);
        }

        /// <summary>
        /// 返回编辑页面 
        /// </summary>
        /// <returns></returns>
        public IActionResult Edit()
        {
            return View();
        }
        /// <summary>
        /// 保存编辑数据
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/[controller]/edit")]
        public IActionResult SaveEdit([FromBody] Client model)
        {
            if (model == null) return BadRequest();
            using (var session = _documentStore.LightweightSession())
            {
                var data = session.Query<Client>().SingleOrDefault(x => x.id == model.id);
                if (data.birthday != model.birthday)
                {
                    model.age = GetAgeByBirthdate(model.birthday).ToString();
                }
                session.Store<Client>(model);
                session.SaveChanges();
            }
            return Ok("保存成功");
        }
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/[controller]/del")]
        public IActionResult Del(int id)
        {
            if (id == 0) return BadRequest();
            FanyiNetwork.Models.Client model = new FanyiNetwork.Models.Client();
            using (var session = _documentStore.LightweightSession())
            {
                model = session.Query<FanyiNetwork.Models.Client>().SingleOrDefault(x => x.id == id);
                if (model != null)
                {
                    session.Delete<Client>(model);
                    session.SaveChanges();
                    return Ok("删除成功");
                }
                else
                {
                    return BadRequest();
                }
            }
        }
    }
}
