using System;
using System.IO;
using System.Text;

namespace SW.Data
{
    /// <summary>기존 파일을 삭제하지 않고 기록이 끝난 임시 파일로 교체합니다.</summary>
    internal static class SWAtomicFile
    {
        /// <summary>내용을 기록하고 교체합니다. 원자적 교체가 지원되지 않으면 기존 파일을 유지하며 예외를 반환합니다.</summary>
        internal static void WriteAllText(string path, string content, string backupPath = null)
        {
            string temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (FileStream stream = new(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    using (StreamWriter writer = new(stream, new UTF8Encoding(false), 1024, true))
                    {
                        writer.Write(content);
                        writer.Flush();
                    }
                    stream.Flush(true);
                }
                if (File.Exists(path))
                    File.Replace(temporaryPath, path, backupPath);
                else
                    File.Move(temporaryPath, path);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }
    }
}
