using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace UCI2MD
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load("Config.xml");
            XmlNode docNode = xmlDoc.DocumentElement;
            XmlElement eleUPGPath = docNode.SelectSingleNode("UnityPGPath") as XmlElement;
            XmlElement eleBlogPath = docNode.SelectSingleNode("BlogPath") as XmlElement;
            XmlElement eleQiniuPath = docNode.SelectSingleNode("QiniuPath") as XmlElement;
            XmlElement eleQiniuConfig = docNode.SelectSingleNode("QiniuConfig") as XmlElement;
            XmlElement eleQiniuBucket = docNode.SelectSingleNode("QiniuBucket") as XmlElement;

            Console.WriteLine(Directory.GetCurrentDirectory());

            string unityPGPath = eleUPGPath.InnerText;      // UnityPG的路径
            string blogPath = eleBlogPath.InnerText;        // HexoBlog路径
            string qiniuPath = eleQiniuPath.InnerText;      // 七牛同步目录路径
            string qiniuCfgPath = eleQiniuConfig.InnerText; // 七牛同步配置路径
            string qiniuBucket = eleQiniuBucket.InnerText;  // 七牛Bucket名

            // enum folder name as category
            string[] dirs = Directory.GetDirectories(unityPGPath);
            foreach (string dir in dirs)
            {
                string[] dirs_2 = Directory.GetDirectories(dir);
                foreach(string dir2 in dirs_2)
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(dir2);
                    string ciInfoPath = Path.Combine(dirInfo.FullName, "ci_info.json");
                    string ciCoverPath = Path.Combine(dirInfo.FullName, "ci_cover.png");

                    if (!File.Exists(ciInfoPath))
                        continue;
                    
                    // get all data
                    string infoStr = File.ReadAllText(ciInfoPath);
                    JObject infoJson = JObject.Parse(infoStr);

                    string name = (string)infoJson["name"];
                    string readName = (string)infoJson["readName"];
                    string category = (string)infoJson["type"];
                    string description = (string)infoJson["desc"];
                    string buildStatus = (string)infoJson["status"];
                    string updateInfo = (string)infoJson["update"];
                    string urlWeb = (string)infoJson["urlWeb"];
                    string urlWin = (string)infoJson["urlWin"];
                    string urlAndroid = (string)infoJson["urlAndroid"];
                    string urlIOS = (string)infoJson["urlIOS"];

                    // =============================
                    // generate a md file in blogpath
                    // =============================
                    // md front-matter
                    bool webAvailable = !String.IsNullOrEmpty(urlWeb);
                    bool winAvailable = !String.IsNullOrEmpty(urlWin);
                    bool androidAvailable = !String.IsNullOrEmpty(urlAndroid);
                    bool iosAvailable = !String.IsNullOrEmpty(urlIOS);

                    string srcWebPlayerPath = Path.Combine(dirInfo.FullName, "Bin/Web/Web.unity3d");
                    string srcAndroidPath = Path.Combine(dirInfo.FullName, "Bin/Android/" + dirInfo.Name + ".apk");

                    webAvailable = File.Exists(srcWebPlayerPath);
                    androidAvailable = File.Exists(srcAndroidPath);
                    iosAvailable = false;

                    StringBuilder builder = new StringBuilder();
                    builder.AppendLine("title: " + readName);
                    builder.AppendLine("date: 2014-10-01 12:00:00");
                    builder.AppendLine("tags: _unityci");
                    builder.AppendLine("categories: " + category);
                    builder.AppendLine("coverImg: http://"+qiniuBucket+".qiniudn.com/ucicontent/" + name + ".png");
                    builder.AppendLine("---");


                    builder.AppendLine("##简介##");
                    builder.AppendLine(description + "\n");
                        
                    builder.AppendLine("##状态##");
                    builder.AppendLine(buildStatus + "\n");

                    builder.AppendLine("##更新信息##");
                    builder.AppendLine(updateInfo + "\n");

                    if (webAvailable)
                    {
                        builder.AppendLine("##Web版##");
                        builder.AppendFormat(@"
<object id='UnityObject'
      classid='clsid:444785F1-DE89-4295-863A-D46C3A781394'
      width='550'
      height='400'
      codebase='http://webplayer.unity3d.com/download_webplayer/UnityWebPlayer.cab#version=2,0,0,0'>
 <param name='http://qiniuBucket.qiniudn.com/ucicontent/{0}.unity3d'
        value='http://{1}.qiniudn.com/ucicontent/{0}.unity3d' />
     <embed id='UnityEmbed'
            src='http://{1}.qiniudn.com/ucicontent/{0}.unity3d'
            width='550'
            height='400'
            type='application/vnd.unity'
            pluginspage='http://www.unity3d.com/unity-web-player-2.x' />
</object>

", name, qiniuBucket);
                    };

                    if (androidAvailable)
                    {
                        builder.AppendLine("##Android版##");
                        builder.AppendFormat("[{0}.apk](http://{1}.qiniudn.com/{0}.apk) \n", name, qiniuBucket);
                    }

                    if (iosAvailable)
                    {
                        builder.AppendLine("##IOS版##");
                        builder.AppendFormat("[{0}.apk](http://{1}.qiniudn.com/{0}.ipa) \n", name, qiniuBucket);
                    }

                    File.WriteAllText(Path.Combine(blogPath, name + ".md"), builder.ToString());

                    // =========================
                    // Cover Images
                    // =========================
                    string dstCoverPath = Path.Combine(qiniuPath, name + ".png");
                    if (File.Exists(ciCoverPath))
                    { 
                        File.Copy(ciCoverPath, dstCoverPath, true);
                    }
                    else
                    {
                        File.Copy("UnityCI.png", dstCoverPath, true);
                    }

                    // ==========================
                    // todo copy all web player
                    // ==========================
                    string dstWebPlayerPath = Path.Combine(qiniuPath, name + ".unity3d");
                    if (File.Exists(srcWebPlayerPath))
                    {
                        File.Copy(srcWebPlayerPath, dstWebPlayerPath, true);
                    }
                    
                    // ==========================
                    // todo copy all android
                    // ==========================
                    string dstAndroidPath = Path.Combine(qiniuPath, name + ".apk");
                    if (File.Exists(srcAndroidPath))
                    {
                        File.Copy(srcAndroidPath, dstAndroidPath, true);
                    }
                }
            }

            // ==========================
            // do sync
            // ==========================
            Process syncProc = new Process();
            syncProc.StartInfo.FileName = "qrsync";
            syncProc.StartInfo.Arguments = qiniuCfgPath;
            syncProc.Start();

            syncProc.WaitForExit();
        }
    }
}
