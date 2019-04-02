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
using Marten.Util;

namespace FanyiNetwork.Controllers
{
    [Authorize]
    public class ClassController : Controller
    {
        private readonly IDocumentStore _documentStore;
        public ClassController(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public IActionResult Index()
        {
            ViewBag.Title = "课程 - 凡易课导部管理系统";
            ViewData["active-name"] = "4";
            int uid = int.Parse(User.FindFirst(ClaimTypes.Sid).Value);
            using (var session = _documentStore.LightweightSession())
            {
                ViewData["userId"] = uid;
                ViewData["userName"] = session.Query<User>().FirstOrDefault(x => x.id == uid).name;
            }
            return View();
        }

        // POST api/values
        [HttpPost]
        [Route("api/[controller]/add")]
        public IActionResult Post([FromBody]ClassInfo model)
        {
            if (model.name == null || model.university == null) return BadRequest();
            if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "客服主管" || x.Value == "客服部" || x.Value == "经理办" || x.Value == "课导部" || x.Value == "课导部主管")))
            {
                using (var session = _documentStore.LightweightSession())
                {
                    if (session.Query<ClassInfo>().SingleOrDefault(x => x.name == model.name && x.university == model.university && x.professor == model.professor) != null)
                    {
                        return BadRequest("同所大学下的同一个教授不能重复添加同一门课程！");
                    }

                    session.Store<ClassInfo>(model);
                    session.SaveChanges();

                    ClassInfoFlow flow = new ClassInfoFlow();
                    flow.classId = model.id;
                    flow.Operator = User.Identity.Name;
                    flow.Operation = "新建课程.";
                    flow.Time = DateTime.Now;
                    session.Store<ClassInfoFlow>(flow);
                    session.SaveChanges();
                }
            }
            else
            {
                return BadRequest("你没有权限执行该操作!");
            }
            return Ok();
        }
        /// <summary>
        /// 根据课程名称检索
        /// </summary>
        /// <param name="nick"></param>
        /// <returns></returns>

        [HttpGet]
        [Route("api/[controller]")]
        public IActionResult Get(string nick)
        {
            List<ClassInfo> models;

            using (var session = _documentStore.LightweightSession())
            {
                if (nick != null && nick != "")
                {
                    models = session.Query<ClassInfo>().Where(x => (x.name != null && x.name.Contains(nick, StringComparison.OrdinalIgnoreCase)) || (x.professor != null && x.professor.Contains(nick, StringComparison.OrdinalIgnoreCase))).OrderByDescending(x => x.id).ToList();
                }
                else
                {
                    models = session.Query<ClassInfo>().OrderByDescending(x => x.id).Take(50).ToList();
                }

                if (models.Count > 0)
                {
                    var classorder = session.Query<ClassOrder>().Where(x => x.status != "已删除").ToList();
                    foreach (var item in models)
                    {
                        item.orderNum = classorder.Where(x => x.classId == item.id).Count();
                    }
                }
                return new ObjectResult(models);
            }
        }

        [HttpGet]
        [Route("api/[controller]/{id}")]
        public IActionResult Get(int id)
        {
            if (id == 0) return BadRequest();

            FanyiNetwork.Models.ClassInfo model = new FanyiNetwork.Models.ClassInfo();

            using (var session = _documentStore.LightweightSession())
            {
                model = session.Query<FanyiNetwork.Models.ClassInfo>().SingleOrDefault(x => x.id == id);

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
        public IActionResult SaveEdit([FromBody] ClassInfo model)
        {
            bool isChange = false;
            if (model == null) return BadRequest();
            if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "客服主管" || x.Value == "客服部" || x.Value == "经理办" || x.Value == "课导部" || x.Value == "课导部主管")))
            {
                using (var session = _documentStore.LightweightSession())
                {
                    ClassInfoFlow flow = new ClassInfoFlow();
                    flow.classId = model.id;
                    flow.Operator = User.Identity.Name;
                    flow.Time = DateTime.Now;
                    ClassInfo data = new ClassInfo();
                    data = session.Query<ClassInfo>().SingleOrDefault(x => x.id == model.id);
                    if (data.name != model.name)
                    {
                        flow.Operation += "修改课程名称：" + data.name + "为：" + model.name + ".";
                        isChange = true;
                    }
                    if (data.university != model.university)
                    {
                        flow.Operation += "修改大学名称：" + data.university + "为:" + model.university + ".";
                        isChange = true;
                    }
                    if (data.difficulty != model.difficulty)
                    {
                        flow.Operation += "修改难度系数：" + data.difficulty + "为:" + model.difficulty + ".";
                        isChange = true;
                    }
                    if (data.professor != model.professor)
                    {
                        flow.Operation += "修改教授名称：" + data.professor + "为:" + model.professor + ".";
                        isChange = true;
                    }
                    if (data.memo != model.memo)
                    {
                        flow.Operation += "修改备注信息：" + (data.memo == null || data.memo == "" ? "无" : data.memo) + "为:" + (model.memo == null || model.memo == "" ? "无" : model.memo) + ".";
                        isChange = true;
                    }
                    if (!isChange)
                    {
                        return BadRequest("未修改课程信息");
                    }
                    session.Store<ClassInfoFlow>(flow);
                    session.SaveChanges();
                    session.Store<ClassInfo>(model);
                    session.SaveChanges();
                }
            }
            else
            {
                return BadRequest("你没有权限执行该操作!");
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
            if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "客服主管" || x.Value == "客服部" || x.Value == "经理办" || x.Value == "课导部主管")))
            {
                FanyiNetwork.Models.ClassInfo model = new FanyiNetwork.Models.ClassInfo();
                using (var session = _documentStore.LightweightSession())
                {
                    model = session.Query<FanyiNetwork.Models.ClassInfo>().SingleOrDefault(x => x.id == id);
                    if (model != null)
                    {
                        session.Delete<ClassInfo>(model);
                        session.DeleteWhere<ClassOrder>(x => x.classId == model.id);
                        session.SaveChanges();
                        return Ok("删除成功");
                    }
                    else
                    {
                        return BadRequest();
                    }
                }
            }
            else
            {
                return BadRequest("你没有权限执行该操作!");
            }
        }


        //根据classid查询关联的订单/资料
        [HttpGet]
        public IActionResult GetOrder(int classid)
        {
            if (classid == 0) return BadRequest();
            List<ClassOrder> model;
            using (var session = _documentStore.LightweightSession())
            {
                model = session.Query<ClassOrder>().Where(x => x.classId == classid && x.status != "已删除").OrderBy(x => x.addTime).ToList();
            }
            if (model.Count() == 0) return BadRequest();
            return new ObjectResult(model);
        }

        //根据uid查询课程
        [HttpGet]
        public IActionResult GetClass(int uid)
        {
            if (uid == 0) return BadRequest();
            List<ClassInfo> model;
            using(var session = _documentStore.LightweightSession())
            {
                model = session.Query<ClassInfo>().Where(x => x.uId == uid).OrderBy(x => x.id).ToList();

            }
            return new ObjectResult(model);
        }

    }
}
