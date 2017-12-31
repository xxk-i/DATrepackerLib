using System.Collections.Generic;
using System.IO;

// Lol what is c#
namespace DATRepackerLib
{
    public class FilePackInfo
    {
        private string PackDirectory;

        internal int FileCount = 0;
        internal List<string> QualifiedFileNames = new List<string>();
        internal List<string> FileBaseNames = new List<string>();
        internal List<string> FileExtensions;

        internal MetadataTable FileOffsetTable;
        internal MetadataTable ExtensionTable;
        internal MetadataTable NameTable;
        // Length of longest name + 1
        internal int NameTableBlockSize;
        internal MetadataTable SizeTable;
        internal MetadataTable CrcTable;
        internal Dictionary<string, long> FileSizeDict; 

        public FilePackInfo(string inDirectory)
        {
            PackDirectory = inDirectory;
            QualifiedFileNames = new List<string>(Directory.GetFiles(PackDirectory));
            FileBaseNames = GetFileBaseNames();
            FileCount = QualifiedFileNames.Count;

            FileExtensions = GetFileExtensions();
            NameTableBlockSize = GetNameTableBlockSize();
            FileSizeDict = GetFileSizeDict();

            FileOffsetTable = new MetadataTable
            {
                Size = 4 * FileCount,
                Offset = 32  // Header is fixed at 32 bytes
            };

            ExtensionTable = new MetadataTable
            {
                Size = 4 * FileCount,
                Offset = FileOffsetTable.EndOffset()
            };

            NameTable = new MetadataTable
            {
                // 4 bytes for blocksize, then one block per file
                Size = 4 + NameTableBlockSize * FileCount,
                Offset = ExtensionTable.EndOffset(),
            };
            NameTable.EndPadding = NameTable.EndOffset() % 4 == 0 
                    ? 0 
                    : 4 - (NameTable.EndOffset() % 4);

            SizeTable = new MetadataTable
            {
                Size = 4 * FileCount,
                Offset = NameTable.EndOffset()
            };

            // I'm just faking crc table as 16 NUL bytes and then padded to 
            // 16 total bytes
            CrcTable = new MetadataTable
            {
                Size = 16,
                Offset = SizeTable.EndOffset(),
            };
            CrcTable.EndPadding = CrcTable.EndOffset() % 16 == 0 
                    ? 0
                    : 16 - (CrcTable.EndOffset() % 16);
        }

        private List<string> GetFileExtensions() 
        {
            List<string> extensions = new List<string>(FileCount);
            foreach (string file in QualifiedFileNames) 
            {
                string path = Path.GetExtension(file);
                path = path.TrimStart('.');
                extensions.Add(path);
            }

            return extensions;
        }

        /// <summary>
        /// Block size is the longest file name + 1 for NUL byte
        /// </summary>
        /// <returns>Blocksize</returns>
        private int GetNameTableBlockSize()
        {
            int size = 0;
            foreach (string file in FileBaseNames) 
            {
                if (file.Length > size) 
                {
                    size = file.Length;
                }
            }

            return size + 1;
        }

        private List<string> GetFileBaseNames()
        {
            List<string> basenames = new List<string>(FileCount);
            foreach (string file in QualifiedFileNames)
            {
                FileInfo fi = new FileInfo(file);
                basenames.Add(fi.Name);
            }

            return basenames;
        }

        private Dictionary<string, long> GetFileSizeDict() 
        {
            Dictionary<string, long> d = new Dictionary<string, long>();
            foreach (string file in QualifiedFileNames) 
            {
                d.Add(file, (new FileInfo(file)).Length);
            }

            return d;
        }

        internal List<int> GetHeaderTableOffsets() 
        {
            return new List<int>
            {
                FileOffsetTable.Offset,
                ExtensionTable.Offset,
                NameTable.Offset,
                SizeTable.Offset,
                CrcTable.Offset
            };
        }

        internal struct MetadataTable
        {
            // All in bytes
            internal int Offset;
            internal int Size;
            internal int EndPadding;

            internal int EndOffset()
            {
                return Offset + Size + EndPadding;
            }

            internal int TotalSize()
            {
                return Size + EndPadding;
            }
        }

    }
}
