using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;

namespace WebServicesDemo
{
    /// <summary>
    /// MyWebService 的摘要说明
    /// </summary>
    //[WebService(Namespace = "http://tempuri.org/")]
    [WebService(Namespace = "http://www.ningxinyaju.com/")]//使用自定义的命名空间
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // 若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消注释以下行。 
    // [System.Web.Script.Services.ScriptService]
    public class MyWebService : System.Web.Services.WebService
    {
        //如果方法需要通过webservice的地址进行调用，那就必须在方法上面打上 [WebMethod] 的特性标签，否则是无法通过webservice访问到的。
        //Description 是方法的描述。
        [WebMethod(Description = "Hello World")]
        public string HelloWorld()
        {
            return "Hello World";
        }

        [WebMethod(Description ="求和运算")]
        public int Add(string a, string b)
        {
            return int.Parse(a) + int.Parse(b);
        }

        [WebMethod(Description ="求积运算")]
        public int Sum(string a, string b)
        {
            return int.Parse(a) * int.Parse(b);
        }
    }
}
