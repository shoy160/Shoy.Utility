using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.IO.Compression;

namespace Hange.Utility
{
    /// <summary>
    /// 代理ip类
    /// </summary>
    public static class ProxyCls
    {
        private const string ProxyUrl = "http://www.sooip.cn/guoneidaili/";//国内代理列1

        private const string ProxyUrl1 = "http://www.dn28.com/html/41/category-catid-141.html";//国内代理列2

        //private const string ProxyUrl2 = "http://www.adminym.com/ip/list_1_226.html";//国内代理列3

        private const int PageLimit = 200;
        private const int GetSize = 2;

        public class ProxyInfo
        {
            public string Ip { get; set; }
            public int Port { get; set; }
            public string Description { get; set; }
            public int UseTime { get; set; }
        }

        /// <summary>
        /// 获取代理，写入文件
        /// </summary>
        public static void WriteProxyInfo(string baseTxt,IEnumerable<string> txtPaths)
        {
            var list = CheckLink(baseTxt);
            if (list.Count() > 5)
            {
                var info = list.Select(t => t.Ip + "," + t.Port + "," + t.Description + "," + t.UseTime).ToList();
                foreach (var txtPath in txtPaths)
                {
                    try
                    {
                        Utils.WriteFile(txtPath, info, false);
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// 获取最新的代理Ip
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<ProxyInfo> GetNewProxyList(string baseTxt)
        {
            var list = new List<ProxyInfo>();
            if (File.Exists(baseTxt))
                list = GetProxysFromTxt(baseTxt).ToList();

            #region sooip.cn --网站常挂~

            if (list.Count() < PageLimit)
            {
                var proHtml = HtmlCls.GetHtmlByUrl(ProxyUrl);

                if (!string.IsNullOrEmpty(proHtml))
                {
                    var box = HtmlCls.GetHtmlByCss(proHtml, "box").FirstOrDefault();
                    var proUrl = Utils.GetRegHtmls(box, "<a[^>]*href=['\"]([^'\"]*)['\"][^>]*>").ToList();
                    for (int i = 0; i < GetSize; i++)
                    {
                        if (!string.IsNullOrEmpty(proUrl[i]))
                        {
                            string url;
                            if (proUrl[i].StartsWith("/"))
                                url = "http://www.sooip.cn" + proUrl[i];
                            else
                                url = proUrl[i];
                            var itemHtml = HtmlCls.GetHtmlByUrl(url);
                            if (!string.IsNullOrEmpty(itemHtml))
                            {
                                var ipBox = HtmlCls.GetHtmlById(itemHtml, "text");
                                var reg =
                                    new Regex(
                                        "(?<ip>\\d{1,3}.\\d{1,3}.\\d{1,3}.\\d{1,3})\\s(?<port>\\d{2,5})\\sHTTP\\s(?<desc>.*?)\\d{2}-\\d{2}",
                                        RegexOptions.IgnoreCase | RegexOptions.Singleline);
                                var ms = reg.Matches(ipBox);
                                list.AddRange(from Match match in ms
                                              select new ProxyInfo
                                                         {
                                                             Ip = match.Groups["ip"].Value,
                                                             Port = Utils.StrToInt(match.Groups["port"].Value, 80),
                                                             Description = match.Groups["desc"].Value
                                                         });
                            }
                        }
                    }
                }
            }

            #endregion


            #region dn28.com 备用方案

            if (list.Count() < PageLimit)
            {
                string dnHtml = HtmlCls.GetHtmlByUrl(ProxyUrl1);
                if (!string.IsNullOrEmpty(dnHtml))
                {
                    var dnBox = HtmlCls.GetHtmlByCss(dnHtml, "block").FirstOrDefault();
                    var dnUrl = Utils.GetRegHtmls(dnBox, "<a[^>]*href=['\"]([^'\"]*)['\"][^>]*>").ToList();
                    for (int i = 0; i < GetSize; i++)
                    {
                        if (!string.IsNullOrEmpty(dnUrl[i]))
                        {
                            string url;
                            if (dnUrl[i].StartsWith("/"))
                                url = "http://www.dn28.com" + dnUrl[i];
                            else
                                url = dnUrl[i];
                            var itemHtml = HtmlCls.GetHtmlByUrl(url);
                            if (!string.IsNullOrEmpty(itemHtml))
                            {
                                var ipBox = HtmlCls.GetHtmlById(itemHtml, "articlebody");
                                var reg =
                                    new Regex(
                                        "(?<ip>\\d{1,3}.\\d{1,3}.\\d{1,3}.\\d{1,3}):(?<port>\\d{2,5})@HTTP;(?<desc>[^<]*?)<",
                                        RegexOptions.IgnoreCase | RegexOptions.Singleline);
                                var ms = reg.Matches(ipBox);
                                list.AddRange(from Match match in ms
                                              select new ProxyInfo
                                                         {
                                                             Ip = match.Groups["ip"].Value,
                                                             Port = Utils.StrToInt(match.Groups["port"].Value, 80),
                                                             Description = match.Groups["desc"].Value
                                                         });
                            }
                        }
                    }
                }
            }

            #endregion
                       
            
            #region adminym.com 备用方案2

            //if (list.Count() < PageLimit)
            //{
            //    string adminymHtml = HtmlCls.GetHtmlByUrl(ProxyUrl2);
            //    if (!string.IsNullOrEmpty(adminymHtml))
            //    {
            //        var adurlList = HtmlCls.GetHtmlByCss(adminymHtml, "list_title");
            //        var adUrl =
            //            adurlList.Select(adItem => Utils.GetRegStr(adItem, "<a[^>]*href=['\"]([^'\"]*)['\"][^>]*>")).
            //                ToList();
            //        for (int i = 0; i < GetSize; i++)
            //        {
            //            if (!string.IsNullOrEmpty(adUrl[i]))
            //            {
            //                string url;
            //                if (adUrl[i].StartsWith("/"))
            //                    url = "http://www.adminym.com" + adUrl[i];
            //                else
            //                    url = adUrl[i];
            //                var itemHtml = HtmlCls.GetHtmlByUrl(url);
            //                if (!string.IsNullOrEmpty(itemHtml))
            //                {
            //                    var ipBox = HtmlCls.GetHtmlById(itemHtml, "mainNewsContent");
            //                    var reg =
            //                        new Regex(
            //                            "(?<ip>\\d{1,3}.\\d{1,3}.\\d{1,3}.\\d{1,3}):(?<port>\\d{2,5})@HTTP;(?<desc>[^<]*?)<",
            //                            RegexOptions.IgnoreCase | RegexOptions.Singleline);
            //                    var ms = reg.Matches(ipBox);
            //                    list.AddRange(from Match match in ms
            //                                  select new ProxyInfo
            //                                             {
            //                                                 Ip = match.Groups["ip"].Value,
            //                                                 Port = Utils.StrToInt(match.Groups["port"].Value, 80),
            //                                                 Description = match.Groups["desc"].Value
            //                                             });
            //                }
            //            }
            //        }
            //    }
            //}

            #endregion



            list = list.Take(PageLimit).Distinct().ToList();

            return list;
        }

        public static IEnumerable<ProxyInfo> GetProxysFromTxt(string txtPath)
        {
            //读取原有的代理ip，再次验证
            var txtList = Utils.GetTxtValue(txtPath);
            var list =
                txtList.Select(proxy => proxy.Split(',')).Select(
                    item => new ProxyInfo {Ip = item[0], Port = Utils.StrToInt(item[1], 80), Description = item[2]}).
                    ToList();
            return list;
        }

        /// <summary>
        /// 排除无效的代理ip
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static IEnumerable<ProxyInfo> CheckLink(IEnumerable<ProxyInfo> list)
        {
            const string url = "http://www.360buy.com/product/615863.html"; //"http://www.360buy.com";

            var newList = new List<ProxyInfo>();
            foreach (var proxyInfo in list)
            {
                ProxyInfo info = proxyInfo;
                var newItem = newList.FirstOrDefault(t => t.Ip == info.Ip);
                if (newItem == null)
                {
                    proxyInfo.UseTime = GetHtmlByProxy(url, proxyInfo);

                    if (proxyInfo.UseTime <= 700)
                    {
                        newList.Add(proxyInfo);
                    }
                }
            }
            return newList.OrderBy(t => t.UseTime).Take(50);
        }

        public static IEnumerable<ProxyInfo> CheckLink(string baseTxt)
        {
            var list = GetNewProxyList(baseTxt);
            return CheckLink(list);
        }

        /// <summary>
        /// 代理ip测试方法
        /// </summary>
        /// <param name="url">测试网页链接</param>
        /// <param name="info">代理ip信息</param>
        /// <returns>4次访问的均毫秒,最大为20000</returns>
        public static int GetHtmlByProxy(string url,ProxyInfo info)
        {
            const int testCount = 4;
            string str = "";
            if (string.IsNullOrEmpty(url))
                return 20000;
            int count = 0, succ = 0;
            if (!url.StartsWith("http://"))
                url = "http://" + url;
            int mini = 0;//请求时间(毫秒)
            while (count < testCount)
            {
                count++;
                DateTime dt = DateTime.Now;
                HttpWebRequest req = null;
                HttpWebResponse rep = null;
                try
                {
                    req = (HttpWebRequest)WebRequest.Create(url);
                    req.Timeout = 3000;
                    req.Proxy = new WebProxy(info.Ip, info.Port);
                    req.ServicePoint.ConnectionLimit = 30;
                    req.ContentType = "text/html";
                    req.Headers.Add("Accept-language", "zh-cn,zh;q=0.5");
                    req.Headers.Add("Accept-Charset", "GB2312,utf-8;q=0.7,*;q=0.7");
                    req.UserAgent =
                        "Mozilla/5.0 (Windows; U; Windows NT 5.1; zh-CN; rv:1.9.1b4) Gecko/20090423 Firefox/3.5b4";
                    req.Headers.Add("Accept-Encoding", "gzip, deflate");
                    req.Headers.Add("Keep-Alive", "300");
                    rep = (HttpWebResponse)req.GetResponse();
                    Stream resStream = (rep.ContentEncoding == "gzip"
                                            ? new GZipStream(rep.GetResponseStream(), CompressionMode.Decompress)
                                            : rep.GetResponseStream());
                    if (resStream != null)
                    {
                        var sr = new StreamReader(resStream, Encoding.Default);
                        string item = sr.ReadToEnd();
                        if (!string.IsNullOrEmpty(item) && item.IndexOf("360buy.com") > 0)
                        {
                            succ++;
                            mini += (int) (DateTime.Now - dt).TotalMilliseconds;
                        }
                        else
                        {
                            mini += 5000;
                        }
                    }
                    else
                    {
                        mini += 5000;
                    }
                }
                catch
                {
                    mini += 5000;
                }
                finally
                {
                    if (req != null)
                        req.Abort();
                    if (rep != null)
                    {
                        rep.Close();
                    }
                }
            }
            if (succ == 0)
            {
                return 20000;
            }
            //var bl = testCount * 50 / str.Length;//链接比重 成功次数越少，值越大。
            return (int) Math.Ceiling((double) (mini*count)/(testCount*succ));
        }
    }
}
