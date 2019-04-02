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
    public class TeamController : Controller
    {
        private readonly IDocumentStore _documentStore;
        public TeamController(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public IActionResult Index()
        {
            ViewBag.Title = "小组 - 凡易单号管理系统";
            ViewData["active-name"] = "7";

            return View();
        }
        /// <summary>
        ///   新建小组
        /// </summary>
        ///// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/[controller]/add")]
        public IActionResult Add([FromBody] Team model)
        {
            if (model.teamName == "" || model.teamName == null) return BadRequest();
            if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "人事部" || x.Value == "经理办")))
            {
                using (var session = _documentStore.LightweightSession())
                {
                    session.Store<Team>(model);
                    session.SaveChanges();
                }
            }
            else
            {
                return BadRequest("你没有权限执行该操作!");
            }
            return Ok();
        }

        [HttpGet]
        [Route("api/[controller]/create")]
        public IActionResult Create(int uid, int teamid)
        {
            if (uid == 0 || teamid == 0) return BadRequest();
            if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "人事部" || x.Value == "经理办")))
            {
                using (var session = _documentStore.LightweightSession())
                {
                    var model = session.Query<User>().FirstOrDefault(x => x.id == uid);
                    if (model != null)
                    {
                        model.teamId = teamid;
                        model.isTeamLeader = false;
                        session.Store<User>(model);
                        session.SaveChanges();
                    }
                    else
                        return BadRequest();
                }
            }
            else
            {
                return BadRequest("你没有权限执行该操作!");
            }
            return Ok();
        }
        /// <summary>
        /// 设置/取消组长
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/[controller]/set")]
        public IActionResult Set(int uid, string type)
        {
            if (uid == 0 || type == "" || type == null) return BadRequest();
            if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "人事部" || x.Value == "经理办")))
            {
                using (var session = _documentStore.LightweightSession())
                {
                    var model = session.Query<User>().FirstOrDefault(x => x.id == uid);
                    if (model != null)
                    {
                        if (type == "1")
                        {
                            var isteamLeader = session.Query<User>().Where(x => x.teamId == model.teamId).Any(x => x.isTeamLeader == true);
                            if (isteamLeader)
                            {
                                return BadRequest("本小组已经存在组长，不能重复设置!");
                            }
                            else
                            {
                                model.isTeamLeader = true;
                            }
                        }
                        else
                        {
                            model.isTeamLeader = false;
                        }
                        session.Store<User>(model);
                        session.SaveChanges();
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
            return Ok();
        }


        /// <summary>
        /// 删除小组
        /// </summary>
        /// <param name="teamid"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/[controller]/del")]
        public IActionResult Del(int teamid)
        {
            if (teamid == 0) return BadRequest();
            if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "人事部" || x.Value == "经理办")))
            {
                using (var session = _documentStore.LightweightSession())
                {
                    var model = session.Query<Team>().FirstOrDefault(x => x.id == teamid);
                    if (model != null)
                    {
                        session.Delete<Team>(model);
                        var user = session.Query<User>().Where(x => x.teamId == model.id).ToList();
                        if (user.Count() > 0)
                        {
                            foreach (var item in user)
                            {
                                item.teamId = 0;
                                if (item.isTeamLeader == true)
                                    item.isTeamLeader = false;
                                session.Store<User>(item);
                            }
                        }
                        session.SaveChanges();
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
            return Ok("删除成功!");
        }

        /// <summary>
        /// 查询所有的小组
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/[controller]")]
        public IActionResult Get()
        {
            List<Team> model;
            using (var session = _documentStore.LightweightSession())
            {
                model = session.Query<Team>().OrderBy(x => x.id).ToList();
            }
            return new ObjectResult(model);
        }

        [HttpGet]
        [Route("api/[controller]/getTeamate")]
        public IActionResult GetTeamate()
        {
            List<dataList> list = new List<dataList>();
            using (var session = _documentStore.LightweightSession())
            {
                var teams = session.Query<Team>().ToList();
                foreach (var item in teams)
                {
                    list.Add(new dataList()
                    {
                        team = item,
                        ls = session.Query<User>().Where(x => x.teamId == item.id).OrderByDescending(x => x.id).ToList()
                    });
                }
                return new ObjectResult(list);
            }
        }

        public class dataList
        {
            public Team team { get; set; }
            public List<User> ls { get; set; }

        }



        /// <summary>
        /// 查询未分配人员
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        [HttpGet("api/[controller]/group/{group}")]
        public IActionResult Get(string group)
        {
            var model = new List<FanyiNetwork.Models.User>();

            using (var session = _documentStore.LightweightSession())
            {
                model = session.Query<FanyiNetwork.Models.User>().Where(x => x.isTerminated == false).ToList();
                model = model.Where(x => x.group == group && x.teamId == 0).ToList();
            }
            if (model == null) return BadRequest();

            return new ObjectResult(model);
        }

    }
}
