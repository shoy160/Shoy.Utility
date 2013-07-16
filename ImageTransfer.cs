using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Xml;
using System;

namespace Hange.Utility
{
    internal class ImageTransferConfig
    {
        
    }

    public static class ImageTransfer
    {
        private class TransferParameters
        {
            public string fileName { get; set; }

            public string destFilePath { get; set; }

            public string destFileName { get; set; }

            public bool deleteSourceFile { get; set; }
        }

        private static readonly string UserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 5.2; Trident/4.0; .NET CLR 1.1.4322; .NET CLR 3.0.4506.2152; .NET CLR 3.5.30729; .NET4.0E; .NET4.0C; .NET CLR 2.0.50727)";

        private static readonly string Boundary = "--------------f4ee5e0eb004440f9c16118a76b6b299";

        private static readonly Encoding Encoding = Encoding.UTF8;

        private static string _key = null;

        private static string Key
        {
            get
            {
                if (string.IsNullOrEmpty(_key))
                {
                    try
                    {
                        XmlDocument cfg = new XmlDocument();

                        cfg.Load(AppDomain.CurrentDomain.BaseDirectory + "\\App_Data\\imgtransfer.config");

                        _key = cfg.SelectSingleNode("/Configs/Key").Attributes["Value"].Value;
                    }
                    catch { _key = "error"; }
                }

                return _key;
            }
        }

        /// <summary>
        /// 转移，后台执行。
        /// </summary>
        /// <param name="fileName">要转移的文件完整路径</param>
        /// <param name="destFilePath">目标网站保存的URL路径，相对网站根目录</param>
        /// <param name="destFileName">目标网站保存的文件名称</param>
        /// <param name="deleteSourceFile">上传成功之后是否删除源文件</param>
        public static void Transfer(string fileName, string destFilePath, string destFileName, bool deleteSourceFile)
        {
            // 开启线程上传，后期可限制线程数量

            Thread transer = new Thread(TransferRoutine);

            transer.Start(new TransferParameters()
            {
                fileName = fileName,
                destFilePath = destFilePath,
                destFileName = destFileName,
                deleteSourceFile = deleteSourceFile,
            });
        }

        private static void TransferRoutine(object param)
        {
            TransferParameters Parameters = (TransferParameters)param;

            EventWaitHandle wh = new AutoResetEvent(false);

            if (File.Exists(Parameters.fileName))
            {
                int counter = 1, maxcount = 5;

                bool success = false;

                while (counter <= maxcount)
                {
                    HttpWebRequest Request = GetRequest();

                    FileStream fs = new FileStream(Parameters.fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

                    Dictionary<string, string> PostParams = new Dictionary<string, string>();

                    PostParams.Add("save_path", Parameters.destFilePath);
                    PostParams.Add("file_name", Parameters.destFileName);

                    byte[]
                        PostTextParams = GetPostTextStream(PostParams),
                        PostFileHead = GetPostFileHead(Parameters.destFileName, Parameters.fileName, "image/pjpeg"),
                        PostStreamEnd = GetPostStreamEnd(),
                        PostFileData = new byte[fs.Length];

                    fs.Read(PostFileData, 0, (int)fs.Length);

                    fs.Close();

                    Request.ContentLength = PostTextParams.Length + PostFileHead.Length + PostFileData.Length + PostStreamEnd.Length;

                    Request.GetRequestStream().Write(PostTextParams, 0, PostTextParams.Length);

                    Request.GetRequestStream().Write(PostFileHead, 0, PostFileHead.Length);

                    Request.GetRequestStream().Write(PostFileData, 0, PostFileData.Length);

                    Request.GetRequestStream().Write(PostStreamEnd, 0, PostStreamEnd.Length);

                    HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();

                    if ((new StreamReader(Response.GetResponseStream())).ReadToEnd().Trim().ToLower() != "success")
                    {
                        if (counter < maxcount)
                           // wh.WaitOne(counter * 60 * 1000); //等待counter分钟

                        counter++;

                        continue;
                    }

                    success = true;

                    try
                    {
                        //File.Delete(Parameters.fileName);
                    }
                    catch { }

                    break; //上传成功
                }

                // 五次都上传失败，记录日志
                if (!success)
                {
                    string LogFiles = AppDomain.CurrentDomain.BaseDirectory + "\\App_Data\\Logs\\ImageTransfer\\";

                    if (!Directory.Exists(LogFiles))
                    {
                        Directory.CreateDirectory(LogFiles);
                    }

                    StreamWriter sr = new StreamWriter(string.Format(LogFiles + "{0}.txt", DateTime.Now.ToString("yyyy-MM-dd")), true, ImageTransfer.Encoding);

                    sr.WriteLine(string.Format("{0} 于 {1} 上传失败", Parameters.fileName, DateTime.Now.ToString("yyyy-MM-dd HH:mm")));

                    sr.Close();
                }
            }

            wh.Set();
        }

        private static HttpWebRequest GetRequest()
        {
            HttpWebRequest Request = (HttpWebRequest)HttpWebRequest.Create("http://www.img.100hg.com/upload.aspx?key=" + ImageTransfer.Key);

            #region //构建请求参数

            Request.Method = "POST";

            Request.ContentType = "multipart/form-data; boundary=" + ImageTransfer.Boundary;

            Request.UserAgent = ImageTransfer.UserAgent;

            #endregion

            return Request;
        }

        private static byte[] GetPostTextStream(Dictionary<string,string> Params)
        {
            StringBuilder sbPostText = new StringBuilder(Params.Count);

            foreach (string param in Params.Keys)
            {
                sbPostText.Append("\r\n");

                sbPostText.AppendFormat("--{0}", ImageTransfer.Boundary);

                sbPostText.Append("\r\n");

                sbPostText.AppendFormat("Content-Disposition: form-data; name=\"{0}\"", param);

                sbPostText.Append("\r\n\r\n");

                sbPostText.Append(Params[param]);
            }

            return ImageTransfer.Encoding.GetBytes(sbPostText.ToString());
        }


        private static byte[] GetPostFileHead(string name, string fileName, string mimeType)
        {
            StringBuilder sbHead = new StringBuilder();

            sbHead.Append("\r\n");

            sbHead.AppendFormat("--{0}", ImageTransfer.Boundary);

            sbHead.Append("\r\n");

            sbHead.AppendFormat("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"", name, fileName);

            sbHead.Append("\r\n");

            sbHead.AppendFormat("Content-Type: {0}", mimeType);

            sbHead.Append("\r\n\r\n");

            return ImageTransfer.Encoding.GetBytes(sbHead.ToString());
        }

        private static byte[] GetPostStreamEnd()
        {
            return ImageTransfer.Encoding.GetBytes(string.Format("\r\n--{0}--\r\n", ImageTransfer.Boundary));
        }
    }
}
