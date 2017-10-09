using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Text.RegularExpressions;

namespace FileManager.Model
{
    [Table("DocumentFile")]
    public class DocumentFile : BaseEntity
    {
       
        // constructors
        public DocumentFile() { }

        public DocumentFile(string FilePath)
        {
            this.DocumentHash = Document.GenerateFileHash(FilePath);
            this.UpdateFileInfo(FilePath);
        }

        public DocumentFile(string FilePath, string DocumentHash)
        {
            this.DocumentHash = DocumentHash;
            this.UpdateFileInfo(FilePath);
        }

        public void UpdateFileInfo(string FilePath)
        {
            this.FilePath = FilePath;

            FileInfo fi = new FileInfo(FilePath);
            
            this.FileName = fi.Name;
            this.FolderPath = fi.DirectoryName;
            this.FileType = fi.Extension;
            this.FileModifiedTimestamp = fi.LastWriteTime;
            this.FileCreatedTimestamp = fi.CreationTime;
            this.FileSize = fi.Length;
            this.PathDepth = FilePath.Count(x => x.Equals('\\')) - 1;
        }

        [Key, Required, MaxLength(4000)]
        public string FilePath { get; set; }

        [Required, MaxLength(4000)]
        public string FolderPath { get; set; }

        [Required, MaxLength(1000)]
        public string FileName { get; set; }

        [Required]
        public DateTime FileCreatedTimestamp { get; set; }
        
        [Required]
        public DateTime FileModifiedTimestamp { get; set; }

        [Required]
        public long FileSize { get; set; }

        [Required]
        public int PathDepth { get; set; }

        [Required, MaxLength(500)]
        public string DocumentHash { get; set; }
        
        [MaxLength(200)]
        public string FileType { get; set; }

        [Required]
        public Byte DeletedFlag { get; set; } = FALSE_FLAG_VALUE;

        [NotMapped]
        public bool Deleted
        {
            get
            {
                return this.DeletedFlag.Equals(TRUE_FLAG_VALUE);
            }
            set
            {
                this.DeletedFlag = (value == true ? TRUE_FLAG_VALUE : FALSE_FLAG_VALUE);
            }
        }

        public static string GetFileNameFromPath(string FilePath)
        {
            return Path.GetFileName(FilePath);
        }

        public static string GetFolderFromPath(string FilePath)
        {
            return Path.GetDirectoryName(FilePath);
        }

        public static string GetFileTypeFromPath(string FilePath)
        {
            return Path.GetExtension(FilePath);
        }

        public static void AssertFolderExists(string FolderPath)
        {
            if(!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
            }
        }

        public static void MoveFile(string OriginalFilePath, string NewFilePath)
        {
            File.Move(OriginalFilePath, NewFilePath);
        }

        public static void CopyFile(string OriginalFilePath, string NewFilePath)
        {
            File.Copy(OriginalFilePath, NewFilePath);
        }

        public static string[] ExtractDocumentWords(string FolderPath, string FileName)
        {
            string[] pathwords = DocumentWord.ExtractPathWords(FolderPath);

            string[] filewords = DocumentWord.ExtractIndividualWords(FileName);

            string[] words = pathwords.Union(filewords).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

            return words;
        }

    }
}
