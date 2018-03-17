using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace InvokeWebServiceDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            #region 常规调用方式
            /*
            WebReference.MyWebService myWebService = new WebReference.MyWebService();

            //Type webServiceObj = typeof(WebReference.MyWebService);
            //foreach (var method in webServiceObj.GetMethods())
            //{
            //    Console.WriteLine("Method:"+ method.Name);
            //}

            int a = 5, b = 23;
            Console.WriteLine("InvokeWebServiceDemo");
            Console.WriteLine($"Invoke HelloWorld:{myWebService.HelloWorld()}");
            Console.WriteLine($"Invoke Add:{a} + {b} = {myWebService.Add(a, b)}");
            Console.WriteLine($"Invoke Sum:{a} + {b} = {myWebService.Sum(a, b)}");
            Console.ReadKey();
            */
            #endregion


            #region Http方式调用
            //Program program = new Program();
            //string webServiceUrl = "http://localhost/MyWebServices/MyWebService.asmx";
            //string method = "Sum";
            //string num1 = "5";
            //string num2 = "11";

            //Console.WriteLine(program.HttpPostInvokeWebService(webServiceUrl, method,num1,num2));
            //Console.ReadKey();
            #endregion

            string webServiceUrl = "http://localhost/MyWebServices/MyWebService.asmx?wsdl";
            string methodName = "Add";// "HelloWorld";//Add
            WebServiceProxy webServiceProxy = new WebServiceProxy(webServiceUrl);
            string[] param = { "5","23"};

            string result=webServiceProxy.ExecuteQuery(methodName, param).ToString();
            Console.WriteLine($"{methodName}:{result}");
            Console.ReadKey();

        }

        public string HttpPostInvokeWebService(string url, string method, string num1, string num2)
        {
            try
            {
                string resultMsg = string.Empty;
                string param = string.Empty;
                byte[] bytes = null;

                Stream writerStream = null;
                HttpWebRequest httpWebRequest = null;
                HttpWebResponse httpWebResponse = null;

                param= HttpUtility.UrlEncode("a")+ "=" + HttpUtility.UrlEncode(num1) + "&" + HttpUtility.UrlEncode("b") + "=" + HttpUtility.UrlEncode(num2);
                bytes = Encoding.UTF8.GetBytes(param);


                httpWebRequest = (HttpWebRequest)WebRequest.Create(url+"/" + method);
                httpWebRequest.Method = "POST";
                httpWebRequest.ContentType = "application/x-www-form-urlencoded";
                httpWebRequest.ContentLength = bytes.Length;

                try
                {
                    writerStream = httpWebRequest.GetRequestStream();
                }
                catch (Exception)
                {
                    return "";
                }

                writerStream.Write(bytes,0,bytes.Length);
                writerStream.Close();

                try
                {
                    httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();  //获得响应
                }
                catch (WebException ex)
                {
                    return "";
                }

                #region 这种方式读取到的是一个返回的结果字符串
                //Stream readStream = httpWebResponse.GetResponseStream();
                //XmlTextReader xmlReader = new XmlTextReader(readStream);
                //xmlReader.MoveToContent();
                //resultMsg = xmlReader.ReadInnerXml();
                #endregion

                #region 这种方式读取到的是一个Xml格式的字符串
                StreamReader readStream = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8);
                resultMsg = readStream.ReadToEnd();
                #endregion

                httpWebResponse.Dispose();
                httpWebResponse.Close();

                //xmlReader.Dispose();
                //xmlReader.Close();

                readStream.Dispose();
                readStream.Close();

                return resultMsg;
            }
            catch (Exception)
            {
                return "Error";
                //throw;
            }
        }
    }
}
