using System.IO;
using FLAccountDB.NoSQL;

namespace FLAccountDB
{
    static class MetadataExtensions
    {

        public static Character GetCharacter(this Metadata md, string pathToAccDir)
        {
            return AccountRetriever.GetAccount(Path.Combine(pathToAccDir, md.CharPath));
        }


        
    }
}
