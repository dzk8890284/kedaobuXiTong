using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Marten;
using System.Security.Claims;
using FanyiNetwork.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Http;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FanyiNetwork.Controllers
{

    [Authorize]
    public class ClassOrderController : Controller
    {

        private readonly IDocumentStore _documentStore;
        public ClassOrderController(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public IActionResult Index()
        {
            ViewBag.Title = "订单 - 凡易课导部管理系统";
            ViewData["active-name"] = "2";
            if (!User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "客服主管" || x.Value == "客服部" || x.Value == "经理办")))
            {
                return RedirectToAction("index", "home");
            }
            return View();
        }

        public IActionResult Detail()
        {
            return View();
        }

        public class ClassOdInfo
        {
            public int orderid { get; set; }
            public string className { get; set; }
            public string university { get; set; }
            public ClassOrder ClassOrders { get; set; }
            public List<modules> modules { get; set; }
        }
        public class modules
        {
            public int classOrderid { get; set; }
            public int moduleid { get; set; }
            public int name { get; set; }
            public int grade { get; set; }
            public List<ModulesContent> modulescontent { get; set; }
        }

        //查看课程详情
        [HttpGet]
        public IActionResult Get(int id)
        {
            if (id == 0) return BadRequest();
            ClassOrder model = new ClassOrder();
            ClassOdInfo od = new ClassOdInfo();
            using (var session = _documentStore.LightweightSession())
            {
                //查订单信息
                model = session.Query<ClassOrder>().SingleOrDefault(x => x.id == id);
                if (model == null) return BadRequest();
                od.ClassOrders = model;
                od.orderid = model.id;
                var classinfo = session.Query<ClassInfo>().SingleOrDefault(x => x.id == model.classId);
                od.className = classinfo.name;
                od.university = classinfo.university;
                List<modules> lsmx = new List<modules>();
                var modules = session.Query<Modules>().Where(x => x.classOrderId == model.id).OrderBy(x => x.id).ToList();
                modules mx = null;
                foreach (var item in modules)
                {
                    mx = new modules();
                    mx.grade = item.grade;
                    mx.classOrderid = item.classOrderId;
                    mx.name = item.no;
                    mx.moduleid = item.id;
                    mx.modulescontent = session.Query<ModulesContent>().Where(x => x.moduleId == item.id).ToList();

                    lsmx.Add(mx);
                }
                od.modules = lsmx;

                return new ObjectResult(od);
            }
        }

        //根据每周课程查询每周课程内容
        [HttpGet]
        public IActionResult getmodule(int moduleid)
        {
            if (moduleid == 0) return BadRequest();
            ModulesContent mc = null;
            using (var session = _documentStore.LightweightSession())
            {
                var model = session.Query<ModulesContent>().Where(x => x.moduleId == moduleid).OrderBy(x => x.id).ToList();
                if (model.Count() == 0)
                {
                    string[] defaultype = new string[4] { "Assignment", "Discussion", "Quiz", "Exam" };
                    foreach (var item in defaultype)
                    {
                        mc = new ModulesContent();
                        mc.moduleId = moduleid;
                        mc.contentType = item;
                        session.Store<ModulesContent>(mc);
                        session.SaveChanges();
                        model.Add(mc);
                    }

                }
                return new ObjectResult(model);
            }
        }
        //新增每周课程信息
        [HttpPost]
        public IActionResult add(int classOrderid)
        {
            if (classOrderid == 0) return BadRequest();
            Modules m = new Modules();
            ClassOrder co = new ClassOrder();
            bool issubmit = false;
            int uid = int.Parse(User.FindFirst(ClaimTypes.Sid).Value);
            using (var session = _documentStore.LightweightSession())
            {
                co = session.Query<ClassOrder>().SingleOrDefault(x => x.id == classOrderid);
                var model = session.Query<Modules>().Where(x => x.classOrderId == classOrderid).OrderBy(x => x.id).ToList();
                if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "课导部主管" || x.Value == "经理办")))
                {
                    issubmit = true;
                }
                else if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "课导部")))
                {

                    if (co.teacher == uid)
                    {
                        issubmit = true;
                    }
                    else
                    {
                        return BadRequest("你不是该课程的负责助教!");
                    }
                }
                if (issubmit)
                {
                    if (model.Count() > 0)
                    {
                        var maxmodel = model.OrderByDescending(x => x.no).FirstOrDefault();
                        m.no = maxmodel.no + 1;
                    }
                    else
                    {
                        m.no = 1;
                    }
                    m.classOrderId = classOrderid;
                    session.Store<Modules>(m);
                    session.SaveChanges();
                    co.lastUpdate = DateTime.Now;
                    session.Store<ClassOrder>(co);
                    session.SaveChanges();
                    ClassFlow flow = new ClassFlow();
                    flow.classorderId = co.id;
                    flow.Operator = User.Identity.Name;
                    flow.Operation = "新建课程模块.";
                    flow.Time = DateTime.Now;
                    session.Store<ClassFlow>(flow);
                    session.SaveChanges();
                }
                else
                {
                    return BadRequest("你没有权限执行该操作!");
                }

            }
            return Ok();
        }

        //删除图片
        [HttpGet]
        public IActionResult DelImg(int orderid, int contentId, string imgurl)
        {
            if (orderid == 0 || contentId == 0 || imgurl == null) return BadRequest();
            int uid = int.Parse(User.FindFirst(ClaimTypes.Sid).Value);
            bool issubmit = false;
            using (var session = _documentStore.LightweightSession())
            {
                if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "课导部主管" || x.Value == "经理办")))
                {
                    issubmit = true;
                }
                else if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "课导部")))
                {
                    var teacher = session.Query<ClassOrder>().SingleOrDefault(x => x.id == orderid).teacher;
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
                    var model = session.Query<ModulesContent>().SingleOrDefault(x => x.id == contentId);
                    //转换为绝对路径
                    string path = System.IO.Path.GetFullPath("wwwroot/" + imgurl);
                    //删除本地
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }
                    //删除数据库图片
                    if (model.contents == imgurl)
                    {
                        model.contents = model.contents.Replace(imgurl, "");
                    }
                    else if (model.contents.IndexOf(imgurl) >= 0)
                    {
                        model.contents = model.contents.Replace("|" + imgurl, "");
                    }
                    session.Store<ModulesContent>(model);
                    session.SaveChanges();
                }
                else
                {
                    return BadRequest("你没有权限执行该操作!");
                }
            }
            return Ok();
        }

        //查看回顾资料  
        [HttpGet]
        public IActionResult Check(int classId)
        {
            if (classId == 0) return BadRequest();
            using (var session = _documentStore.LightweightSession())
            {
                var order = session.Query<ClassOrder>().Where(x => x.status == "已结课" && x.classId == classId).ToList();
                return new ObjectResult(order);
            }

        }

        //新建课程订单
        [HttpPost]
        [Route("/api/[controller]/create")]
        public IActionResult post([FromBody]ClassOrder model)
        {
            if (model == null) return BadRequest();
            if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "客服主管" || x.Value == "客服部" || x.Value == "经理办")))
            {
                using (var session = _documentStore.LightweightSession())
                {
                    if (model.teacher == 0)
                    {
                        model.status = "待分配";
                    }
                    model.ordertape = "课程订单";
                    model.cs = int.Parse(User.FindFirst(ClaimTypes.Sid).Value);
                    model.addTime = DateTime.Now;
                    session.Store<ClassOrder>(model);
                    session.SaveChanges();
                    ClassFlow flow = new ClassFlow();
                    flow.classorderId = model.id;
                    flow.Operator = User.Identity.Name;
                    flow.Operation = "新建课程订单.";
                    flow.Time = DateTime.Now;
                    session.Store<ClassFlow>(flow);
                    session.SaveChanges();
                }
            }
            else
            {
                return BadRequest("你没有权限执行该操作!");
            }
            return Ok();
        }

        //分配订单
        [HttpPost]
        [Route("api/[controller]/assign")]
        public IActionResult Assign([FromBody]ClassOrder model)
        {
            if (model == null) return BadRequest();
            if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "课导部主管" || x.Value == "经理办")))
            {
                using (var session = _documentStore.LightweightSession())
                {
                    var co = session.Query<ClassOrder>().FirstOrDefault(x => x.id == model.id);
                    if (co != null)
                    {
                        co.status = "进行中";
                        co.cheif = int.Parse(User.FindFirst(ClaimTypes.Sid).Value);
                        co.teacher = model.teacher;
                        co.assignTime = DateTime.Now;
                        session.Store<ClassOrder>(co);
                        session.SaveChanges();

                        ClassFlow flow = new ClassFlow();
                        flow.classorderId = model.id;
                        flow.Operator = User.Identity.Name;
                        flow.Operation = "成功分配，标记该单为进行中.";
                        flow.Time = DateTime.Now;
                        session.Store<ClassFlow>(flow);
                        session.SaveChanges();
                    }
                    else
                    {
                        return BadRequest("分配失败!");
                    }
                }
            }
            else
            {
                return BadRequest("你没有权限执行该操作!");
            }
            return Ok("分配成功");
        }

        //删除每周课程
        [HttpGet]
        public IActionResult Del(int orderid, int moduleid)
        {
            if (orderid == 0 || moduleid == 0) return BadRequest();
            int uid = int.Parse(User.FindFirst(ClaimTypes.Sid).Value);
            bool issubmit = false;
            ClassOrder co = new ClassOrder();
            using (var session = _documentStore.LightweightSession())
            {
                co = session.Query<ClassOrder>().SingleOrDefault(x => x.id == orderid);
                var model = session.Query<Modules>().Where(x => x.classOrderId == orderid).ToList();
                if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "课导部主管" || x.Value == "经理办")))
                {
                    issubmit = true;
                }
                else if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "课导部")))
                {
                    var teacher = session.Query<ClassOrder>().SingleOrDefault(x => x.id == orderid).teacher;
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
                    if (model.Count() > 0)
                    {
                        var maxmodel = model.OrderByDescending(x => x.id).FirstOrDefault();
                        if (maxmodel.id == moduleid)
                        {
                            session.Delete<Modules>(maxmodel);
                            session.DeleteWhere<ModulesContent>(x => x.moduleId == moduleid);
                            session.SaveChanges();
                            co.lastUpdate = DateTime.Now;
                            session.Store<ClassOrder>(co);
                            session.SaveChanges();
                            ClassFlow flow = new ClassFlow();
                            flow.classorderId = co.id;
                            flow.Operator = User.Identity.Name;
                            flow.Operation = "删除课程模块";
                            flow.Time = DateTime.Now;
                            session.Store<ClassFlow>(flow);
                            session.SaveChanges();

                        }
                        else
                        {
                            return BadRequest("删除失败");
                        }
                    }
                }
                else
                {
                    return BadRequest("你没有权限执行该操作!");
                }
            }

            return Ok("删除成功");
        }

        //完成课程
        [HttpGet]
        public IActionResult Finish(int orderid)
        {
            if (orderid == 0) return BadRequest();
            int uid = int.Parse(User.FindFirst(ClaimTypes.Sid).Value);
            //是否为课导部
            bool isKdb = User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "课导部"));
            //是否为课导部主管,经理办
            bool isManage = User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "课导部主管" || x.Value == "经理办"));
            using (var session = _documentStore.LightweightSession())
            {
                var model = session.Query<ClassOrder>().SingleOrDefault(x => x.id == orderid);
                if (model.status == "进行中")
                {
                    if ((isKdb && model.teacher == uid) || isManage)
                    {
                        model.status = "已完成";
                        model.finishTime = DateTime.Now;
                        session.Store<ClassOrder>(model);
                        session.SaveChanges();
                        ClassFlow flow = new ClassFlow();
                        flow.classorderId = model.id;
                        flow.Operator = User.Identity.Name;
                        flow.Operation = "完成了课程，标记该单为已完成.";
                        flow.Time = DateTime.Now;
                        session.Store<ClassFlow>(flow);
                        session.SaveChanges();
                    }
                    else
                    {
                        return BadRequest("你没有权限执行该操作!");

                    }
                }
                else
                {
                    return BadRequest("该订单的状态不是进行中，不能变更为已完成");
                }
            }
            return Ok();
        }
        //结课
        [HttpGet]
        public IActionResult Over(int orderid)
        {
            if (orderid == 0) return BadRequest();
            using (var session = _documentStore.LightweightSession())
            {
                if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "课导部主管" || x.Value == "经理办")))
                {
                    var model = session.Query<ClassOrder>().SingleOrDefault(x => x.id == orderid);
                    if (model.status == "已完成")
                    {
                        model.status = "已结课";
                        model.closeTime = DateTime.Now;
                        session.Store<ClassOrder>(model);
                        session.SaveChanges();
                        ClassFlow flow = new ClassFlow();
                        flow.classorderId = model.id;
                        flow.Operator = User.Identity.Name;
                        flow.Operation = "审核通过，标记该单为已结课.";
                        flow.Time = DateTime.Now;
                        session.Store<ClassFlow>(flow);
                        session.SaveChanges();
                    }
                    else if (model.status == "已结课")
                    {
                        return BadRequest("该订单已结课!");
                    }
                    else
                    {
                        return BadRequest("该订单的状态未完成，不能结课!");
                    }
                }
                else
                {
                    return BadRequest("你没有权限执行该操作!");
                }
            }
            return Ok();
        }
        //重新开启课程
        [HttpGet]
        public IActionResult Reopen(int orderid)
        {
            if (orderid == 0) return BadRequest();
            using (var session = _documentStore.LightweightSession())
            {
                if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "课导部主管" || x.Value == "经理办")))
                {
                    var model = session.Query<ClassOrder>().SingleOrDefault(x => x.id == orderid);
                    if (model.status == "已结课")
                    {
                        model.status = "进行中";
                        session.Store<ClassOrder>(model);
                        session.SaveChanges();
                        ClassFlow flow = new ClassFlow();
                        flow.classorderId = model.id;
                        flow.Operator = User.Identity.Name;
                        flow.Operation = "重新开启，标记该单为进行中.";
                        flow.Time = DateTime.Now;
                        session.Store<ClassFlow>(flow);
                        session.SaveChanges();
                    }
                    else
                    {
                        return BadRequest("该订单的状态未结课，不能重新开启!");
                    }
                }
                else
                {
                    return BadRequest("你没有权限执行该操作!");
                }
            }
            return Ok();
        }

        //得分
        [HttpGet]
        public IActionResult save(int orderid, int contentid, int grade)
        {
            if (orderid == 0 || contentid == 0 || grade == 0) return BadRequest();
            int uid = int.Parse(User.FindFirst(ClaimTypes.Sid).Value);
            bool issubmit = false;
            using (var session = _documentStore.LightweightSession())
            {
                if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "课导部主管" || x.Value == "经理办")))
                {
                    issubmit = true;
                }
                else if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "课导部")))
                {
                    var teacher = session.Query<ClassOrder>().SingleOrDefault(x => x.id == orderid).teacher;
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
                    //得分存到子级
                    var model = session.Query<ModulesContent>().FirstOrDefault(x => x.id == contentid);
                    if (model.contents != null && model.contents != "")
                    {
                        model.grade = grade;
                        session.Store<ModulesContent>(model);
                        session.SaveChanges();
                        //根据子级分数算出平均分存到父级
                        var content = session.Query<ModulesContent>().Where(x => x.modulesContentId == model.modulesContentId && x.grade > 0).ToList();
                        if (content.Count() > 0)
                        {
                            int number = content.Count();
                            int sumgrade = content.Sum(x => x.grade);
                            var c = session.Query<ModulesContent>().SingleOrDefault(x => x.id == model.modulesContentId && x.modulesContentId == 0);
                            c.grade = sumgrade / number;
                            session.Store<ModulesContent>(c);
                            session.SaveChanges();
                        }
                        //根据父级分数算出平均分存到相对应的每周课程
                        var mcontent = session.Query<ModulesContent>().Where(x => x.moduleId == model.moduleId && x.grade > 0 && x.modulesContentId == 0).OrderBy(x => x.id).ToList();
                        if (mcontent.Count() > 0)
                        {
                            int number = mcontent.Count();
                            int sumgrade = mcontent.Sum(x => x.grade);
                            var modules = session.Query<Modules>().SingleOrDefault(x => x.id == model.moduleId);
                            modules.grade = sumgrade / number;
                            session.Store<Modules>(modules);
                            session.SaveChanges();
                        }
                        else
                        {
                            return BadRequest();
                        }
                    }
                    else
                    {
                        return BadRequest("未上传图片,不能提交得分");
                    }
                }
                else
                {
                    return BadRequest("你没有权限执行该操作!");
                }
            }
            return Ok();
        }

        //新增每周内容
        [HttpPost]
        public IActionResult Addcontent(int orderid, int contentid)
        {
            if (orderid == 0 || contentid == 0) return BadRequest();
            int uid = int.Parse(User.FindFirst(ClaimTypes.Sid).Value);
            //是否为课导部
            bool isKdb = User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "课导部"));
            //是否为课导部主管,经理办
            bool isManage = User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "课导部主管" || x.Value == "经理办"));
            ModulesContent mc = new ModulesContent();
            using (var session = _documentStore.LightweightSession())
            {
                var teacher = session.Query<ClassOrder>().SingleOrDefault(x => x.id == orderid).teacher;
                var model = session.Query<ModulesContent>().SingleOrDefault(x => x.id == contentid);
                if (model != null)
                {
                    if ((isKdb && teacher == uid) || isManage)
                    {
                        mc.modulesContentId = contentid;
                        mc.moduleId = model.moduleId;
                        mc.contentType = model.contentType;
                        session.Store<ModulesContent>(mc);
                        session.SaveChanges();
                    }
                    else
                    {
                        return BadRequest("你没有权限执行该操作!");
                    }
                }
                else
                {
                    return BadRequest();
                }
            }
            return Ok();
        }

        //删除每周内容
        [HttpGet]
        public IActionResult Delcontent(int orderid, int contentid)
        {
            if (orderid == 0 || contentid == 0) return BadRequest();
            int uid = int.Parse(User.FindFirst(ClaimTypes.Sid).Value);
            //是否为课导部
            bool isKdb = User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "课导部"));
            //是否为课导部主管,经理办
            bool isManage = User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "课导部主管" || x.Value == "经理办"));
            using (var session = _documentStore.LightweightSession())
            {
                var teacher = session.Query<ClassOrder>().SingleOrDefault(x => x.id == orderid).teacher;
                if ((isKdb && teacher == uid) || isManage)
                {
                    var model = session.Query<ModulesContent>().SingleOrDefault(x => x.id == contentid);
                    if (model != null)
                    {
                        session.Delete<ModulesContent>(model);
                        session.SaveChanges();
                        var content = session.Query<ModulesContent>().Where(x => x.modulesContentId == model.modulesContentId && x.grade > 0).ToList();
                        var mcontent = session.Query<ModulesContent>().SingleOrDefault(x => x.id == model.modulesContentId);
                        //重新计算父级得分，父级下面不存在子级的情况
                        if (content.Count() == 0)
                            mcontent.grade = 0;
                        //重新计算父级得分，父级下面存在子级的情况
                        else
                        {
                            int number = content.Count();
                            int sumgrade = content.Sum(x => x.grade);
                            mcontent.grade = sumgrade / number;
                        }
                        session.Store<ModulesContent>(mcontent);
                        session.SaveChanges();
                        var fcontent = session.Query<ModulesContent>().Where(x => x.moduleId == model.moduleId && x.modulesContentId == 0 && x.grade > 0).ToList();
                        var f = session.Query<Modules>().SingleOrDefault(x => x.id == model.moduleId);
                        //重新计算父级对应的每周课程得分,父级对应的每周课程下面不存在子级
                        if (fcontent.Count() == 0)
                            f.grade = 0;
                        //重新计算父级对应的每周课程得分,父级对应的每周课程下面存在子级
                        else
                        {
                            int fnumber = fcontent.Count();
                            int fsumgrade = fcontent.Sum(x => x.grade);
                            f.grade = fsumgrade / fnumber;
                        }
                        session.Store<Modules>(f);
                        session.SaveChanges();
                    }
                    else
                    {
                        return BadRequest();
                    }
                }
                else
                {
                    return BadRequest("你没有权限执行该操作!");
                }

            }
            return Ok("删除成功!");
        }

        //根据父级的每周内容查询相对应的子级
        [HttpGet]
        public IActionResult GetContentSubset(int contentid)
        {
            if (contentid == 0) return BadRequest();
            using (var session = _documentStore.LightweightSession())
            {
                var model = session.Query<ModulesContent>().Where(x => x.modulesContentId == contentid).OrderBy(x => x.id).ToList();
                return new ObjectResult(model);
            }
        }

        //删除订单
        [HttpGet]
        public IActionResult DelOrder(int orderid)
        {
            if (orderid == 0) return BadRequest();
            ClassOrder model = new ClassOrder();
            int uid = int.Parse(User.FindFirst(ClaimTypes.Sid).Value);
            //是否为客服部,课导部
            bool isKfb = User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "客服部" || x.Value == "课导部"));
            //是否为课导部主管,客服主管,经理办
            bool isManage = User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "课导部主管" || x.Value == "客服主管" || x.Value == "经理办"));
            using (var session = _documentStore.LightweightSession())
            {
                model = session.Query<ClassOrder>().SingleOrDefault(x => x.id == orderid);
                if (model != null)
                {
                    if ((isKfb && model.cs == uid) || isManage)
                    {
                        model.status = "已删除";
                        session.Store<ClassOrder>(model);
                        session.SaveChanges();
                    }
                    else
                    {
                        return BadRequest("你没有权限执行该操作!");
                    }
                }
            }
            return Ok("删除成功!");
        }

        //更新订单
        [HttpPost]
        public IActionResult UpdateOrder([FromBody] ClassOrder model)
        {
            if (model == null) return BadRequest();
            if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "客服部" || x.Value == "客服主管" || x.Value == "经理办")))
            {
                using (var session = _documentStore.LightweightSession())
                {
                    session.Store<ClassOrder>(model);
                    session.SaveChanges();
                    ClassFlow flow = new ClassFlow();
                    flow.classorderId = model.id;
                    flow.Operator = User.Identity.Name;
                    flow.Operation = "修改了基本信息.";
                    flow.Time = DateTime.Now;
                    session.Store<ClassFlow>(flow);
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
        public IActionResult GetOrderList(string userid)
        {
            if (userid != null || userid != "")
            {
                int uid = int.Parse(userid);
                List<ClassOrder> model;
                using (var session = _documentStore.LightweightSession())
                {
                    model = session.Query<ClassOrder>().Where(x => x.teacher == uid).OrderBy(x => x.addTime).ToList();
                    return new ObjectResult(model);
                }
            }
            else
            {
                return BadRequest();
            }
        }
        [HttpGet]
        public IActionResult GetMyOrderList(string type)
        {
            if (type != null || type != "")
            {
                int uid = int.Parse(User.FindFirst(ClaimTypes.Sid).Value);
                List<ClassOrder> model;
                using (var session = _documentStore.LightweightSession())
                {
                    model = session.Query<ClassOrder>().Where(x => (x.teacher == uid || x.cs == uid) && x.ordertape == type).OrderBy(x => x.addTime).ToList();
                    return new ObjectResult(model);
                }
            }
            else
            {
                return BadRequest();
            }
        }

        //同一课程下的订单/资料信息
        public IActionResult OrderInfo()
        {
            return View();
        }
    }
}


