using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;
using System.Threading;

namespace Hange.Utility
{
    /// <summary>
    /// 爬虫基类
    /// </summary>
    public static class HtmlCls
    {
        public class ProxyIp
        {
            public string Ip { get; set; }
            public int Port { get; set; }
            public int ErrCount { get; set; }
        }

        private static ProxyIp _proxyIp;

        private static List<ProxyIp> _ipList = new List<ProxyIp>();

        /// <summary>
        /// 获取代理列表(更新代理)
        /// </summary>
        public static void GetProxyIps(string baseTxt)
        {
            _ipList = ProxyCls.GetProxysFromTxt(baseTxt).Select(t => new ProxyIp {Ip = t.Ip, Port = t.Port}).ToList();
        }

        /// <summary>
        /// 获取代理
        /// </summary>
        /// <returns></returns>
        private static ProxyIp GetProxy()
        {
            _proxyIp = null;
            if (_ipList.Count() > 0)
            {
                var proxyIndex = (new Random()).Next(_ipList.Count());
                _proxyIp = _ipList[proxyIndex];
            }
            return _proxyIp;
        }

        /// <summary>
        /// 根据url获取html
        /// 使用代理之前必须执行GetProxyIps()
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="cookie">cookie</param>
        /// <param name="encoding">编码</param>
        /// <param name="useProxy">是否用代理</param>
        /// <returns></returns>
        public static string GetHtmlByUrl(string url, string cookie, Encoding encoding, bool useProxy)
        {
            string html = "";

            try
            {
                using (var http = new HttpHelper(url, "Get", encoding, cookie, "", ""))
                {
                    //若已没有代理，则不使用代理
                    if (useProxy)
                    {
                        var proxy = GetProxy();
                        if (proxy != null)
                            http.SetWebProxy(_proxyIp.Ip, _proxyIp.Port);
                    }
                    html = http.GetHtml();
                    if (string.IsNullOrEmpty(html))
                        CheckProxyIpErr();
                    Thread.Sleep(300);
                }
            }
            catch { }
            return html;
        }

        private static void CheckProxyIpErr()
        {
            if (_proxyIp != null)
            {
                _proxyIp.ErrCount++;
                if (_proxyIp.ErrCount >= 40)
                {
                    _ipList.Remove(_proxyIp);
                    Utils.WriteFile(Utils.GetMapPath("/proxyIpErr.log"),
                                    "代理ip:[" + _proxyIp.Ip + "]被移除，错误次数：" + _proxyIp.ErrCount + "还剩" + _ipList.Count() +
                                    "个代理=>" + Utils.GetTimeNow());
                }
            }
        }

        /// <summary>
        /// 根据url获取html
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="cookie">cookie</param>
        /// <param name="encoding">编码</param>
        /// <returns></returns>
        public static string GetHtmlByUrl(string url, string cookie, Encoding encoding)
        {
            return GetHtmlByUrl(url, cookie, encoding, false);
        }

        /// <summary>
        /// 根据url获取html
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="encoding">编码</param>
        /// <returns></returns>
        public static string GetHtmlByUrl(string url, Encoding encoding)
        {
            return GetHtmlByUrl(url, null, encoding, false);
        }

        /// <summary>
        /// 根据url获取html
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="useProxy">是否用代理</param>
        /// <returns></returns>
        public static string GetHtmlByUrl(string url,bool useProxy)
        {
            return GetHtmlByUrl(url, null, Encoding.Default, useProxy);
        }

        /// <summary>
        /// 根据url获取html
        /// </summary>
        /// <param name="url">url</param>
        /// <returns></returns>
        public static string GetHtmlByUrl(string url)
        {
            return GetHtmlByUrl(url, null, Encoding.Default, false);
        }

