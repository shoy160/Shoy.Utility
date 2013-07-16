using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Hange.Utility
{
    public static class JsonCls
    {
        /// <summary>
        /// 类  名：JSONConvert
        /// 描  述：JSON解析类
        /// </summary>
        public static class JsonConvert
        {
            #region 全局变量

            private static JsonObject _json = new JsonObject();//寄存器
            private const string Semicolon = "@semicolon";//分号转义符
            private const string Comma = "@comma"; //逗号转义符

            public static void SetJson(JsonObject jo)
            {
                _json = jo;
            }

            #endregion

            #region 字符串转义
            /// <summary>
            /// 字符串转义,将双引号内的:和,分别转成Semicolon和Comma
            /// </summary>
            /// <param name="text"></param>
            /// <returns></returns>
            private static string StrEncode(string text)
            {
                MatchCollection matches = Regex.Matches(text, "\"[^\"]+\"");

                return matches.Cast<Match>().Aggregate(text,
                                                       (current, match) =>
                                                       current.Replace(match.Value,
                                                                       match.Value.Replace(":", Semicolon).Replace(",",
                                                                                                                    Comma)));
            }

            /// <summary>
            /// 字符串转义,将Semicolon和Comma分别转成:和,
            /// </summary>
            /// <param name="text"></param>
            /// <returns></returns>
            private static string StrDecode(string text)
            {
                return text.Replace(Semicolon, ":").Replace(Comma, ",");
            }

            #endregion

            #region JSON最小单元解析

            /// <summary>
            /// 最小对象转为JsonObject
            /// </summary>
            /// <param name="text"></param>
            /// <returns></returns>
            private static JsonObject DeserializeSingletonObject(string text)
            {
                var jsonObject = new JsonObject();

                MatchCollection matches = Regex.Matches(text,
                                                        "(\"(?<key>[^\"]+)\":\"(?<value>[^,\"]+)\")|(\"(?<key>[^\"]+)\":(?<value>[^,\"\\}]+))");
                foreach (Match match in matches)
                {
                    string value = match.Groups["value"].Value;
                    jsonObject.Add(match.Groups["key"].Value, _json.ContainsKey(value) ? _json[value] : StrDecode(value));
                }

                return jsonObject;
            }

            /// <summary>
            /// 最小数组转为JsonArray
            /// </summary>
            /// <param name="text"></param>
            /// <returns></returns>
            private static JsonArray DeserializeSingletonArray(string text)
            {
                var jsonArray = new JsonArray();

                MatchCollection matches = Regex.Matches(text, "(\"(?<value>[^,\"]+)\")|(?<value>[^,\\[\\]]+)");
                jsonArray.AddRange(from Match match in matches
                                   select match.Groups["value"].Value
                                       into value
                                       select _json.ContainsKey(value) ? _json[value] : StrDecode(value));

                return jsonArray;
            }

            /// <summary>
            /// 反序列化
            /// </summary>
            /// <param name="text"></param>
            /// <returns></returns>
            private static string Deserialize(string text)
            {
                text = StrEncode(text);//转义;和,

                int count = 0;
                const string pattern = @"(\{[^\[\]\{\}]+\})|(\[[^\[\]\{\}]+\])";

                while (Regex.IsMatch(text, pattern))
                {
                    MatchCollection matches = Regex.Matches(text, pattern);
                    foreach (Match match in matches)
                    {
                        string key = "___key" + count + "___";

                        if (match.Value.Substring(0, 1) == "{")
                            _json.Add(key, DeserializeSingletonObject(match.Value));
                        else
                            _json.Add(key, DeserializeSingletonArray(match.Value));

                        text = text.Replace(match.Value, key);

                        count++;
                    }
                }
                return text;
            }

            #endregion

            #region 公共接口

            /// <summary>
            /// 序列化JSONObject对象
            /// </summary>
            /// <param name="text"></param>
            /// <returns></returns>
            public static JsonObject DeserializeObject(string text)
            {
                return _json[Deserialize(text)] as JsonObject;
            }

            /// <summary>
            /// 序列化JSONArray对象
            /// </summary>
            /// <param name="text"></param>
            /// <returns></returns>
            public static JsonArray DeserializeArray(string text)
            {
                return _json[Deserialize(text)] as JsonArray;
            }

            /// <summary>
            /// 反序列化JSONObject对象
            /// </summary>
            /// <param name="jsonObject"></param>
            /// <returns></returns>
            public static string SerializeObject(JsonObject jsonObject)
            {
                var sb = new StringBuilder();
                sb.Append("{");
                foreach (KeyValuePair<string, object> kvp in jsonObject)
                {
                    if (kvp.Value is JsonObject)
                    {
                        sb.Append(string.Format("\"{0}\":{1},", kvp.Key, SerializeObject((JsonObject)kvp.Value)));
                    }
                    else if (kvp.Value is JsonArray)
                    {
                        sb.Append(string.Format("\"{0}\":{1},", kvp.Key, SerializeArray((JsonArray)kvp.Value)));
                    }
                    else if (kvp.Value is String)
                    {
                        sb.Append(string.Format("\"{0}\":\"{1}\",", kvp.Key, kvp.Value));
                    }
                    else
                    {
                        sb.Append(string.Format("\"{0}\":\"{1}\",", kvp.Key, ""));
                    }
                }
                if (sb.Length > 1)
                    sb.Remove(sb.Length - 1, 1);
                sb.Append("}");
                return sb.ToString();
            }

            /// <summary>
            /// 反序列化JSONArray对象
            /// </summary>
            /// <param name="jsonArray"></param>
            /// <returns></returns>
            public static string SerializeArray(JsonArray jsonArray)
            {
                var sb = new StringBuilder();
                sb.Append("[");
                foreach (object t in jsonArray)
                {
                    if (t is JsonObject)
                    {
                        sb.Append(string.Format("{0},", SerializeObject((JsonObject)t)));
                    }
                    else if (t is JsonArray)
                    {
                        sb.Append(string.Format("{0},", SerializeArray((JsonArray)t)));
                    }
                    else if (t is String)
                    {
                        sb.Append(string.Format("\"{0}\",", t));
                    }
                    else
                    {
                        sb.Append(string.Format("\"{0}\",", ""));
                    }
                }
                if (sb.Length > 1)
                    sb.Remove(sb.Length - 1, 1);
                sb.Append("]");
                return sb.ToString();
            }
            #endregion
        }

        /// <summary>
        /// 类  名：JsonObject
        /// 描  述：Json对象类
        /// </summary>
        public class JsonObject : Dictionary<string, object>
        { }

        /// <summary>
        /// 类  名：JsonArray
        /// 描  述：Json数组类
        /// </summary>
        public class JsonArray : List<object>
        { }

        /// <summary>
        /// 调用示例
        /// </summary>
        public static void JsonTest()
        {
            //控制台调用示例
            //序列化
            var array = new JsonArray { "1", "2", "3", "4" };
            var obj = new JsonObject { { "oneKey", "one" }, { "twoArray", array } };//新建json对象作为内嵌

            var jsonArray = new JsonArray { "2006", "2007", "2008", "2009", "2010" };

            var jsonObject = new JsonObject { { "domain", "mzwu.com" }, { "two", obj }, { "years", jsonArray } };
            Console.WriteLine("json序列化为字符串");
            Console.WriteLine(JsonConvert.SerializeObject(jsonObject));//执行序列化

            //反序列化
            JsonObject json = JsonConvert.DeserializeObject("{\"domain\":\"mzwu.com\",\"two\":{\"oneKey\":\"one\",\"twoArray\":[1,2,3,4]},\"years\":[2006,2007,2008,2009,2010]}");//执行反序列化
            if (json != null)
            {
                Console.WriteLine("将json结构的字符串反序列化为json对象并调用");
                Console.WriteLine(json["domain"]);
                Console.WriteLine(((JsonObject)json["two"])["oneKey"]);
                Console.WriteLine(((JsonArray)((JsonObject)json["two"])["twoArray"])[0]);
                Console.WriteLine(((JsonArray)json["years"])[3]);
            }

            Console.ReadLine();
        }
    }
}
