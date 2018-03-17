using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Services.Description;
using System.Web.Services.Discovery;
using System.Web.Services.Protocols;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace InvokeWebServiceDemo
{
    public class WebServiceProxy : System.Web.Services.WebService
    {
        private string _wsdlUrl = string.Empty;
        private string _wsdlName = string.Empty;
        private string _wsdlNamespace = "FrameWork.WebService.DynamicWebServiceCalling.{0}";
        private Type _typeName = null;
        private string _assName = string.Empty;
        private string _assPath = string.Empty;
        private object _instance = null;

        private object Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Activator.CreateInstance(_typeName);
                    return _instance;
                }
                else
                {
                    return _instance;
                }
            }
        }


        public WebServiceProxy(string wsdlUrl)
        {
            this._wsdlUrl = wsdlUrl;
            string wsdlName = WebServiceProxy.getWsclassName(wsdlUrl);
            this._wsdlName = wsdlName;
            this._assName = string.Format(_wsdlNamespace, wsdlName);
            this._assPath = Path.GetTempPath() + this._assName + getMd5Sum(this._wsdlUrl) + ".dll";
            this.CreateServiceAssembly();
        }

        public WebServiceProxy(string wsdlUrl, string wsdlName)
        {
            this._wsdlUrl = wsdlUrl;
            this._wsdlName = wsdlName;
            this._assName = string.Format(_wsdlNamespace,wsdlName);
            this._assPath = Path.GetTempPath() + this._assName + getMd5Sum(this._wsdlUrl) + ".dll";

            this.CreateServiceAssembly();
        }

        private string getMd5Sum(string str)
        {
            Encoder enc = System.Text.Encoding.Unicode.GetEncoder();
            byte[] unicodeText = new byte[str.Length * 2];//一个字符两个字节
            enc.GetBytes(str.ToCharArray(), 0, str.Length, unicodeText, 0, true);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] result = md5.ComputeHash(unicodeText);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
            {
                sb.Append(result[i].ToString("X2"));
            }
            return sb.ToString();
        }

        /// <summary>
        /// 得到代理类类型名称
        /// </summary>
        private void initTypeName()
        {
            Assembly serviceAsm = Assembly.LoadFrom(this._assPath);
            Type[] types = serviceAsm.GetTypes();
            string objTypeName = "";
            foreach (var t in types)
            {
                if (t.BaseType == typeof(SoapHttpClientProtocol))
                {
                    objTypeName = t.Name;
                    break;
                }
            }

            _typeName = serviceAsm.GetType(this._assName + "." + objTypeName);
        }

        /// <summary>
        /// 根据Web service文档架构向代理类添加ServiceDescription和XmlSchema
        /// </summary>
        /// <param name="baseWsdlUrl">Web服务器地址</param>
        /// <param name="importer">代理类</param>
        private void checkForImports(string baseWsdlUrl, ServiceDescriptionImporter importer)
        {
            DiscoveryClientProtocol dcp = new DiscoveryClientProtocol();
            dcp.DiscoverAny(baseWsdlUrl);
            dcp.ResolveAll();

            foreach (object osd in dcp.Documents.Values)
            {
                if (osd is ServiceDescription)
                {
                    importer.AddServiceDescription((ServiceDescription)osd,null,null);
                }

                if (osd is XmlSchema)
                {
                    importer.Schemas.Add((XmlSchema)osd);
                }
            }
        }

        /// <summary>
        /// 复制程序集到指定路径
        /// </summary>
        /// <param name="pathToAssembly"></param>
        private void copyTempAssembly(string pathToAssembly)
        {
            File.Copy(pathToAssembly,this._assPath);
        }

        /// <summary>
        /// 是否已经存在该程序集
        /// </summary>
        /// <returns></returns>
        private bool checkCache()
        {
            if (File.Exists(this._assPath))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static string getWsclassName(string wsdlUrl)
        {
            string[] parts = wsdlUrl.Split('/');
            string[] pps = parts[parts.Length - 1].Split('.');
            return pps[0];
        }

        #region 得到WSDL信息，生成本地代理类并编译为DLL，构造函数调用，类生成时加载
        private void CreateServiceAssembly()
        {
            if (this.checkCache())
            {
                this.initTypeName();
                return;
            }

            if (string.IsNullOrEmpty(this._wsdlUrl))
            {
                return;
            }

            try
            {
                //使用WebClient下载WSDL信息
                WebClient web = new WebClient();
                Stream stream = web.OpenRead(this._wsdlUrl);
                //创建和格式化WSDL文档  
                ServiceDescription description = ServiceDescription.Read(stream);
                //创建客户端代理类
                ServiceDescriptionImporter importer = new ServiceDescriptionImporter();
                importer.ProtocolName = "Soap";
                importer.Style = ServiceDescriptionImportStyle.Client;//生成客户端代理
                importer.CodeGenerationOptions = CodeGenerationOptions.GenerateProperties | CodeGenerationOptions.GenerateNewAsync;
                importer.AddServiceDescription(description,null,null);//添加WSDL文档
                //使用CodeDom编译客户端代理类
                CodeNamespace nmspace = new CodeNamespace(this._assName);
                CodeCompileUnit unit = new CodeCompileUnit();
                unit.Namespaces.Add(nmspace);

                this.checkForImports(this._wsdlUrl,importer);

                ServiceDescriptionImportWarnings warning = importer.Import(nmspace,unit);
                CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
                CompilerParameters parameters = new CompilerParameters();
                parameters.ReferencedAssemblies.Add("System.dll");
                parameters.ReferencedAssemblies.Add("System.XML.dll");
                parameters.ReferencedAssemblies.Add("System.Web.Services.dll");
                parameters.ReferencedAssemblies.Add("System.Data.dll");
                parameters.GenerateExecutable = false;
                parameters.GenerateInMemory = false;
                parameters.IncludeDebugInformation = false;

                CompilerResults result = provider.CompileAssemblyFromDom(parameters,unit);
                provider.Dispose();

                if (result.Errors.HasErrors)
                {
                    string errors = string.Format(@"编译错误：{0}错误！",result.Errors.Count);
                    foreach (CompilerError error in result.Errors)
                    {
                        errors += error.ErrorText;
                    }
                    throw new Exception(errors);
                }

                this.copyTempAssembly(result.PathToAssembly);
                this.initTypeName();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message); ;
            }
        }
        #endregion

        #region 执行Web服务方法
        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public object ExecuteQuery(string methodName, object[] param)
        {
            object rtnObj = null;
            //string[] args = new string[2];
            //List<string> list = new List<string>();
            //List<string> list1 = new List<string>();
            //List<string> list2 = new List<string>();
            //object[] objs = new object[3];
            //string sb = "";

            try
            {
                if (this._typeName==null)
                {
                    //记录Web服务访问类名错误日志代码位置 
                    throw new TypeLoadException("Web服务访问类名【" + this._wsdlName + "】不正确，请检查！");
                }

                //调用方法
                MethodInfo mi = this._typeName.GetMethod(methodName);

                if (mi==null)
                {
                    //记录Web服务方法名错误日志代码位置  
                    throw new TypeLoadException("Web服务访问方法名【" + methodName + "】不正确，请检查！");
                }

                try
                {
                    if (param==null)
                    {
                        rtnObj = (string)mi.Invoke(Instance,null);
                    }
                    else
                    {
                        //for (int i = 0; i < param.Length; i++)
                        //{
                        //    sb += param[i].ToString();
                        //}
                        rtnObj = mi.Invoke(Instance, param);
                    }
                }
                catch (TypeLoadException tle)
                {
                    //记录Web服务方法参数个数错误日志代码位置  
                    throw new TypeLoadException("Web服务访问方法【" + methodName + "】参数个数不正确，请检查！", new TypeLoadException(tle.StackTrace));
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, new Exception(ex.StackTrace));
            }
            return rtnObj;
        }

        /// <summary>                           
        /// 执行代理类指定方法，无返回值                                
        /// </summary>                                  
        /// <param   name="methodName">方法名称</param>                           
        /// <param   name="param">参数</param>                              
        public void ExecuteNoQuery(string methodName, object[] param)
        {
            try
            {
                if (this._typeName == null)
                {
                    //记录Web服务访问类名错误日志代码位置  
                    throw new TypeLoadException("Web服务访问类名【" + this._wsdlName + "】不正确，请检查！");
                }
                //调用方法  
                MethodInfo mi = this._typeName.GetMethod(methodName);
                if (mi == null)
                {
                    //记录Web服务方法名错误日志代码位置  
                    throw new TypeLoadException("Web服务访问方法名【" + methodName + "】不正确，请检查！");
                }
                try
                {
                    if (param == null)
                        mi.Invoke(Instance, null);
                    else
                        mi.Invoke(Instance, param);
                }
                catch (TypeLoadException tle)
                {
                    //记录Web服务方法参数个数错误日志代码位置  
                    throw new TypeLoadException("Web服务访问方法【" + methodName + "】参数个数不正确，请检查！", new TypeLoadException(tle.StackTrace));
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, new Exception(ex.StackTrace));
            }
        }
        #endregion
        //class end
    }
}
