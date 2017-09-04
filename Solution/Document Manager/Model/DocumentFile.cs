using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
namespace FileManager.Model
{
    [Table("DocumentFile")]
    public class DocumentFile : BaseEntity
    {
       
        // constructors
        public DocumentFile() { }

        public DocumentFile(string FilePath)
        {
            this.UpdateFileInfo(FilePath);
        }

        public void UpdateFileInfo(string FilePath)
        {
            this.FilePath = FilePath;
            this.FileName = DocumentFile.GetFileNameFromPath(FilePath);
            this.FolderPath = DocumentFile.GetFolderFromPath(FilePath);
            this.FileType = DocumentFile.GetFileTypeFromPath(FilePath);
            this.DocumentHash = Document.GenerateFileHash(FilePath);
            this.FileModifiedTimestamp = File.GetLastWriteTime(FilePath);
            this.FileCreatedTimestamp = File.GetCreationTime(FilePath);
            this.PathDepth = FilePath.Count(x => x.Equals('\\')) - 1;
        }

        [Index("IX_FilePath", IsUnique = true, Order = 1)]
        [Key, Required, MaxLength(4000)]
        public string FilePath { get; set; }

        [Required, MaxLength(4000)]
        public string FolderPath { get; set; }

        [Required, MaxLength(500)]
        public string FileName { get; set; }

        [Required]
        public DateTime FileCreatedTimestamp { get; set; }
        
        [Required]
        public DateTime FileModifiedTimestamp { get; set; }
        [Required]
        public int PathDepth { get; set; }

        [Required, MaxLength(500)]
        public string DocumentHash { get; set; }
        
        [ForeignKey("DocumentHash")]
        public virtual Document Document { get; set; }


        [MaxLength(20)]
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

    }
}
