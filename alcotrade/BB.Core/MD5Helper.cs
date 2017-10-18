using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BB.Core
{
    public static class MD5Helper
    {
        public static byte[] CalculateMD5Hash(this byte[] inputData)
        {
            if (inputData == null)
                throw new ArgumentNullException("inputData");

            using (var hasher = MD5.Create())
            {
                return hasher.ComputeHash(inputData);
            }
        }

        public static byte[] CalculateMD5HashForFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException("filePath");

            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    return md5.ComputeHash(stream);
                }
            }
        }

        public static string CalculateMD5Hash(this string inputData, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;

            var inputBytes = encoding.GetBytes(inputData);
            var hash = CalculateMD5Hash(inputBytes);

            return Convert.ToBase64String(hash);
        }

        public static string GenerateSmartCardStopListItemId(string crystalSerialNumber, string outerNumber, int smartCardTypeCode)
        {
            var stringForHash = String.Format("{0}_{1}_{2}", crystalSerialNumber, outerNumber, smartCardTypeCode);
            return stringForHash.CalculateMD5Hash();
        }

        public static byte[] ComputeHashForFileListWithoutSalt(string[] filePaths)
        {
            return ComputeHashForFileList(filePaths, File.ReadAllBytes);
        }

        public static byte[] ComputeHashForFileListWithSalt(string[] filePaths, string salt)
        {
            if (string.IsNullOrWhiteSpace(salt))
                throw new ArgumentNullException("salt");

            var saltBytes = Encoding.UTF8.GetBytes(salt);

            return ComputeHashForFileList(filePaths, filePath => File.ReadAllBytes(filePath).Concat(saltBytes).ToArray());
        }

        public static byte[] ComputeHashForFileList(string[] filePaths, Func<string, byte[]> readFileFunc)
        {
            var md5 = MD5.Create();

            for (int i = 0; i < filePaths.Length; i++)
            {
                string filePath = filePaths[i];

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException(Path.GetFileName(filePath));
                }

                byte[] fileContentInBytes = readFileFunc(filePath);

                if (i == filePaths.Length - 1)
                    md5.TransformFinalBlock(fileContentInBytes, 0, fileContentInBytes.Length);
                else
                    md5.TransformBlock(fileContentInBytes, 0, fileContentInBytes.Length, fileContentInBytes, 0);
            }

            return md5.Hash;
        }

    }
}

