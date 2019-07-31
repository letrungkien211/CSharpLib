using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KL.AzureBlobSync
{
    internal static class LocalFileUtils
    {
        public static IEnumerable<string> ListFiles(string dirPath)
        {
            var skipDirectory = dirPath.Length;
            // because we don't want it to be prefixed by a slash
            // if dirPath like "C:\MyFolder", rather than "C:\MyFolder\"
            if (!dirPath.EndsWith("" + Path.DirectorySeparatorChar)) skipDirectory++;

            return Directory
                            .EnumerateFiles(dirPath, "*", SearchOption.AllDirectories)
                            .Select(f => f.Substring(skipDirectory));
        }
    }
}
