using SNESassetsWPF.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SNESassetsWPF.Models
{
    public class FileNode
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public FileType Type { get; set; }

        /// <summary>
        /// True if this file is a built-in test asset rather than a real file on disk.
        /// </summary>
        public bool IsBuiltIn { get; set; } = false;



        public FileNode() { }  // keep old usage working



        public FileNode(string name , string fullPath , FileType type)
        {
            Name = name;
            FullPath = fullPath;
            Type = type;
        }
    }

}
