using System.Threading.Tasks;

namespace NIRS_Demonstrator.Core
{
    /// <summary>
    /// 
    /// </summary>
    public interface IFileManager
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="path"></param>
        /// <param name="append"></param>
        /// <returns></returns>
        Task WriteTextToFileAsync(string text, string path, bool append = false);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        string NormalizePath(string path);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        string ResolvePath(string path);

    }
}
