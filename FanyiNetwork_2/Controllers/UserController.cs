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
    [Route("api/[controller]")]
    public class UserController : Controller
    {
        private readonly IDocumentStore _documentStore;
        public UserController(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }
        // GET: api/values
        [HttpGet]
        public IActionResult Get()
        {
            var model = new List<FanyiNetwork.Models.User>();

            using (var session = _documentStore.LightweightSession())
            {
                model = session.Query<FanyiNetwork.Models.User>().Where(x => x.isTerminated == false).ToList();
            }

            if (model == null) return BadRequest();

            return new ObjectResult(model);
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            if (id == 0) return BadRequest();

            FanyiNetwork.Models.User model = new FanyiNetwork.Models.User();

            using (var session = _documentStore.LightweightSession())
            {
                model = session.Query<FanyiNetwork.Models.User>().SingleOrDefault(x => x.id == id);
            }

            if (model == null) return BadRequest();

            return new ObjectResult(model);
        }
        [HttpGet("group/{group}")]
        public IActionResult Get(string group)
        {
            var model = new List<FanyiNetwork.Models.User>();

            using (var session = _documentStore.LightweightSession())
            {
                model = session.Query<FanyiNetwork.Models.User>().Where(x=>x.isTerminated == false).ToList();

                switch (group)
                {
                    case "负责助教":
                        model = model.Where(x => x.group.IsOneOf(new string[] { "课导部", "课导部主管", "经理办" })).ToList();
                        break;
                    case "负责客服":
                        model = model.Where(x => x.group.IsOneOf(new string[] { "客服部", "客服主管", "经理办" })).ToList();
                        break;
                    default:
                        model = model.Where(x => x.group == group).ToList();
                        break;
                }

                
            }

            if (model == null) return BadRequest();

            return new ObjectResult(model);
        }

        [HttpGet("GetLoginHistory")]
        public IActionResult GetLoginHistory(int uid)
        {
            var model = new List<FanyiNetwork.Models.LoginHistory>();

            using (var session = _documentStore.LightweightSession())
            {
                model = session.Query<FanyiNetwork.Models.LoginHistory>().Where(x => x.userID == uid).ToList();
            }

            if (model == null) return BadRequest();

            return new ObjectResult(model);
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        public IActionResult Put([FromBody]FanyiNetwork.Models.User model)
        {
            if (!User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "经理办" || x.Value == "人事部")))
            {
                return BadRequest("你没有权限执行该操作!");
            }

            if (model == null) return BadRequest();

            using (var session = _documentStore.LightweightSession())
            {
                session.Store<FanyiNetwork.Models.User>(model);
                session.SaveChanges();
            }

            return Ok();
        }

        [HttpGet("GetTeacherStats")]
        public IActionResult GetTeacherStats(string userGroup)
        {
            List<TeacherStatsVM> stats = new List<TeacherStatsVM>();

            using (var session = _documentStore.LightweightSession())
            {
                var editors = session.Query<User>().Where(x => x.group == userGroup && x.isTerminated == false).ToList();
                foreach (User item in editors)
                {
                    TeacherStatsVM target = new TeacherStatsVM();
                    target.userid = item.id;
                    target.name = item.name;

                    var targetOrders = session.Query<ClassOrder>().Where(x => x.teacher == item.id).ToList();

                    foreach (ClassOrder order in targetOrders)
                    {
                        if (order.ordertape == "课程订单") target.totalOrder += 1;
                        if (order.ordertape == "回顾资料") target.totalStudyOrder += 1;
                    }

                    stats.Add(target);
                }
            }

            return new ObjectResult(stats);
        }
    }
}
