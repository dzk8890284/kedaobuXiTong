using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FanyiNetwork.Models
{
    public class CheifStatsVM
    {
        public int userid { get; set; }
        public string name { get; set; }
        public string group { get; set; }
        public int totalAssign { get; set; }
        public int assignoverdue { get; set; }
        public double assignoverduePercentage { get; set; }
        public int reviewoverdue { get; set; }
        public double reviewoverduePercentage { get; set; }
    }

    public class TeacherStatsVM
    {
        public int userid { get; set; }
        public string name { get; set; }
        public string group { get; set; }
        public int totalOrder { get; set; }
        public int totalStudyOrder { get; set; }
    }
}
