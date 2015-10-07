using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Impl.Common.HashFile
{
    internal static class HashFileUpdater
    {
        public static void UpdateFile(string fileName)
        {
            var fileInfo = new FileInfo(fileName);
            if (!fileInfo.Exists)
                return;

            var otherFiles = fileInfo.Directory.GetFiles().ToList();
                        
            otherFiles.Sort(((info, info1) => string.CompareOrdinal(info1.Name, info.Name)));
            otherFiles.ForEach(x=>FilterFile(x, fileInfo));            
        }

        private static void FilterFile(FileInfo fileInfo, FileInfo rootFile)
        {
            if(fileInfo.Extension!= rootFile.Extension)
                return;
            var fileInfoName = fileInfo.Name.Replace(fileInfo.Extension, string.Empty);
            var rootFileName = rootFile.Name.Replace(rootFile.Extension, string.Empty);

            var tail = fileInfoName.Replace(rootFileName, string.Empty);

            int version = 0;
            if (int.TryParse(tail, out version) || tail == string.Empty)
            {
                File.Move(fileInfo.FullName,
                    string.Format("{0}\\{1}{2}{3}", fileInfo.DirectoryName, rootFileName, version + 1,
                        rootFile.Extension));                
            }
        }
    }
}
