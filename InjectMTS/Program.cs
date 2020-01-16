using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Management;
using System.Threading;

namespace InjectMTS
{
    class Program
    {
        static string FindMTSProcessId()
        {
            using (ManagementObjectSearcher search = new ManagementObjectSearcher("select * from Win32_Process"))
            {
                ManagementObjectCollection managementObjectCollection = search.Get();
                if (managementObjectCollection.Count > 0)
                {
                    foreach (ManagementObject mo in managementObjectCollection)
                    {
                        try
                        {
                            var propertyValue = (string) mo.GetPropertyValue("Name");
                            if (propertyValue != null)
                            {
                                if (propertyValue.Contains("java"))
                                {
                                    var cmdLine = (string) mo.GetPropertyValue("CommandLine");
                                    if (cmdLine.Contains("ModTheSpire"))
                                    {
                                        return mo.GetPropertyValue("Handle").ToString();
                                    }
                                }
                            }
                            mo.Dispose();
                        }
                        catch
                        {
                        }
                    }
                }
            }
            return "";
        }

        static void RunCmd(String cmd, Boolean showWindow, Boolean waitForExit)
        {
            //Console.WriteLine("RunCmd " + cmd);
            var p = new Process();
            var si = new ProcessStartInfo();
            var path = Environment.SystemDirectory;
            path        = Path.Combine(path, @"cmd.exe");
            si.FileName = path;
            if (!cmd.StartsWith(@"/")) cmd = @"/c " + cmd;
            si.Arguments              = cmd;
            si.UseShellExecute        = false;
            si.CreateNoWindow         = !showWindow;
            si.RedirectStandardOutput = true;
            si.RedirectStandardError  = true;
            p.StartInfo               = si;

            p.Start();
            if (waitForExit)
            {
                p.WaitForExit();

                var str = p.StandardOutput.ReadToEnd();
                if (!String.IsNullOrEmpty(str)) Console.WriteLine(str.Trim(new Char[] {'\r', '\n', '\t'}).Trim());
                str = p.StandardError.ReadToEnd();
                if (!String.IsNullOrEmpty(str)) Console.WriteLine(str.Trim(new Char[] {'\r', '\n', '\t'}).Trim());
            }
        }

        static void Main(string[] args)

        {
            string JAVA_HOME = Environment.GetEnvironmentVariable("JAVA_HOME");
            if (string.IsNullOrEmpty(JAVA_HOME))
            {
                Console.WriteLine("请安装JDK1.8，并配置JAVA_HOME环境变量.");
            }
            else
            {
                //            string java = @".\jre\bin\java.exe";
                //            if (!File.Exists(java))
                //            {
                //                Console.WriteLine("请放置于游戏根目录....");
                //                return;
                //            }
                Console.WriteLine("正在查找Mod启动器进程...");
                string findMtsProcessId = "";
                while (string.IsNullOrEmpty(findMtsProcessId))
                {
                    findMtsProcessId = FindMTSProcessId();
                    Thread.Sleep(500);
                }
                if (string.IsNullOrEmpty(findMtsProcessId))
                {
                    Console.WriteLine("未找到Mod启动器进程！");
                }
                else
                {
                    List<string> classpath = new List<string>()
                    {
                        @"byteman\lib\byteman.jar",
                        @"byteman\lib\tools.jar",
                        @"byteman\contrib\jboss-modules-system\byteman-jboss-modules-plugin.jar",
                    };

                    Console.WriteLine("开始注入MTS进程...");
                    RunCmd($@"byteman\bin\bminstall {findMtsProcessId}", false, true);
                    List<string> installCP = new List<string>() {@"byteman\lib\byteman-install.jar"}.Concat(classpath).ToList();
                    List<string> submitCP = new List<string>() {@"byteman\lib\byteman-submit.jar"}.Concat(classpath).ToList();
                    //RunCmd($@"{java} -cp {string.Join(";", installCP.ToArray())} org.jboss.byteman.agent.install.Install {findMtsProcessId}", false, true);
                    string[] btms = Directory.GetFiles(".", "*.btm", SearchOption.TopDirectoryOnly);
                    List<string> btmParams = btms.Select(r => new FileInfo(r)).Select(r => r.Name).ToList();
                    string param = string.Join(" ", btmParams.ToArray());
                    Console.WriteLine("开始加载脚本...");
                    RunCmd($@"byteman\bin\bmsubmit -l {param}", false, true);
                    //RunCmd($@"{java} -cp {string.Join(";", submitCP.ToArray())} org.jboss.byteman.agent.submit.Submit -l {param}", false, true);
                    Console.WriteLine("注入脚本完毕！");
                }
            }
            Console.WriteLine("任意键退出...");
            Console.ReadKey(true);
        }
    }
}