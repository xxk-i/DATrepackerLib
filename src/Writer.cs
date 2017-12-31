using System;
using System.IO;
using System.Text;

namespace DATRepackerLib
{
    public class Writer
    {
        private static readonly byte[] MAGIC = { 0x44, 0x41, 0x54, 0x00 };
        private static readonly byte[] NUL = { 0x00 };
        private readonly FilePackInfo PackInfo;

        public Writer(FilePackInfo packInfo) 
        {
            PackInfo = packInfo;
        }

        public void WriteToFile(string outFile)
        {
            if (PackInfo == null)
            {
                throw new InvalidOperationException(
                        "Error: need valid FilePackInfo to write file");
            }

            try 
            {
                using (var fs = new FileStream(
                        outFile, FileMode.Create, FileAccess.Write))
                {
                    WriteFile(fs);
                }
            }
            catch (Exception ex) 
            {
                throw ex;
            }

        }

        // pad int to 4 bytes, little endian
        private byte[] PaddedBytes(int num)
        {
            byte[] bytes = BitConverter.GetBytes(num);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            return bytes;
        }

        private static void _write(FileStream stream, byte[] bytes)
        {
            //FIXME: this might break?
            stream.Write(bytes, 0, bytes.Length);
        }

        private static int _calculate16BytePadding(int offset)
        {
            int overflow = offset % 16;
            if (overflow == 0)
            {
                return 0;
            }

            return 16 - overflow;
        }

        private static byte[] NulPad(int length)
        {
            byte[] pad = new byte[length];
            return pad;
        }

        // Go to town
        private void WriteFile(FileStream stream)
        {
            // for brevity
            FilePackInfo pi = PackInfo;

            ///////////
            // First 32 bytes
            ///////////

            stream.Write(MAGIC, 0, 4);
            _write(stream, PaddedBytes(pi.FileCount));
            // There are 5 tables
            foreach (int offset in pi.GetHeaderTableOffsets()) 
            {
                _write(stream, PaddedBytes(offset));
            }

            // 4 bytes pads out to 32
            _write(stream, PaddedBytes(0x00));

            ///////////
            // end of offset data
            ///////////

            // File offset table
            int fileOffset = pi.CrcTable.EndOffset();
            foreach (string file in pi.QualifiedFileNames)
            {
                // eh
                int size = (int) pi.FileSizeDict[file];
                _write(stream, PaddedBytes(fileOffset));
                fileOffset += size;
                fileOffset += _calculate16BytePadding(fileOffset);
            }

            // Extension table
            foreach (string ext in pi.FileExtensions)
            {
                _write(stream, Encoding.ASCII.GetBytes(ext));
                _write(stream, NUL);
            }

            // Name table
            _write(stream, PaddedBytes(pi.NameTableBlockSize));
            foreach (string file in pi.FileBaseNames)
            {
                // Not little-endian
                int padLength = pi.NameTableBlockSize - file.Length;
                _write(stream, Encoding.ASCII.GetBytes(file));
                _write(stream, NulPad(padLength));
            }
            // make sure to fall on a 4-byte boundary
            int overflow = (int) stream.Position % 4;
            if (overflow != 0)
            {
                _write(stream, NulPad(4 - overflow));
            }

            // Size table
            foreach (string file in pi.QualifiedFileNames)
            {
                _write(stream, PaddedBytes((int) pi.FileSizeDict[file]));
            }

            // CRC table (just nuls)
            _write(stream, NulPad(pi.CrcTable.TotalSize()));


            // Now just glom on files
            foreach (string file in pi.QualifiedFileNames)
            {
                // Files have to start on 16 byte boundaries, therefore
                // the offset % 16 will always be zero and can be ignored
                int size = (int) pi.FileSizeDict[file];
                int underflow = _calculate16BytePadding(size);

                // Open the file and write it
                byte[] fileBytes = File.ReadAllBytes(file);
                _write(stream, fileBytes);
                if (underflow > 0)
                {
                    _write(stream, NulPad(underflow));
                }
            }
        }
    }
}
