using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCodeSnippet
{
    internal class ReadState
    {
        public int depth = 0;
        public bool endQuatation = true;
        public bool endArray = true;
    }

    class Program
    {

        public static int MAX_DOC_SIZE = 2 * 1024 * 1024;

        static void Main(string[] args)
        {
            //var path = args[0];

            if (args.Any(s => s == "--debug"))
            {
                args = args.Where(s => s != "--debug").ToArray();
                Debugger.Launch();
            }

            if (args.Length > 0 && args[0] == "--tsv")
            {
                var path = args[1];

                var output = path + ".tsv";
                var lineCount = 0;
                var watch = new Stopwatch();
                watch.Start();
                using (var file = new FileStream(output, FileMode.Create, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(file, Encoding.UTF8))
                {
                    foreach (var r in ConvertBigJsonToTSV(path))
                    {
                        ++lineCount;
                        writer.WriteLine(r);
                        if (lineCount % 10000 == 0)
                        {
                            Console.WriteLine($"{(int)watch.Elapsed.TotalSeconds}s: {lineCount}");
                        }
                    }
                }
                Console.WriteLine($"{watch.Elapsed.TotalMinutes}mins: {lineCount}");
                return;
            }

            if (args.Length > 0 && args[0] == "--verify-json-tsv")
            {
                var path = args[1];
                VerifyTSVJson(path);
                return;
            }

            // read file, and give text before&after offset
            if (args.Length > 0 && args[0] == "--offset")
            {
                var path = args[1];
                var offset = args[2];
                var count = args[3];

                ReadOffset(path, long.Parse(offset), long.Parse(count));
            }
        }

        private static IEnumerable<char> ReadSequence(string path)
        {
            using (var reader = new StreamReader(File.OpenRead(path)))
            {
                char[] buffer = new char[1024];
                int count = 0;
                while ((count = reader.ReadBlock(buffer, 0, 1024)) > 0)
                {
                    for (var i = 0; i < count; ++i)
                    {
                        yield return buffer[i];
                    }
                }
            }
        }

        /// <summary>
        /// return true if need skip the current char
        /// </summary>
        /// <param name="ch"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private static bool AdjustState(char preCh, char ch, ReadState state)
        {
            switch (ch)
            {
                case '"':
                    {
                        if (preCh != '\\')
                        {
                            state.endQuatation = !state.endQuatation;
                        }
                    }
                    break;

                case '{':
                    {
                        if (state.endQuatation) ++state.depth;
                    }
                    break;
                case '}':
                    {
                        if (state.endQuatation) --state.depth;
                    }
                    break;
                case ',':
                    {
                        if (state.depth == 0) return true;
                    }
                    break;
                case '\n':
                case '\r':
                    return true;
                default:
                    break;
            }
            return false;
        }

        private static IEnumerable<string> ConvertBigJsonToTSV(string path)
        {
            var sb = new StringBuilder();
            var sbStartOffset = 0L;
            var sbEndOffset = 0L;
            var lineCount = 0;

            var offset = -1L;
            char start = '0';
            var state = new ReadState
            {
                depth = 0,
                endQuatation = true
            };

            char preCh = '0';
            foreach (var ch in ReadSequence(path))
            {
                //if(lineCount== 717232)
                //{
                //    Debugger.Launch();
                //}

                ++offset;
                if (start == '0')
                {
                    if (ch != '{' && ch != '[')
                    {
                        continue;
                    }

                    start = ch;

                    // single object
                    if (start == '{')
                    {
                        sb.Append(ch);
                        ++state.depth;
                    }

                    continue;
                }


                // single object
                if (!AdjustState(preCh, ch, state))
                {

                    var isLast = ch == ']' && start == '[' && state.endQuatation && state.depth == 0;
                    if (!isLast)
                    {
                        sb.Append(ch);
                    }

                    if (sb.Length >= MAX_DOC_SIZE)
                    {
                        sbEndOffset = offset;
                        Console.WriteLine($"Big JSON: start offset={sbStartOffset}, end={sbEndOffset}, lineCount={lineCount}");
                        File.WriteAllText(path + "." + sbStartOffset + ".txt", sb.ToString());
                        yield break;
                    }

                    if (state.endQuatation && state.depth == 0)
                    {
                        var line = sb.ToString();
                        sb.Clear();
                        sbStartOffset = offset;

                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            ++lineCount;
                            yield return line;
                        }
                    }
                }

                preCh = ch;
            }
        }


        private static void VerifyTSVJson(string path)
        {
            using (var reader = new StreamReader(path))
            {
                int number = -1;
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    ++number;
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    try
                    {
                        var json = JObject.Parse(line);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Invalid Json Line {number}: {line}");
                    }
                }
            }
        }


        private static void ReadOffset(string path, long offset, long count)
        {
            var start = Math.Max(0, offset - count);
            var end = offset + count;
            var sb = new StringBuilder();

            var i = -1L;
            foreach (var ch in ReadSequence(path))
            {
                ++i;
                if (i >= start && i <= end) sb.Append(ch);

                if (i == offset)
                {
                    sb.Append("###Here###");
                }

                if (i >= end) break;
            }

            Console.WriteLine(sb.ToString());
        }
    }
}
