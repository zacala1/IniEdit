using System;
using System.IO;
using System.Text;
using System.Linq;

namespace IniEdit.GUI
{
    /// <summary>
    /// Helper class for detecting and converting file encodings
    /// </summary>
    public static class EncodingHelper
    {
        /// <summary>
        /// Detect encoding of a file
        /// </summary>
        public static Encoding DetectEncoding(string filePath)
        {
            // Read first few bytes to detect BOM
            using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                if (file.Length < 2)
                    return Encoding.UTF8;

                byte[] bom = new byte[4];
                int bytesRead = file.Read(bom, 0, 4);

                // Check for BOM
                if (bytesRead >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
                    return Encoding.UTF8; // UTF-8 with BOM

                if (bytesRead >= 2 && bom[0] == 0xFF && bom[1] == 0xFE)
                    return Encoding.Unicode; // UTF-16 LE

                if (bytesRead >= 2 && bom[0] == 0xFE && bom[1] == 0xFF)
                    return Encoding.BigEndianUnicode; // UTF-16 BE

                if (bytesRead >= 4 && bom[0] == 0xFF && bom[1] == 0xFE && bom[2] == 0x00 && bom[3] == 0x00)
                    return Encoding.UTF32; // UTF-32 LE

                // No BOM detected, try to detect encoding by content
                file.Position = 0;
                byte[] buffer = new byte[Math.Min(file.Length, 4096)];
                file.Read(buffer, 0, buffer.Length);

                return DetectEncodingFromContent(buffer);
            }
        }

        /// <summary>
        /// Detect encoding from file content
        /// </summary>
        private static Encoding DetectEncodingFromContent(byte[] bytes)
        {
            // Check for non-ASCII characters
            bool hasHighBytes = bytes.Any(b => b > 127);

            if (!hasHighBytes)
            {
                // All ASCII, default to UTF-8
                return Encoding.UTF8;
            }

            // Try to detect UTF-8
            if (IsValidUtf8(bytes))
            {
                return Encoding.UTF8;
            }

            // Default to system default encoding (usually Windows-1252 on Windows)
            return Encoding.Default;
        }

        /// <summary>
        /// Check if byte array is valid UTF-8
        /// </summary>
        private static bool IsValidUtf8(byte[] bytes)
        {
            int i = 0;
            while (i < bytes.Length)
            {
                if (bytes[i] <= 0x7F) // ASCII
                {
                    i++;
                    continue;
                }

                int additionalBytes = 0;
                if ((bytes[i] & 0xE0) == 0xC0) additionalBytes = 1;
                else if ((bytes[i] & 0xF0) == 0xE0) additionalBytes = 2;
                else if ((bytes[i] & 0xF8) == 0xF0) additionalBytes = 3;
                else return false; // Invalid UTF-8 start byte

                if (i + additionalBytes >= bytes.Length)
                    return false;

                for (int j = 0; j < additionalBytes; j++)
                {
                    if ((bytes[i + 1 + j] & 0xC0) != 0x80)
                        return false; // Invalid UTF-8 continuation byte
                }

                i += additionalBytes + 1;
            }

            return true;
        }

        /// <summary>
        /// Get friendly encoding name
        /// </summary>
        public static string GetEncodingName(Encoding encoding)
        {
            if (encoding == null)
                return "Unknown";

            if (encoding.Equals(Encoding.UTF8))
                return "UTF-8";
            if (encoding.Equals(Encoding.Unicode))
                return "UTF-16 LE";
            if (encoding.Equals(Encoding.BigEndianUnicode))
                return "UTF-16 BE";
            if (encoding.Equals(Encoding.UTF32))
                return "UTF-32";
            if (encoding.Equals(Encoding.ASCII))
                return "ASCII";
            if (encoding.Equals(Encoding.Default))
                return $"System Default ({encoding.WebName})";

            return encoding.EncodingName;
        }

        /// <summary>
        /// Get all available encodings
        /// </summary>
        public static EncodingInfo[] GetAvailableEncodings()
        {
            return Encoding.GetEncodings()
                .OrderBy(e => e.DisplayName)
                .ToArray();
        }

        /// <summary>
        /// Convert file encoding
        /// </summary>
        public static void ConvertFileEncoding(string filePath, Encoding sourceEncoding, Encoding targetEncoding)
        {
            // Read file with source encoding
            string content = File.ReadAllText(filePath, sourceEncoding);

            // Write file with target encoding
            File.WriteAllText(filePath, content, targetEncoding);
        }
    }
}
