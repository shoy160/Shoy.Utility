using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Linq;
using System.Web;
using Hange.Utility.Extend;

namespace Hange.Utility
{
    /// <summary>
    /// 促销广告处理类
    /// </summary>
    /// <remarks>2012-09-10 .cpp 后缀改为 .html  by shitou.</remarks>
    public class Advertising
    {
        private const string AspxHeader =
            "<%@ Page Language=\"C#\" MasterPageFile=\"~/index_v1_2/hangeShop.master\" AutoEventWireup=\"true\" Inherits=\"myws168.web.myws168Base\" %><asp:Content ID=\"head\" runat=\"server\" ContentPlaceHolderID=\"head\"><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" />{0}</asp:Content><asp:Content ID=\"content\" runat=\"server\" ContentPlaceHolderID=\"ContentBox\">";

        private const string AspxFooter =
            "<script type=\"text/javascript\" src=\"/index_v1_2/js/embed-operatorParty.js\"></script></asp:Content>";

        private static readonly List<string> HtmlExtList = new List<string> {".html", ".htm"};
        private static readonly List<string> ImageExtList = new List<string> {".jpg", ".gif", ".png"};
        private static readonly object Lockobj = new object();

        private static string _imgsDir = @"D:\imgs";
        private static string _aspxDir = @"D:\aspx";
        private static string _cacheDir = @"D:\advcache";
        private static string _rarexe = @"D:\Program Files\WinRAR\WinRAR.exe";

        public static void SetImgsDir(string dir)
        {
            _imgsDir = dir;
        }
        public static void SetAspxDir(string dir)
        {
            _aspxDir = dir;
        }
        public static void SetCacheDir(string dir)
        {
            _cacheDir = dir;
        }
        public static void SetRarExtPath(string path)
        {
            _rarexe = path;
        }

        /// <summary>
        /// rar.exe文件路径
        /// </summary>
        private static bool DeComplexFile(string rarFilePath, string cacheDir,out string errMsg)
        {
            errMsg = "";
            bool result = false;
            try
            {
                if (!Directory.Exists(cacheDir))
                {
                    Directory.CreateDirectory(cacheDir);
                }

                //rar x -t -o-p xj.rar .\xj
                string cmd = String.Format("x -t -o-p \"{0}\" \"{1}\"", rarFilePath, cacheDir);
                var p = new Process();
                var info = new ProcessStartInfo
                               {
                                   FileName = _rarexe,
                                   Arguments = cmd,
                                   WindowStyle = ProcessWindowStyle.Hidden
                               };
                p.StartInfo = info;
                p.Start();
                p.WaitForExit();
                if (p.HasExited)
                {
                    result = true;
                }
                else
                {
                    errMsg = "DeComplexFile->关联进程未终止";
                }
            }
            catch(Exception ex)
            {
                errMsg = "DeComplexFile->" + ex.Message;
            }
            return result;
        }

        ///<summary>
        ///</summary>
        ///<param name="file"></param>
        ///<param name="url"></param>
        ///<returns></returns>
        public static string MakeAdvertis(HttpPostedFile file, string url)
        {
            var extList = new[] {".rar", ".zip"};
            if (file == null)
            {
                return "请先上传文件！";
            }
            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(ext) || !extList.Contains(ext))
            {
                return "上传文件格式不正确！";
            }
            if (!string.IsNullOrEmpty(url) && !Regex.IsMatch(url, "^ext_\\w+.html$"))
            {
                return "输入的广告链接不正确！";
            }
            var path = _cacheDir + @"\" + Path.GetFileName(file.FileName);
            file.SaveAs(path);
            var advUrl = ProcessPromotion(path, url);
            File.Delete(path);
            return advUrl;
        }

