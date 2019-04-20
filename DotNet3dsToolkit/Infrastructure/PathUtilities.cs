using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNet3dsToolkit.Infrastructure
{
    public static class PathUtilities
    {
        /// <summary>
        /// Makes the given path a relative path
        /// </summary>
        /// <param name="targetPath">The path to make relative</param>
        /// <param name="relativeToPath">The path to which <paramref name="targetPath"/> is relative.  Must be a directory</param>
        /// <returns><paramref name="targetPath"/>, except relative to <paramref name="relativeToPath"/></returns>
        public static string MakeRelativePath(string targetPath, string relativeToPath)
        {
            var otherPathString = relativeToPath.Replace('\\', '/');
            if (!otherPathString.EndsWith("/"))
            {
                otherPathString += "/";
            }
            var absolutePath = new Uri("file://" + targetPath.Replace('\\', '/'));
            var otherPath = new Uri("file://" + otherPathString);

            return Uri.UnescapeDataString(otherPath.MakeRelativeUri(absolutePath).OriginalString);
        }
    }
}
