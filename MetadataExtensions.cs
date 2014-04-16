using System.IO;
using FLAccountDB.NoSQL;

namespace FLAccountDB
{
    public static class MetadataExtensions
    {

        public static Character GetCharacter(this Metadata md, string pathToAccDir)
        {
            return AccountRetriever.GetAccount(Path.Combine(pathToAccDir, md.CharPath + ".fl"));
        }


        public static bool SaveCharacter(this Character ch, string pathToAccDir, LogDispatcher.LogDispatcher log)
        {
            return AccountRetriever.SaveCharacter(ch,Path.Combine(pathToAccDir, ch.CharPath + ".fl"),log);
        }

        
    }
}
