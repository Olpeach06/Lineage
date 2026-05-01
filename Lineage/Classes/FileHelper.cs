using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lineage.Classes
{
    public static class FileHelper
    {
        private static string _mediaFolderPath = null;

        /// <summary>
        /// Получает путь к папке Media в корне проекта
        /// </summary>
        public static string GetMediaFolderPath()
        {
            if (_mediaFolderPath == null)
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string projectDirectory = Path.GetFullPath(Path.Combine(baseDirectory, @"..\..\..\"));
                _mediaFolderPath = Path.Combine(projectDirectory, "Media");

                if (!Directory.Exists(_mediaFolderPath))
                    Directory.CreateDirectory(_mediaFolderPath);
            }
            return _mediaFolderPath;
        }

        /// <summary>
        /// Получает полный путь к файлу
        /// </summary>
        public static string GetFullFilePath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return null;

            string fileName = Path.GetFileName(relativePath);
            string mediaFolder = GetMediaFolderPath();
            string fullPath = Path.Combine(mediaFolder, fileName);

            return File.Exists(fullPath) ? fullPath : null;
        }

        /// <summary>
        /// Сохраняет файл в папку Media
        /// </summary>
        public static string SaveFileToMedia(string sourceFilePath, string targetFileName = null)
        {
            if (!File.Exists(sourceFilePath))
                return null;

            string mediaFolder = GetMediaFolderPath();
            string fileName = targetFileName ?? Path.GetFileName(sourceFilePath);
            string destPath = Path.Combine(mediaFolder, fileName);

            File.Copy(sourceFilePath, destPath, true);
            return fileName;
        }

        /// <summary>
        /// Удаляет файл из папки Media
        /// </summary>
        public static bool DeleteFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            string mediaFolder = GetMediaFolderPath();
            string fullPath = Path.Combine(mediaFolder, fileName);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return true;
            }
            return false;
        }
    }
}
