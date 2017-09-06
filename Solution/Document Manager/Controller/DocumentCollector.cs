using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using FileManager.Model;
using FileManager.Data;

namespace FileManager.Controller
{

    class DocumentFileInfo
    {
        public string FilePath { get; set; }
        public string DocumentHash { get; set; }
    }

    public class DocumentCollector
    {

        private ApplicationContext AppContext;

        private static string FileTypeExcludePattern = @"^\.({0-9}*|~*|_*)$";
        private static string FileNameExcludePattern = @"^(~*)$";
        private Regex FileTypeExcludeRegex;
        private Regex FileNameExcludeRegex;
        private List<DocumentFileInfo> FileInfoStore;
        private List<string> DocumentHashStore;

        private int FileCounter = 0;


        public DocumentCollector()
        {
            PrepareRegex();
            
        }

        public DocumentCollector(ApplicationContext context)
        {
            this.AppContext = context;
            PrepareRegex();
        }

        private void PrepareRegex()
        {
            FileTypeExcludeRegex = new Regex(DocumentCollector.FileTypeExcludePattern, RegexOptions.IgnoreCase);
            FileNameExcludeRegex = new Regex(DocumentCollector.FileNameExcludePattern, RegexOptions.IgnoreCase);
        }

        public void CollectDocuments(string RootFolderPath, bool Recursive)
        {
            string[] FolderPaths = null;

            if (Recursive)
            {
                SearchOption sopt = (Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                FolderPaths = Directory.GetDirectories(RootFolderPath, "*", sopt);
            }
            else
            {
                FolderPaths = new string[] { RootFolderPath };
            }

            Console.WriteLine("Creating stores at {0}.", DateTime.Now.ToString("s"));
            DocumentHashStore = AppContext.Documents.Select(x => x.DocumentHash).ToList<string>();

            Console.WriteLine("Document store created with {0} documents at {1}.",
                DocumentHashStore.Count.ToString("#,##0"), DateTime.Now.ToString("s"));

            FileInfoStore = AppContext.DocumentFiles
                .Select(x => new DocumentFileInfo() { FilePath = x.FilePath, DocumentHash = x.DocumentHash })
                .ToList<DocumentFileInfo>();

            Console.WriteLine("File Info store created with {0} files at {1}.",
                FileInfoStore.Count.ToString("#,##0"), DateTime.Now.ToString("s"));

            foreach (string FolderPath in FolderPaths)
            {
                ProcessFolder(FolderPath);
            }

            Console.WriteLine("Processed {0} files in TOTAL.", FileCounter.ToString("#,##0"));
        }

        public void ProcessFolder(string FolderPath)
        {
            // get current file paths in the folder
            // exclude temporary files
            string[] FilePaths = Directory.GetFiles(FolderPath, "*", SearchOption.TopDirectoryOnly);

            // get past file paths from the folder that are not already deleted
            string[] PastFilePaths = AppContext.DocumentFiles
                .Where(f => f.DeletedFlag.Equals(DocumentFile.FALSE_FLAG_VALUE) 
                    && f.FolderPath.Equals(FolderPath, StringComparison.CurrentCultureIgnoreCase))
                .Select(x => x.FilePath).ToArray<string>();

            // get paths in past files but not current files
            string[] DeletedFilePaths = PastFilePaths.Except(FilePaths).ToArray<string>();
            ProcessDeletedFiles(DeletedFilePaths);
            
            // process files files
            foreach (string FilePath in FilePaths)
            {
                ProcessFile(FilePath);
            }

        }

        public void ProcessDeletedFiles(string[] DeletedFilePaths)
        {

            // skip processing if no deleted file paths are presented
            if (DeletedFilePaths == null || DeletedFilePaths.Length == 0) return;
            
            Model.DocumentFile DeletedFile = null;

            foreach(string DeletedFilePath in DeletedFilePaths)
            {
                DeletedFile = AppContext.DocumentFiles.Where(f => f.FilePath.Equals(DeletedFilePath)).SingleOrDefault<Model.DocumentFile>();
                DeletedFile.Deleted = true;
            }

            AppContext.SaveChanges();
        }

        public void ProcessFile(string FilePath)
        {
            // create a temp file object with file details
            Model.DocumentFile TempFile = new Model.DocumentFile(FilePath);
            
            // skip file if...
            // temp file or not matching allowed file types
            if (TempFile.FileType.Length == 0 || 
                    this.FileTypeExcludeRegex.IsMatch(TempFile.FileType) ||
                    this.FileNameExcludeRegex.IsMatch(TempFile.FileType))
            {
                // skip the file
                return;
            }

            FileCounter += 1;
            if (FileCounter % 100 == 0)
            {
                Console.WriteLine("Processed {0} files at {1}.", FileCounter.ToString("#,##0"), DateTime.Now.ToString("s"));
            }

            // create the document if it does not exist
            // if (!AppContext.Documents.Any(d => d.DocumentHash.Equals(TempFile.DocumentHash)))
            if (!DocumentHashStore.Contains(TempFile.DocumentHash))
            {
                Document doc = new Document()
                {
                    DocumentHash = TempFile.DocumentHash,
                    TargetFileName = TempFile.FileName,
                    OriginalFileName = TempFile.FileName,
                    OriginalFolderPath = TempFile.FolderPath
                };

                AppContext.Documents.Add(doc);
                AppContext.SaveChanges();

                // add the new document hash to the store
                DocumentHashStore.Add(TempFile.DocumentHash);

            }


            //create the TempFile if it does not exist
            if (!FileInfoStore.Any(fi => fi.FilePath.Equals(FilePath)))
            {
                AppContext.DocumentFiles.Add(TempFile);
                AppContext.SaveChanges();

                // add the new file info to the store
                FileInfoStore.Add(new DocumentFileInfo() { FilePath = FilePath, DocumentHash = TempFile.DocumentHash });
            }

            else
            {
                // get the relevant file info
                DocumentFileInfo FileInfo = FileInfoStore.Where(fi => fi.FilePath.Equals(FilePath)).SingleOrDefault();

                // update the document hash if it has changed
                if (!FileInfo.DocumentHash.Equals(TempFile.DocumentHash))
                {

                    DocumentFile StoredDocumentFile = AppContext.DocumentFiles
                        .Where(x => x.FilePath.Equals(FilePath, StringComparison.CurrentCultureIgnoreCase)).SingleOrDefault();

                    StoredDocumentFile.DocumentHash = TempFile.DocumentHash;
                    AppContext.SaveChanges();

                    // update store to reflect the new document hash
                    FileInfo.DocumentHash = TempFile.DocumentHash;
                }

            }

        }

    }
}
