using System.IO;
using FLAccountDB.Data;
using FLAccountDB.NoSQL;

namespace FLAccountDB
{
    public static class MetadataExtensions
    {

        public static Character GetCharacter(this Metadata md, string pathToAccDir,LogDispatcher.LogDispatcher log)
        {
            var acc = AccountRetriever.GetAccount(Path.Combine(pathToAccDir, md.CharPath + ".fl"),log);
            //TODO: fix against FOS
            if (acc == null) return null;
            acc.IsBanned = File.Exists(Path.Combine(pathToAccDir, md.CharPath + "banned"));
            return acc;
        }

        public static bool SaveCharacter(this Character ch, string pathToAccDir, LogDispatcher.LogDispatcher log)
        {
            return AccountRetriever.SaveCharacter(ch,Path.Combine(pathToAccDir, ch.CharPath + ".fl"));
        }

        
    }
}
