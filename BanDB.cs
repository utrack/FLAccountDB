using System;
using System.Data.SQLite;
using System.IO;
using FLAccountDB.NoSQL;
using LogDispatcher;

namespace FLAccountDB
{
    public class BanDB
    {

        private NoSQLDB _db;
        private readonly DBQueue _queue;
        public BanDB(NoSQLDB db,DBQueue queue)
        {
            _db = db;
            _queue = queue;
        }
        private const string AddBanString = "INSERT INTO Bans "
+ "(AccID,Reason,DateStarting,DateFinishing) "
+ "VALUES(@AccID,@Reason,@DSt,@DFn);";
        public void AddBan(string accID, string reason,DateTime start, DateTime finish)
        {
            using (var comm = new SQLiteCommand(AddBanString, _queue.Conn))
            {
                comm.Parameters.AddWithValue("@AccID", accID);
                comm.Parameters.AddWithValue("@Reason", reason);
                comm.Parameters.AddWithValue("@DSt", start.ToUniversalTime());
                comm.Parameters.AddWithValue("@DFn", finish.ToUniversalTime());
                if (_queue != null)
                    _queue.Execute(comm);
            }
            Logger.LogDisp.NewMessage(LogType.Info,"Ban added: {0} from {1} to {2}",accID,start.Date,finish.Date);
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
