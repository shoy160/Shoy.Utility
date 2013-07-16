using System.Collections;
using System.Linq;
using System;
using System.Globalization;
using System.Xml.Serialization;
using System.IO;

namespace Hange.Utility.Extend
{
    public static class ObjectExtension
    {
        public static bool In(this object o, IEnumerable c)
        {
            return c.Cast<object>().Contains(o);
        }

        public static bool In<T>(this T t, params T[] c)
        {
            return c.Any(i => i.Equals(t));
        }

        public static T ObjectToT<T>(this object obj, T def)
        {
            try
            {
                var type = typeof (T);
                if (type.Name == "Nullable`1")
                    type = type.GetGenericArguments()[0];
                return (T) Convert.ChangeType(obj, type, CultureInfo.InvariantCulture);
            }
            catch
            {
                return def;
            }
        }

        public static T ObjectToT<T>(this object obj)
        {
            return obj.ObjectToT(default(T));
        }

        public static void WriteTo(this Exception ex, string path)
        {
            Utils.WriteException(path, ex);
        }

        /// <summary>
        /// xml序列化
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool ObjectToXml(this object obj,string path)
        {
            if (obj == null) return false;
            var directory = Path.GetDirectoryName(path);
            if (directory == null || !Directory.Exists(directory))
                return false;
            FileStream fs = null;
            var tmp = path + ".tmp";
            try
            {
                var serializer = new XmlSerializer(obj.GetType());
                fs = new FileStream(tmp, FileMode.OpenOrCreate, FileAccess.Write);
                serializer.Serialize(fs, obj);
            }
            catch
            {
                return false;
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }
            return Utils.MoveFile(tmp, path, true);
        }

        /// <summary>
        /// Xml反序列化
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="path">xml路径</param>
        /// <returns></returns>
        public static T XmlToObject<T>(this string path)
        {
            if (!File.Exists(path))
                return default(T);
            FileStream fs = null;
            try
            {
                fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                var serializer = new XmlSerializer(typeof (T));
                return serializer.Deserialize(fs).ObjectToT<T>();
            }
            catch
            {
                return default(T);
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }
        }
    }
}