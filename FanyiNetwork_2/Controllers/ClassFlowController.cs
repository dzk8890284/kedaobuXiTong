using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using FanyiNetwork.Models;
using Marten;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FanyiNetwork.Controllers
{
    [Route("api/[controller]")]
    public class ClassFlowController : Controller
    {
        private readonly IDocumentStore _documentStore;
        public ClassFlowController(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        // GET: api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            List<ClassFlow> models;

            using (var session = _documentStore.LightweightSession())
            {
                models = session.Query<ClassFlow>().Where(x => x.classorderId == id).OrderBy(x => x.Time).ToList();
            }

            return new ObjectResult(models);
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]ClassFlow model)
        {
            using (var session = _documentStore.LightweightSession())
            {
                model.Operator = User.Identity.Name;
                model.Time = DateTime.Now;

                session.Store<ClassFlow>(model);
                session.SaveChanges();
            }
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
