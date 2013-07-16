using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Hange.Utility
{
    public class CsvReader
    {
        private string fileName;

        private string content = string.Empty;

        private int index = 0;

        private int state = 0;

        public CsvReader(string file)
        {
            if (!File.Exists(file))
            {
                throw new FileNotFoundException("文件未找到", file);
            }

            fileName = file;

            var sr = new StreamReader(fileName, Encoding.Default);

            content = sr.ReadToEnd();

            sr.Close();
        }

        public CsvReader(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            var sr = new StreamReader(stream, Encoding.Default);

            content = sr.ReadToEnd();

            sr.Close();
        }

        public string[] ReadLine()
        {
            if (index >= content.Length)
                return null;

            var list = new List<string>();

            var cell = new StringBuilder();

            bool finish = false;

            for (; index < content.Length; index++)
            {
                char character = content[index];

                switch (character)
                {
                    case ',':
                        if (state == 0)
                        {
                            list.Add(cell.ToString());
                            cell.Remove(0, cell.Length);
                        }
                        else if (state == 1)
                        {
                            cell.Append(character);
                        }
                        break;

                    case '"':
                        if (state == 0)
                        {
                            state = 1;
                            break;
                        }
                        if (state == 1)
                        {
                            if (index + 1 < content.Length && content[index + 1] == '"')
                            {
                                index = index + 1;
                                cell.Append(character);
                                break;
                            }
                            state = 0;
                            break;
                        }
                        break;

                    default:
                        cell.Append(character);
                        break;

                    case '\r':
                        if (state == 0 && (index + 1) < content.Length && content[index + 1] == '\n')
                        {
                            list.Add(cell.ToString());
                            index = index + 2;
                            finish = true;
                            break;
                        }

                        if (state == 1 && (index + 1) < content.Length && content[index + 1] == '\n')
                        {
                            index = index + 1;
                            break;
                        }
                        //cell.Append(character);
                        break;
                }

                if (finish)
                {
                    break;
                }
            }

            if (index >= content.Length && cell.Length > 0)
                list.Add(cell.ToString());

            return list.ToArray();
        }
    }
}
