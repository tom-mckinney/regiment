﻿using System.Collections.Generic;
using System.IO;

namespace Regi.Models
{
    public interface IFileSystemDictionary : IDictionary<string, FileSystemInfo>
    {
        string Name { get; }
        string Path { get; }
    }

    public class FileSystemDictionary : Dictionary<string, FileSystemInfo>, IFileSystemDictionary
    {
        public FileSystemDictionary(DirectoryInfo directory)
        {
            Name = directory.Name;
            Path = directory.FullName;

            foreach (var directoryItem in directory.GetFileSystemInfos())
            {
                Add(directoryItem);
            }
        }

        public string Name { get; private set; }

        public string Path { get; private set; }

        public void Add(FileSystemInfo fileSystemInfo)
        {
            Add(fileSystemInfo.Name, fileSystemInfo);
        }
    }
}
