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

        public static string TargetRootFolder = null;

        // constructors
        public Document() { }

        [Index("IX_DocumentHash", IsUnique = true, Order = 1)]
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

        [NotMapped]
        public string TargetFilePath
        {
            
            get {

                if (this.TargetFolderBranch == null || TargetRootFolder == null)
                {
                    throw new MissingFieldException("Branch or target root folder is missing");
                }


                // dymanically generate the folder branch
                string Branch = this.TargetFolderBranch.Replace("{client}", this.Client);

                Branch = Branch.Replace("{scope}", this.Scope);
                Branch = Branch.Replace("{project}", this.Project);
                
                return string.Join("\\", new string[] { TargetRootFolder, Branch, this.TargetFileName });
            }
        }        

        public static string GenerateFileHash(string FilePath)
        {
            FileStream file = new FileStream(FilePath, FileMode.Open);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(file);
            file.Close();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
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


        public virtual ICollection<DocumentFile> DocumentFiles { get; set; }

    }
}
