using System;
using System.IO;
using System.Linq;
using FLAccountDB.Data;
using FLAccountDB.NoSQL;

namespace FLAccountDB
{
    public static class MetadataExtensions
    {

        public static Character GetCharacter(this Metadata md, string pathToAccDir,LogDispatcher.LogDispatcher log)
        {
            var acc = AccountRetriever.GetAccount(Path.Combine(pathToAccDir, md.CharPath + ".fl"),log);
            //TODO: get it outta here, FOS
            acc.AdminRights = Scanner.IsAdmin(Path.Combine(pathToAccDir, md.AccountID));
            return acc;
        }

        public static bool SaveCharacter(this Character ch, string pathToAccDir, LogDispatcher.LogDispatcher log)
        {
            return AccountRetriever.SaveCharacter(ch,Path.Combine(pathToAccDir, ch.CharPath + ".fl"));
        }

        
    }
}
