
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace NIRS_Demonstrator.Core
{
    /// <summary>
    /// 
    /// </summary>
    public class FileManager : IFileManager
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="path"></param>
        /// <param name="append"></param>
        /// <returns></returns>
        public async Task WriteTextToFileAsync(string text, string path, bool append = false)
        {
            // TODO: Add exeption catching

            //  Normalize path
            path = NormalizePath(path);

            // Resolve path
            path = ResolvePath(path);

            //
            await AsyncAwaiter.AwaitAsync(nameof(FileManager) + path, async () =>
            {
                
                //
                await IoC.Task.Run(() =>
                {
                    //
                    using (var fileStream = (TextWriter)new StreamWriter(File.Open(path, append ? FileMode.Append : FileMode.Create)))
                        fileStream.Write(text);
                });
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string NormalizePath(string path)
        {
            //
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                //
                return path?.Replace('/', '\\').Trim();
            //
            else
                //
                return path?.Replace('\\', '/').Trim();

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string ResolvePath(string path)
        {
            //
            return Path.GetFullPath(path);
        }
    }
}
