using System.IO;

namespace Lucene.Net.IndexProvider.Helpers;

public static class FileHelpers
{
    public static void CopyFilesRecursively(string sourcePath, string targetPath)
    {
        if (!Directory.Exists(targetPath))
        {
            Directory.CreateDirectory(targetPath);
        }

        //Now Create all of the directories
        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        }

        //Copy all the files & Replaces any files with the same name
        foreach (string filePath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
        {
            var outputFilePath = filePath.Replace(sourcePath, targetPath);
            File.Copy(
                filePath,
                outputFilePath,
                true);

            using (var inputFile = new FileStream(
                       filePath,
                       FileMode.Open,
                       FileAccess.Read,
                       FileShare.ReadWrite))
            {
                using (var outputFile = new FileStream(outputFilePath, FileMode.Create))
                {
                    var buffer = new byte[0x10000];
                    int bytes;

                    while ((bytes = inputFile.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        outputFile.Write(buffer, 0, bytes);
                    }
                }
            }
        }
    }
}