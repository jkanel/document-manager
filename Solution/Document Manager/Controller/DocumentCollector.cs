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

        private static string FileTypeExcludePattern = @"^\.({0-9}*|~*|_*|ini)$";
        private static string FileNameExcludePattern = @"^(~*)$";
        private Regex FileTypeExcludeRegex;
        private Regex FileNameExcludeRegex;

        private int FileCounter = 0;
        private int RootFileCounter = 0;


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

        /// <summary>
        /// Collect document information from the root folder path and contained folders.
        /// </summary>
        /// <param name="RootFolderPath"></param>
        /// <param name="Recursive"></param>
        /// <param name="IgnoreDeleteCheck"></param>
        public void CollectDocuments(string RootFolderPath, bool Recursive, bool IgnoreDeleteCheck)
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

            RootFileCounter = 0;
            foreach (string FolderPath in FolderPaths)
            {
                Console.WriteLine("Processing Path: {0}", FolderPath);
                ProcessFolder(FolderPath, IgnoreDeleteCheck);
            }

            // apply database save changes
            AppContext.SaveChanges();

            //  update the document table
            AppContext.RefreshDocuments();

            Console.WriteLine("Processed {0} files in \"{1}\".", RootFileCounter.ToString("#,##0"), RootFolderPath);
            Console.WriteLine("Processed {0} files so far.", FileCounter.ToString("#,##0"));
        }

        public void ProcessFolder(string FolderPath, bool IgnoreDeleteCheck)
        {
            // get current file paths in the folder
            // exclude temporary files
            string[] FilePaths = Directory.GetFiles(FolderPath, "*", SearchOption.TopDirectoryOnly);

            List<DocumentFileInfo> CollectedFileInfos = null;

            if (!IgnoreDeleteCheck)
            {

                //get past file paths from the folder that are not already deleted
                CollectedFileInfos = AppContext.DocumentFiles
                    .Where(f => f.DeletedFlag.Equals(DocumentFile.FALSE_FLAG_VALUE)
                        && f.FolderPath.Equals(FolderPath, StringComparison.CurrentCultureIgnoreCase))
                    .Select(x => new DocumentFileInfo { FilePath = x.FilePath, DocumentHash = x.DocumentHash })
                    .ToList<DocumentFileInfo>();

                string[] CollectedFilePaths = CollectedFileInfos.Select(x => x.FilePath).ToArray<string>();

                // get files  that were previously collected but are no longer in the file list
                string[] DeletedFilePaths = CollectedFileInfos
                    .Where(x => !FilePaths.Contains(x.FilePath, StringComparer.OrdinalIgnoreCase))
                    .Select(x => x.FilePath)
                    .ToArray<string>();

                if (DeletedFilePaths != null && DeletedFilePaths.Length > 0)
                {
                    ProcessFileDeletes(DeletedFilePaths);
                }
            }

            // process files
            foreach (string FilePath in FilePaths)
            {

                string DocumentHash = Document.GenerateFileHash(FilePath);

                // if null then process all file paths as new
                if (IgnoreDeleteCheck)
                {
                    ProcessFileInsert(FilePath, DocumentHash);
                    break;
                }

                // get the file information match based on file path
                DocumentFileInfo MatchFileInfo = CollectedFileInfos
                    .Where(x => x.FilePath.Equals(FilePath, StringComparison.CurrentCultureIgnoreCase))
                    .FirstOrDefault();
                
                // if a file match was not found, assume new insert
                if(MatchFileInfo == null)
                {
                    // insert the file
                    ProcessFileInsert(FilePath, DocumentHash);

                }
                // compare the hash to determine if update is required
                // otherwise do nothing
                else if(!MatchFileInfo.DocumentHash.Equals(DocumentHash))
                {
                    // update the file
                    ProcessFileUpdate(FilePath, DocumentHash);
                }

                // display message on the console
                FileCounter += 1;
                RootFileCounter += 1;

                if (FileCounter % 100 == 0)
                {
                    Console.WriteLine("Processed {0} files at {1}.", FileCounter.ToString("#,##0"), DateTime.Now.ToString("s"));
                }

            }

            // final save changes
            AppContext.SaveChanges();

        }

        public void ProcessFileDeletes(string[] DeletedFilePaths)
        {
           
            foreach(string DeletedFilePath in DeletedFilePaths)
            {
                DocumentFile DeletedFile = AppContext.DocumentFiles.Where(f => f.FilePath.Equals(DeletedFilePath)).SingleOrDefault<Model.DocumentFile>();
                if(DeletedFile != null) DeletedFile.Deleted = true;
            }
        }

        public void ProcessFileInsert(string FilePath, string DocumentHash)
        {

            // create a temp file object with file details
            Model.DocumentFile TempFile = new Model.DocumentFile(FilePath, DocumentHash);

            // skip file if...
            // temp file or not matching allowed file types
            if (TempFile.FileType.Length == 0 ||
                    this.FileTypeExcludeRegex.IsMatch(TempFile.FileType) ||
                    this.FileNameExcludeRegex.IsMatch(TempFile.FileType))
            {
                // skip the file
                return;
            }

            AppContext.DocumentFiles.Add(TempFile);

            // ProcessWords(TempFile);

            
        }

        public void ProcessFileUpdate(string FilePath, string DocumentHash)
        {
            DocumentFile df = AppContext.DocumentFiles.Where(x => x.FilePath.Equals(FilePath)).FirstOrDefault();

            if (df != null)
            {
                df.DocumentHash = DocumentHash;
                df.DeletedFlag = Document.FALSE_FLAG_VALUE;
                df.ModifyTimestamp = DateTime.Now;
            };

        }

       public void ProcessWords(string FilePath, string DocumentHash)
       {
            string FolderPath = DocumentFile.GetFileNameFromPath(FilePath);
            string FileName = DocumentFile.GetFolderFromPath(FilePath);

            // add words
            string[] Words = DocumentFile.ExtractDocumentWords(FolderPath, FileName);
            {
                foreach (string Word in Words)
                {
                    DocumentWord dw = new DocumentWord()
                    {
                        DocumentHash = DocumentHash,
                        Word = Word,
                        FilePath = FilePath
                    };

                    AppContext.DocumentWords.Add(dw);

                }
            }

        }

    }
}
