using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Resplado.zip;
static class ZipFileWithProgress
{
    public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, IProgress<double> progress)
    {
        sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);

        FileInfo[] sourceFiles = new DirectoryInfo(sourceDirectoryName)
            .GetFiles("*", SearchOption.AllDirectories)
            .OrderByDescending(x => x.LastWriteTime)
            .ToArray();
       
        double totalBytes = sourceFiles.Sum(f => f.Length);

        long currentBytes = 0;

        using (ZipArchive archive = ZipFile.Open(destinationArchiveFileName, ZipArchiveMode.Create))
        {
            foreach (FileInfo file in sourceFiles)
            {
                string entryName = file.FullName.Substring(sourceDirectoryName.Length + 1);
                ZipArchiveEntry entry = archive.CreateEntry(entryName);

                entry.LastWriteTime = file.LastWriteTime;

                using (Stream inputStream = File.OpenRead(file.FullName))
                using (Stream outputStream = entry.Open())
                {
                    Stream progressStream = new StreamWithProgress(inputStream,
                        new BasicProgress<int>(i =>
                        {
                            currentBytes += i;
                            progress.Report(currentBytes / totalBytes);
                        }), null);
                    progressStream.CopyTo(outputStream);
                }
            }
        }
    }
    public static async Task CreateFromDirectoryAsync(string sourceDirectoryName, string destinationArchiveFileName, IProgress<double> progress)
    {
        sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);

        var sourceFiles = GetOrderedSourceFiles(sourceDirectoryName);
        double avance = 0;
        double totalBytes = sourceFiles.Sum(f => f.Length);
        long currentBytes = 0;
        using (var archive = CreateZipArchive(destinationArchiveFileName))
        {
            foreach (var file in sourceFiles)
            {
                string entryName = GetEntryName(file, sourceDirectoryName);
                var entry = archive.CreateEntry(entryName);
                entry.LastWriteTime = file.LastWriteTime;

                using (var inputStream = File.OpenRead(file.FullName))
                using (var outputStream = entry.Open())
                {
                    var progressStream = new StreamWithProgress(inputStream, new BasicProgress<int>(i =>
                    {
                        currentBytes += i;
                        avance = (double)currentBytes / totalBytes;
                        progress.Report(avance);
                    }), null);

                    await progressStream.CopyToAsync(outputStream);
                }
            }
        }
        //if (Convert.ToInt32((avance * 100))==100)
        if (avance >= 1.0)
        {
            DeleteFiles(sourceFiles);
        }
    }
    private static FileInfo[] GetOrderedSourceFiles(string sourceDirectoryName)
    {
        return new DirectoryInfo(sourceDirectoryName)
            .GetFiles("*", SearchOption.AllDirectories)
            .OrderByDescending(x => x.LastWriteTime)
            .Skip(1)
            .ToArray();
    }
    private static void DeleteFiles(FileInfo[] files)
    {
        foreach (var item in files)
        {
            item.Delete();
        }
    }
    private static ZipArchive CreateZipArchive(string destinationArchiveFileName)
    {
        return new ZipArchive(File.Create(destinationArchiveFileName), ZipArchiveMode.Create);
    }
    private static string GetEntryName(FileInfo file, string sourceDirectoryName)
    {
        return file.FullName.Substring(sourceDirectoryName.Length + 1);
    }
 
    public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, IProgress<double> progress)
    {
        using (ZipArchive archive = ZipFile.OpenRead(sourceArchiveFileName))
        {
            double totalBytes = archive.Entries.Sum(e => e.Length);
            long currentBytes = 0;

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string fileName = Path.Combine(destinationDirectoryName, entry.FullName);

                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                using (Stream inputStream = entry.Open())
                using (Stream outputStream = File.OpenWrite(fileName))
                {
                    Stream progressStream = new StreamWithProgress(outputStream, null,
                        new BasicProgress<int>(i =>
                        {
                            currentBytes += i;
                            progress.Report(currentBytes / totalBytes);
                        }));

                    inputStream.CopyTo(progressStream);
                }

                File.SetLastWriteTime(fileName, entry.LastWriteTime.LocalDateTime);
            }
        }
    }
}