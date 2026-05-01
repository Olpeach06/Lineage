using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Lineage.Classes
{
    public static class PhotoHelper
    {
        private static string _imagesFolderPath = null;

        public static string GetImagesFolderPath()
        {
            if (_imagesFolderPath == null)
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string projectDirectory = Path.GetFullPath(Path.Combine(baseDirectory, @"..\..\..\"));
                _imagesFolderPath = Path.Combine(projectDirectory, "Images");

                if (!Directory.Exists(_imagesFolderPath))
                    Directory.CreateDirectory(_imagesFolderPath);
            }
            return _imagesFolderPath;
        }

        public static string GetProfilePhoto(string photoPath)
        {
            string imagesFolder = GetImagesFolderPath();
            string defaultPhoto = Path.Combine(imagesFolder, "icon-profile.png");

            if (string.IsNullOrEmpty(photoPath))
                return defaultPhoto;

            if (File.Exists(photoPath))
                return photoPath;

            string fileName = Path.GetFileName(photoPath);
            string fullPath = Path.Combine(imagesFolder, fileName);

            if (File.Exists(fullPath))
                return fullPath;

            return defaultPhoto;
        }
    }
}