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

            string RootFolderPath = @"C:\Users\jeff.kanel\Temporary\Data and Analytics Repository";

            new DocumentCollector(AppContext).CollectDocuments(RootFolderPath, true);

            Console.Write("Document collection completed.");
            Console.Out.Flush();
            string wait = Console.ReadLine();
        }
        
    }
}
