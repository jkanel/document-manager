using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FileManager.Model;
using FileManager.Data;
using System.Data.SqlClient;


namespace FileManager.Controller
{
    public class DocumentPublisher
    {
        private ApplicationContext AppContext;
        private string TargetRootFolder;
        List<DocumentFileInfo> FileInfoStore;

        public DocumentPublisher() { }

        public DocumentPublisher(ApplicationContext context, string TargetRootFolder)
        {
            this.AppContext = context;
            this.TargetRootFolder = TargetRootFolder;
        }
        public void Publish()
        {
            Publish(null);
        }

        /// <summary>
        /// Pulishes all relevant documents matching the target folder branch
        /// </summary>
        /// <param name="TargetFolderBranch">Filter of the target branch applied to documents that are published.</param>
        public void Publish(string FilterFolderBranch)
        {

            if (FilterFolderBranch == null) FilterFolderBranch = "";

            // build the target root folder
            string FilterFolderPath = BuildPath(this.TargetRootFolder, FilterFolderBranch);
            DocumentFile.AssertFolderExists(FilterFolderPath);

            // get file info under the target root
            this.FileInfoStore = GetFileInfoStoreFromPath(FilterFolderPath, true);

            // get target folders to publish
            string[] TargetFolderBranches = AppContext.Documents
                .Where(d => d.IgnoreFlag == Document.FALSE_FLAG_VALUE
                    && d.TargetFolderBranch.StartsWith(FilterFolderBranch))
                .Select(b => b.TargetFolderBranch)
                .Distinct()
                .ToArray<string>();

            string TargetFolderPath = null;

            // loop through folders
            foreach(string TargetFolderBranch in TargetFolderBranches)
            {
                TargetFolderPath = BuildPath(this.TargetRootFolder, TargetFolderBranch);
                PublishFolder(TargetFolderPath, TargetFolderBranch);
            }
        }

        private string BuildPath(string PrefixPath, string SuffixBranch)
        {
            if (SuffixBranch != null && SuffixBranch.Length > 0)
            {
                return PrefixPath + "\\" + SuffixBranch;
            }
            else
            {
                return PrefixPath;
            }
        }

        private static List<DocumentFileInfo> GetFileInfoStoreFromPath(string FolderPath, bool Recursive)
        {
            string[] FilePaths;

            if (Recursive)
            {
                FilePaths = Directory.GetFiles(FolderPath, "*", SearchOption.AllDirectories);
            }
            else
            {
                FilePaths = Directory.GetFiles(FolderPath, "*", SearchOption.TopDirectoryOnly);
            }

            Console.WriteLine("Creating stores at {0}.", DateTime.Now.ToString("s"));

            List<DocumentFileInfo> FileInfoStore = new List<DocumentFileInfo>();

            foreach (string FilePath in FilePaths)
            {
                FileInfoStore.Add(new DocumentFileInfo() { FilePath = FilePath, DocumentHash = Document.GenerateFileHash(FilePath) });
            }

            return FileInfoStore;

        }

        private class DocumentDetail
        {
            public string DocumentHash { get; set; }
            public string SourceFilePath { get; set; }
            public string TargetFileName { get; set; }
            public string TargetFolderBranch { get; set; }
            public string Scope { get; set; }
            public string Client { get; set; }
            public string Project { get; set; }
        }

        public void PublishFolder(string TargetFolderPath, string TargetFolderBranch)
        {

            string Query = @"SELECT
                  d.DocumentHash
                , fx.FilePath AS SourceFilePath
                , d.TargetFileName
                , d.TargetFolderBranch
                , d.Scope
                , d.Client
                , d.Project
                FROM
                Document d
                INNER JOIN(

                    SELECT
                      f.FilePath
                    , f.DocumentHash
                    , ROW_NUMBER() OVER (PARTITION BY f.DocumentHash
                        ORDER BY f.FilePath ASC) AS FilterIndex
                    FROM
                    DocumentFile f

                ) fx ON fx.DocumentHash = d.DocumentHash
                WHERE
                fx.FilterIndex = 1
                AND d.TargetFolderBranch LIKE @p0";

            SqlParameter param1 = new SqlParameter("p0", TargetFolderBranch + "%")
            {
                SqlDbType = System.Data.SqlDbType.VarChar,
                Direction = System.Data.ParameterDirection.Input
            };

            List<DocumentDetail> DocumentDetailList = AppContext.Database
                .SqlQuery<DocumentDetail>(Query, param1)
                .ToList();

            // loop through documents
            foreach(DocumentDetail detail in DocumentDetailList)
            {
                PublishDocument(
                    detail.SourceFilePath, 
                    TargetFolderPath,
                    detail.TargetFileName,
                    detail.DocumentHash);
            }
        }

        public void PublishDocument(string SourceFilePath, string TargetFolderPath, string TargetFileName, string DocumentHash)
        {
            // rename file in the same directory with Hash
            DocumentFileInfo dfi = this.FileInfoStore.Where(x => x.DocumentHash.Equals(DocumentHash)).FirstOrDefault();

            string TargetFilePath = BuildPath(TargetFolderPath, TargetFileName);

            // if the document exists under the root folder...
            if (dfi != null)
            {
                // if target path is different
                // otherwise no action is required: file name and hash are identical
                if (!dfi.FilePath.Equals(TargetFilePath))
                {
                    Console.WriteLine("Moving file to \"{0}\".", TargetFilePath);

                    // move the file and rename it
                    DocumentFile.MoveFile(dfi.FilePath, TargetFilePath);
                }
            }            
            else
            {
                try
                {
                    Console.WriteLine("Copying file to \"{0}\".", TargetFilePath);

                    // otherwise copy file from source (outside root folder)
                    DocumentFile.CopyFile(SourceFilePath, TargetFilePath);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("ERROR: Unable to copy the file \"{0}\" to \"{1}\". {2}", SourceFilePath, TargetFilePath, ex.Message);
                }
            }           
        }
    }
}
