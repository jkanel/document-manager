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
        /// <param name="FilterTargetFolderBranch">Filter of the target branch applied to documents that are published.</param>
        public void Publish(string FilterTargetFolderBranch)
        {
            // build the target root folder
            string TargetRootFolderPath = BuildPath(this.TargetRootFolder, FilterTargetFolderBranch);
            DocumentFile.AssertFolderExists(TargetRootFolderPath);

            // get file info under the target root
            this.FileInfoStore = GetFileInfoStoreFromPath(TargetRootFolderPath, true);

            if (FilterTargetFolderBranch == null) FilterTargetFolderBranch = "";

            // get target folders to publish
            var TargetFolderDetails = AppContext.Documents
                .Where(d =>
                    d.IgnoreFlag == Document.FALSE_FLAG_VALUE
                    && d.TargetFolderBranch.StartsWith(FilterTargetFolderBranch))
                .Select(d => new
                {
                    TargetFolderBranch = d.TargetFolderBranch,
                    Scope = d.Scope,
                    Client = d.Client,
                    Project = d.Project
                }).Distinct().ToList();


            // loop through folders
            foreach(var TargetFolderDetail in TargetFolderDetails)
            {
                // generate target path
                string TargetFolderPath = Document.BuildFilePath(
                    this.TargetRootFolder,
                    TargetFolderDetail.TargetFolderBranch,
                    TargetFolderDetail.Scope,
                    TargetFolderDetail.Client,
                    TargetFolderDetail.Project);

                PublishFolder(TargetFolderPath, TargetFolderDetail.TargetFolderBranch);
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

        private static List<DocumentFileInfo> GetFileInfoStoreFromPath(string RootFolderPath, bool Recursive)
        {
            string[] FilePaths;

            if (Recursive)
            {
                FilePaths = Directory.GetFiles(RootFolderPath, "*", SearchOption.AllDirectories);
            }
            else
            {
                FilePaths = Directory.GetFiles(RootFolderPath, "*", SearchOption.TopDirectoryOnly);
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

            // create the target folder if it does not exist
            //DocumentFile.AssertFolderExists(TargetFolderPath);
            //AppContext.Database.SqlQuery<>
            //// get all documents in the target folder branch
            //var DocumentDetailList = AppContext.DocumentFiles
            //    .Where(d => d.DeletedFlag.Equals(DocumentFile.FALSE_FLAG_VALUE)
            //        && d.Document.IgnoreFlag.Equals(Document.FALSE_FLAG_VALUE)
            //        && d.Document.TargetFolderBranch.Equals(TargetFolderBranch))
            //    .FirstOrDefault(x => x.DocumentHash)
            //    .Select(y => y.OrderBy(a => a.FilePath))
            //    .Select(z => new {
            //        SourceFilePath = z.FilePath,
            //        DocumentHash = z.DocumentHash,
            //        TargetFileName = z.Document.TargetFileName }).ToList();

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
                if (!dfi.FilePath.Equals(TargetFilePath))
                {
                    // move the file and rename it
                    DocumentFile.MoveFile(dfi.FilePath, TargetFilePath);
                }
            }            
            else
            {
                try
                {
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