        /// <summary>
        /// 根据Id获取html内相关id标签下的html
        /// </summary>
        /// <param name="html"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static string GetHtmlById(string html, string id)
        {
            const string pt =
                @"<([0-9a-zA-Z]+)[^>]*\bid=([""']){0}\2[^>]*>(?><\1[^>]*>(?<tag>)|</\1>(?<-tag>)|.)*?(?(tag)(?!))</\1>";
            const string pt1 = @"<([0-9a-zA-Z]+)[^>]*\bid=([""']){0}\2[^>]*/>";
            string reg = String.Format(pt, id);
            if (!Regex.IsMatch(html, reg))
                reg = String.Format(pt1, id);
            return Regex.Match(html, reg, RegexOptions.Singleline | RegexOptions.IgnoreCase).Value;
        }

        /// <summary>
        /// 根据Id获取html内相关css标签下的html
        /// </summary>
        /// <param name="html"></param>
        /// <param name="css"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetHtmlByCss(string html, string css)
        {
            const string pt =
                @"<([0-9a-zA-Z]+)[^>]*\bclass=(['""]?)(?<t>[^""'\s]*\s)*{0}(?<b>\s[^""'\s]*)*\2[^>]*>(?><\1[^>]*>(?<tag>)|</\1>(?<-tag>)|.)*?(?(tag)(?!))</\1>";
            const string pt1 = @"<([0-9a-zA-Z]+)[^>]*\bclass=(['""]?)(?<t>[^""'\s]*\s)*{0}(?<b>\s[^""'\s]*)*\2[^>]*/>";
            string reg = String.Format(pt, css);
            if (!Regex.IsMatch(html, reg))
                reg = String.Format(pt1, css);
            var ms = Regex.Matches(html, reg, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            return (from Match match in ms select match.Value).ToList();
        }

        /// <summary>
        /// 根据Id获取html内相关css标签下的html
        /// </summary>
        /// <param name="html"></param>
        /// <param name="attr"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetHtmlByAttr(string html, string attr)
        {
            const string pt =
                @"<([0-9a-zA-Z]+)[^>]*\b{0}[^>]*>(?><\1[^>]*>(?<tag>)|</\1>(?<-tag>)|.)*?(?(tag)(?!))</\1>";
            const string pt1 = @"<([0-9a-zA-Z]+)[^>]*\b{0}[^>]*/>";
            string reg = String.Format(pt, attr);
            if (!Regex.IsMatch(html, reg))
                reg = String.Format(pt1, attr);
            var ms = Regex.Matches(html, reg, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            return (from Match match in ms select match.Value).ToList();
        }

        /// <summary>
        /// 根据Id获取html内相关css标签下的html
        /// </summary>
        /// <param name="html"></param>
        /// <param name="attr"></param>
        /// <returns></returns>
        public static string GetAttrValue(string html, string attr)
        {
            const string pt =
                @"<([0-9a-zA-Z]+)[^>]*\b{0}=([""'])(?<attr>[^""']*)\2[^>]*/?>";
            string reg = String.Format(pt, attr);
            var ms = Regex.Match(html, reg, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            return ms.Groups["attr"].Value;
        }

        /// <summary>
        /// 根据url地址下载文件(主要是图片文件)
        /// </summary>
        /// <param name="url">文件地址</param>
        /// <param name="filename">保存的文件名(含路径)</param>
        /// <param name="useProxy">是否用代理</param>
        /// <returns></returns>
        public static bool UrlDownLoadToFile(string url, string filename,bool useProxy)
        {
            bool result;

            using (var http = new HttpHelper(url))
            {
                //若已没有代理，则不使用代理
                if (useProxy && _ipList.Count() > 0)
                {
                    var proxy = GetProxy();
                    if (proxy != null)
                        http.SetWebProxy(_proxyIp.Ip, _proxyIp.Port);
                }
                result = http.SaveFile(filename);
                Thread.Sleep(100);
            }
            return result;
        }

        /// <summary>
        /// 根据url地址下载文件(主要是图片文件)
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="filename">保存的文件名(含路径)</param>
        /// <returns></returns>
        public static bool UrlDownLoadToFile(string url, string filename)
        {
            return UrlDownLoadToFile(url, filename, false);
        }
    }
}