        /// <summary>
        /// 处理单个广告压缩文件,强制文件结构如下
        /// ssss\
        /// ssss\*.html|htm
        /// ssss\images\*.jpg|gif|png
        /// </summary>
        /// <param name="rarFilePath">文件名称应该由web程序自动处理成时间字符串</param>
        /// <param name="targetWebUrl">替换目标url,主要用于golist.html</param>
        /// <returns>url地址  或者 错误信息</returns>
        public static string ProcessPromotion(string rarFilePath, string targetWebUrl)
        {
            /**基本思路
             * 1.由webApplication上传rar文件
             * 2.解压文件到缓存目录
             * 3.拷贝图片到www.img.100hg.com ext目录下
             * 4.修改htm文件为aspx文件,并调整内部代码
             * 5.拷贝aspx文件到myws168站点根目录下
             */

            #region safeCheck

            if (string.IsNullOrEmpty(rarFilePath))
            {
                return "参数错误!";
            }

            #endregion
            try
            {
                string url = "操作失败";
                var urlReg = Regex.Match(targetWebUrl, @"^ext_(\w+)\.html$");
                string aspxFileName;

                if (urlReg.Success)
                    aspxFileName = urlReg.Groups[1].Value;
                else
                    aspxFileName = "bhg" + DateTime.Now.ToString("yyyyMMddHHmmss");

                lock (Lockobj)
                {
                    string rarFileName = GetPromotionRarFileName(rarFilePath);
                    string deComplexFolder = _cacheDir + "\\" + rarFileName;
                    string sourceImageFileDir = deComplexFolder + "\\images";

                    if (Directory.Exists(deComplexFolder) && deComplexFolder != "\\" &&
                        deComplexFolder.IndexOf("cache") >= 0)
                        //安全保护,防止去删除根目录,揪心
                    {
                        Directory.Delete(deComplexFolder, true); //防止意外文件被拷贝,删除之
                    }
                    string comMsg = "";
                    if (DeComplexFile(rarFilePath, deComplexFolder, out comMsg))
                    {
                        //成功解压
                        try
                        {
                            //拷贝图片文件
                            string targetImageFileDir = _imgsDir + "\\" + aspxFileName;

                            string imgFileName = aspxFileName;
                            //判断是否存在图片文件
                            if (Directory.Exists(targetImageFileDir))
                            {
                                var time = Utils.GetTimeNow("yyyyMMddHHmmss");
                                if (targetImageFileDir.StartsWith("bhg"))
                                    imgFileName = "bhg" + time;
                                else
                                    imgFileName += time;
                                targetImageFileDir = _imgsDir + "\\" + imgFileName;
                            }
                            if (MovePromotionImg(sourceImageFileDir, targetImageFileDir))
                            {
                                //成功拷贝图片

                                //处理html为aspx,并拷贝至指定位置
                                string htmlFilePath = GetHtmlPromotionFilePath(deComplexFolder);
                                if (htmlFilePath.IsNotNullOrEmpty())
                                {
                                    url = ConvertHtmlToAspx(htmlFilePath, _aspxDir, imgFileName, aspxFileName);
                                }
                                else
                                {
                                    return "未找到html文件";
                                }
                            }
                            else
                            {
                                return "图片转移失败！";
                            }
                        }
                        catch (Exception ex)
                        {
                            return "MovePromotionImg->" + ex.Message;
                        }
                    }
                    else
                    {
                        return comMsg;
                    }
                    if (Directory.Exists(deComplexFolder) && deComplexFolder != "\\" &&
                        deComplexFolder.IndexOf("cache") >= 0)
                    {
                        Directory.Delete(deComplexFolder, true); //删除掉临时文件
                    }
                }
                return url;
            }
            catch(Exception ex)
            {
                return "操作异常:" + ex.Message;
            }
        }


        ///// <param name="targetWebUrl">预设目标连接,默认string.Empty</param>
        /// <summary>
        /// 将指定html或者htm文件装换成ext目录下面的aspx文件,并移动到目标目录
        /// </summary>
        /// <param name="htmlFilePath">html文件路径</param>
        /// <param name="moveToDir">目标目录</param>
        /// <param name="imgFileName">图片文件名</param>
        /// <param name="aspxFileName">aspx文件名</param>
        /// <returns>前端可以使用的url,如golist.html, 错误返回string.Empty;</returns>
        private static string ConvertHtmlToAspx(string htmlFilePath, string moveToDir, string imgFileName, string aspxFileName)
        {
            string url = string.Empty;
            try
            {
                if (!Directory.Exists(moveToDir))
                    Directory.CreateDirectory(moveToDir);
                var html = Utils.GetHtmlFromFile(htmlFilePath, Encoding.Default);
                html = Utils.ClearTrn(html);
                var data = Utils.GetRegStr(html, @"<body[^>]*>([\w\W]*)</body>", 1);
                if (data.IsNotNullOrEmpty())
                {
                    var head = Utils.GetRegStr(html, @"<head[^>]*>([\w\W]*)</head>", 1);
                    data = data.Replace("src=\"images/", "src=\"/images/s-Master/placeHolder.gif\" hsrc=\"http://www.img.100hg.com/ext/" + imgFileName + "/");
                    var styleStr = "";
                    if (!string.IsNullOrEmpty(head))
                    {
                        var links = Utils.GetRegHtmls(head, "<link[^>]*>", 0);
                        styleStr = links.Aggregate("", (current, link) => current + link);
                        var styles = Utils.GetRegHtmls(head, "<style[^>]*>[\\s\\S]*</style>", 0);
                        foreach (var style in styles)
                        {
                            data = data.Replace(style, "");
                            styleStr += style;
                        }
                        styleStr = styleStr.Replace("url(images/", "url(http://www.img.100hg.com/ext/" + imgFileName + "/");
                    }
                    var header = AspxHeader.FormatWith(styleStr);
                    data = header + data + AspxFooter;
                    string targetFile = moveToDir + "\\" + aspxFileName + ".aspx";
                    url = "ext_" + aspxFileName + ".html";
                    Utils.WriteFile(targetFile, data, false, Encoding.UTF8);
                }
                else
                {
                    return "未找到body标签";
                }
            }
            catch (Exception ex)
            {
                url = "ConvertHtmlToAspx->" + ex.Message;
            }
            return url;
        }


