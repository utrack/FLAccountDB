using System.IO;
using FLAccountDB.NoSQL;

namespace FLAccountDB
{
    public class BanDB
    {

        private readonly NoSQLDB _db;
        public BanDB(NoSQLDB db)
        {
            _db = db;
        }


        public string GetAccBanReason(string accID)
        {
            var path = Path.Combine(_db.AccPath, accID, @"banned");
            return !File.Exists(path) ? "" : File.ReadAllText(path);
        }

        public void AccountBan(string accID, string reason)
        {
            var path = Path.Combine(_db.AccPath, accID, @"banned");
            if (File.Exists(path))
                File.Delete(path);

            File.WriteAllText(path,reason);
            _db.Scan.LoadAccountDirectory(Path.Combine(_db.AccPath, accID));
        }

        public void AccountUnban(string accID)
        {
            var path = Path.Combine(_db.AccPath, accID, @"banned");
            if (File.Exists(path))
                File.Delete(path);
            _db.Scan.LoadAccountDirectory(Path.Combine(_db.AccPath, accID));
        }
    }
}
