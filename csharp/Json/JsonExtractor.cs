using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MyCodeSnippet.Json
{
    /// <summary>
    /// extract every JSON object from a big JSON array. less validation for json content
    /// </summary>
    public class JsonExtractor
    {
        private const int BUFFER_SIZE = 4096; // 4KB

        private int depth = 0;
        private bool endQuatation = true;

        private readonly Stream stream;

        public JsonExtractor(Stream stream)
        {
            this.stream = stream;
        }

        public IEnumerable<string> Extract()
        {

        }


        private static IEnumerable<char> ReadSequence(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                char[] buffer = new char[BUFFER_SIZE];
                int count = 0;
                while ((count = reader.ReadBlock(buffer, 0, BUFFER_SIZE)) > 0)
                {
                    for (var i = 0; i < count; ++i)
                    {
                        yield return buffer[i];
                    }
                }
            }
        }


    }
}