        /// <summary>
        /// 扫描指定目录,获取唯一一个html或者htm格式文件
        /// </summary>
        /// <param name="deComplexFolder"></param>
        /// <returns></returns>
        private static string GetHtmlPromotionFilePath(string deComplexFolder)
        {
            string result = string.Empty;
            try
            {
                string[] files = Directory.GetFiles(deComplexFolder);
                foreach (string filename in files)
                {
                    var ext = Path.GetExtension(filename);
                    if (!string.IsNullOrEmpty(ext) && HtmlExtList.Contains(ext.ToLower()))
                    {
                        result = filename;
                        break;
                        //切记,里面只能有一个html文件
                    }
                }
            }
            catch { }
            return result;
        }


        /// <summary>
        /// 把解压出来的图片文件移动到目标目录下面
        /// </summary>
        /// <param name="sourceImageFileDir">临时图片文件</param>
        /// <param name="targetImageFileDir">目标图片文件</param>
        /// <returns></returns>
        private static bool MovePromotionImg(string sourceImageFileDir, string targetImageFileDir)
        {
            bool result = true;
            try
            {
                if (!Directory.Exists(targetImageFileDir))
                {
                    Directory.CreateDirectory(targetImageFileDir);
                }
                string[] files = Directory.GetFiles(sourceImageFileDir);
                foreach (string filename in files)
                {
                    string ext = Path.GetExtension(filename);//filename.Substring(filename.LastIndexOf('.'));
                    if (!string.IsNullOrEmpty(ext) && ImageExtList.Contains(ext.ToLower()))
                    {
                        //开始移动文件
                        string targetFileName = filename.Substring(filename.LastIndexOf('\\') + 1);
                        targetFileName = targetImageFileDir + "\\" + targetFileName;
                        File.Copy(filename, targetFileName, true);
                    }
                }
            }
            catch (Exception ex)
            {
                result = false;
            }
            return result;
        }

        /// <summary>
        /// 返回rar文件名,不包含扩展名!!!
        /// </summary>
        /// <param name="rarFilePath">文件全路径</param>
        /// <returns></returns>
        private static string GetPromotionRarFileName(string rarFilePath)
        {
            string result = string.Empty;
            try
            {
                result = rarFilePath.Substring(rarFilePath.LastIndexOf('\\') + 1);
                result = result.Substring(0, result.LastIndexOf('.'));
            }
            catch { }
            return result;
        }

        /// <summary>
        /// 获取广告链接列表
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAdvUrlList()
        {
            var aspxList = Directory.GetFiles(_aspxDir);
            return
                aspxList.Where(t => t.EndsWith(".aspx")).Select(
                    t => "ext_" + Path.GetFileName(t).Replace(".aspx", ".html")).ToList();
        }

        /// <summary>
        /// 删除广告
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool DelAdvUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;
            try
            {
                var urlReg = Regex.Match(url, "^ext_(\\w+).html$", RegexOptions.IgnoreCase);
                if (!urlReg.Success)
                {
                    return false;
                }
                string aspxPath = _aspxDir + @"\" + urlReg.Groups[1].Value + ".aspx";
                if (File.Exists(aspxPath))
                {
                    var sr = new StreamReader(aspxPath, Encoding.UTF8);
                    var data = sr.ReadToEnd();
                    sr.Close();
                    var imgReg = Regex.Match(data, "http://www.img.100hg.com/ext/([^/]+)/");
                    if (imgReg.Success)
                    {
                        var name = imgReg.Groups[1].Value;
                        if (!string.IsNullOrEmpty(name) && name.Length > 3)
                        {
                            var imgPath = _imgsDir + @"\" + imgReg.Groups[1].Value;
                            if (Directory.Exists(imgPath) && imgPath != "/" && imgPath.IndexOf(_imgsDir) >= 0 &&
                                !imgPath.EndsWith("/"))
                                Directory.Delete(imgPath, true);
                        }
                    }
                    File.Delete(aspxPath);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
