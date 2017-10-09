using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileManager.Data;
using FileManager.Controller;

namespace FileManager
{
    class Program
    {
        static void Main(string[] args)
        {
            string ConnectionString = @"Server=localhost;Database=DocumentManager;Trusted_Connection=True;";
            ApplicationContext AppContext = new ApplicationContext(ConnectionString);

            // collecting document information
            string[] RootFolderPaths = new string[] {
                //@"E:\C1GD",
                //@"E:\C1GD2",
                //@"E:\CGD",
                //@"E:\CL1",
                //@"E:\CL2",
                @"C:\Users\jeff.kanel\Temporary\Data and Analytics Repository"
            };
            Collect(RootFolderPaths, AppContext);

            // extract words
            string ParentFolderPath = @"C:\Users\jeff.kanel\Temporary\Data and Analytics Repository";
            //ExtractWords(ParentFolderPath, AppContext);


            // publishing document information
            string TargetRootFolderPath = @"C:\Temporary\Target";
            string FilterFolderBranch = @"\Artifacts\Projects\Fidelity";
            //Publish(TargetRootFolderPath, FilterFolderBranch, AppContext);


            // prompt the console
            Console.WriteLine("PRESS ANY KEY TO FINISH");
            Console.Out.Flush();
            string wait = Console.ReadLine();

        }
        /// <summary>
        /// Collects and stores information about documents under the root folder paths.
        /// </summary>
        /// <param name="RootFolderPaths">String array of folder paths.</param>
        /// <param name="AppContext">Application context.</param>
        static void Collect(string[] RootFolderPaths, ApplicationContext AppContext)
        {
            DocumentCollector dc = new DocumentCollector(AppContext);

            foreach (string path in RootFolderPaths)
            {
                bool IgnoreDeleteCheck = false;

                dc.CollectDocuments(path, true, IgnoreDeleteCheck);
                Console.WriteLine("Completed collection for \"{0}\".", path);
            }

            Console.WriteLine("Document collection completed.");
            
        }

        /// <summary>
        /// Extracts words from the folder path and file name
        /// </summary>
        /// <param name="ParentFolderPath">Folder path beneath which all files are included for word extraction</param>
        /// <param name="AppContext">Application context.</param>
        static void ExtractWords(string ParentFolderPath, ApplicationContext AppContext)
        {
            AppContext.TruncateDocumentWords();

            List<DocumentFileInfo> dfs = AppContext.DocumentFiles
                .Where(x => x.FilePath.StartsWith(ParentFolderPath))
                .Select(x => new DocumentFileInfo { FilePath = x.FilePath, DocumentHash = x.DocumentHash })
                .ToList<DocumentFileInfo>();

            DocumentCollector dc = new DocumentCollector(AppContext);
            int WordFileCounter = 0;

            foreach (DocumentFileInfo df in dfs)
            {
                dc.ProcessWords(df.FilePath, df.DocumentHash);
                WordFileCounter += 1;
                
                if(WordFileCounter % 100 == 0)
                {
                    AppContext.SaveChanges();
                }

                // Console.WriteLine("Words extracted for \"{0}\".", df.FilePath);
            }

            // final database save
            AppContext.SaveChanges();

            Console.WriteLine("Document word extraction completed.");

        }

        /// <summary>
        /// Publishes all files under the filter branch into the target folder path.
        /// </summary>
        /// <param name="TargetRootFolderPath">Path declared as the root, under which branch folders are maintained.</param>
        /// <param name="AppContext">Application context.</param>
        static void Publish(string TargetRootFolderPath, string FilterFolderBranch, ApplicationContext AppContext)
        {
            DocumentPublisher dp = new DocumentPublisher(AppContext, TargetRootFolderPath);

            dp.Publish(FilterFolderBranch);

            Console.WriteLine("Document publishing completed.");
        }

    }
}
