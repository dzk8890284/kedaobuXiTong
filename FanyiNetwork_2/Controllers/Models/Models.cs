using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FanyiNetwork.Models
{
    public class BaseModel
    {
        public int id { get; set; }
    }

    public class User : BaseModel
    {
        public string account { get; set; }
        public string password { get; set; }
        public string name { get; set; }
        public string qq { get; set; }
        public string mobile { get; set; }
        public string info { get; set; }
        public string group { get; set; }
        public bool status { get; set; }
        public string wechat { get; set;}
        public string barcode { get; set; }

        public bool isTerminated { get; set; }
    }
   
    public class LoginHistory : BaseModel
    {
        public int userID { get; set; }
        public string ipAddress { get; set; }
        public DateTime addTime { get; set; }
    }

    public class Client : BaseModel {
        public string name { get; set; }
        public string realName { get; set; }
        public string sex { get; set; }
        public DateTime birthday { get; set; }
        public string memo { get; set; }
        public DateTime addTime { get; set; }
        public string age { get; set; }
    }


       //课程订单
     public class  ClassOrder : BaseModel
      {
        public string ordertape { get; set; }//课程订单,回顾资料
        public int classId { get; set; }//课程id
        public int cs { get; set; }//客服id
        public int client { get; set; }//客户id
        public int cheif { get; set; }//主管id
        public int teacher { get; set; }//助教id
        public int price { get; set; }//订单金额
        public string status { get; set; }//订单状态
        public string  currency { get; set; }//货币类型
        public DateTime addTime { get; set; }//建立时间
        public DateTime assignTime{ get; set; }//分配时间 
        public DateTime finishTime { get; set; }//完成时间
        public DateTime closeTime { get; set; }//结课时间
        public DateTime lastUpdate { get; set; }//最后更新时间
        public string memo { get; set; }//备注
            
    }
     //每周课程
    public class  Modules : BaseModel
    {
        
        public int classOrderId { get; set; }//课程订单id
        public int no { get; set; }//每周课程名称
        public int grade{ get; set; }//每周课程评分
    }

    //课程信息
    public class ClassInfo : BaseModel
    {
        public int uId { get; set; } 
        public string name { get; set; }//课程名称
        public string university { get; set; }//大学名称
        public string memo { get; set; }//备注
        public string difficulty { get; set; } //难度系数
        public string professor { get; set; }//教授
        public int orderNum { get; set; } //订单/资料数量

    }
    //每周课程内容
    public class ModulesContent : BaseModel
    {

        public int moduleId { get; set; }//每周课程id
        public int modulesContentId { get; set; }//每周课程内容id
        public string contentType { get; set; }//内容类型
        public int grade{ get; set; }//每周课程内容得分
        public string contents { get; set; }//图片内容
    }

    public class ClassFlow : BaseModel
    {
        public int classorderId { get; set; }
        public string Operator { get; set; }
        public string Operation { get; set; }
        public DateTime Time { get; set; }
    }

    public class ClassInfoFlow : BaseModel
    {
        public int classId { get; set; }
        public string Operator { get; set; }
        public string Operation { get; set; }
        public DateTime Time { get; set; }
    }
}
