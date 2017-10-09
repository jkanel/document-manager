using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileManager.Model
{
    public class DocumentWord
    {
        public static string RegexWordPattern = @"[^\W\d](\w|[-']{1,2}(?=\w))*";

        public static string[] StopWords = new string[]
        {
            "the", "and", "v"
        };

        [Key,DatabaseGenerated(DatabaseGeneratedOption.Identity), Required]
        public int DocumentWordId { get; set; }

        [Required, MaxLength(4000)]
        public string FilePath { get; set; }

        [Required, MaxLength(200)]
        public string Word { get; set; }

        [Required,MaxLength(200)]
        public string DocumentHash { get; set; }

        public static string[] ExtractPathWords(string Path)
        {
            return Path.Split('\\');
        }
        public static string[] ExtractIndividualWords(string Phrase)
        {
            
            Regex r = new Regex(DocumentWord.RegexWordPattern, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            MatchCollection matches = r.Matches(Phrase);

            string[] filewords = matches.Cast<Match>()
                .Where(x => !DocumentWord.StopWords.Contains(x.Value, StringComparer.OrdinalIgnoreCase))
                .Select(a => a.Value).ToArray<string>();

            return filewords;
        }
    }
}
