using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileManager.Model
{
    [Table("Document")]
    public class Document : BaseEntity
    {
        // constructors
        public Document() { }

        [Key, Required, MaxLength(200)]
        public string DocumentHash { get; set; }

        [MaxLength(100)]
        public string Scope { get; set; }

        [MaxLength(100)]
        public string Client { get; set; }

        [MaxLength(100)]
        public string Project { get; set; }
        
        [MaxLength(500)]
        public string OriginalFileName { get; set; }

        [MaxLength(4000)]
        public string OriginalFolderPath { get; set; }

        [MaxLength(500)]
        public string TargetFileName { get; set; }

        [MaxLength(4000)]
        public string TargetFolderBranch { get; set; }

        [Required]
        public long FileSize { get; set; }

        [MaxLength(200)]
        public string FileType { get; set; }

        [Required]
        public DateTime FileCreatedTimestamp { get; set; }

        [Required]
        public DateTime FileModifiedTimestamp { get; set; }


        public static string BuildFilePath(string RootFolder, string Branch, string Scope, string Client, string Project)
        {
            
            if (Branch == null || RootFolder == null)
            {
                throw new MissingFieldException("Branch or target root folder is missing");
            }

            string QualifiedBranch;

            // dymanically generate the folder branch
            QualifiedBranch = Branch.Replace("{client}", Client);
            QualifiedBranch = QualifiedBranch.Replace("{scope}", Scope);
            QualifiedBranch = QualifiedBranch.Replace("{project}", Project);
                
            return string.Join("\\", new string[] { RootFolder, QualifiedBranch });

        }        

        [Required]
        public Byte IgnoreFlag { get; set; } = FALSE_FLAG_VALUE;

        [NotMapped]
        public bool Ignore
        {
            get
            {
                return this.IgnoreFlag.Equals(TRUE_FLAG_VALUE);
            }
            set
            {
                this.IgnoreFlag = value ? TRUE_FLAG_VALUE : FALSE_FLAG_VALUE;
            }
        }

        public static string GenerateFileHash(string FilePath)
        {
            try
            { 
                FileStream file = new FileStream(FilePath, FileMode.Open, FileAccess.Read);
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(file);
                file.Close();

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();

            } catch (Exception e)
            {
                Console.WriteLine("Generate hash error encountered for \"{0}\". {1}", FilePath, e.Message);
                return "ERROR";
            }
        }

        
    }
}
