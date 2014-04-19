using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FLAccountDB.Data;
using LogDispatcher;

namespace FLAccountDB.NoSQL
{
    public class Scanner
    {

        public event PercentageChanged ProgressChanged;
        public delegate void PercentageChanged(int percent, int qCount);

        public event StateChange StateChanged;
        public delegate void StateChange(DBStates state);

        /// <summary>
        /// Used for player checking. Thrown every time the account needs to be checked
        /// Return null to re-read the char, e.Cancel = true to rescan it next time
        /// </summary>
        public event OnAccountScan AccountScanned;
        public delegate Character OnAccountScan(Character ch,CancelEventArgs e);

        private readonly SQLiteConnection _conn;
        private readonly NoSQLDB _db;
        public Scanner(SQLiteConnection conn,NoSQLDB db)
        {
            _conn = conn;
            _db = db;
        }


        private BackgroundWorker _bgwLoader;



        private readonly AutoResetEvent _areReadyToClose = new AutoResetEvent(false);


        private bool IsJobRunning()
        {
            if (_bgwLoader != null)
                if (_bgwLoader.IsBusy) return true;
            if (_bgwUpdater != null)
                if (_bgwUpdater.IsBusy) return true;
            return false;
        }

        #region "LoadDB"
        /// <summary>
        /// Rescans the whole FL directory to the AccDB.
        /// </summary>
        /// <param name="aggressive">Use aggressive scan? Very HDD-hungry but about twice as fast.</param>
        public void LoadDB(bool aggressive = false)
        {

            if (_bgwLoader != null)
                if (_bgwLoader.IsBusy)
                    return;



            _bgwLoader = new BackgroundWorker
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = true
            };
            _bgwLoader.DoWork += _bgwLoader_DoWork;
            _bgwLoader.ProgressChanged += _bgwLoader_ProgressChanged;
            _bgwLoader.RunWorkerCompleted += _bgwLoader_RunWorkerCompleted;
            _bgwLoader.RunWorkerAsync(aggressive);
            

        }

        void _bgwLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _areReadyToClose.Reset();

            if (e.Cancelled && _db.ClosePending)
            {
                _areReadyToClose.Set();
                return;
            }


            if (StateChanged != null)
                StateChanged(DBStates.Ready);


            if (ProgressChanged != null)
                ProgressChanged(100, _db.Queue.Count);


        }

        public void Cancel()
        {
            if (_bgwLoader != null)
                if (_bgwLoader.IsBusy)
                {
                    _bgwLoader.CancelAsync();
                    _areReadyToClose.WaitOne();
                }


            if (_bgwUpdater != null)
                if (_bgwUpdater.IsBusy)
                {
                    _bgwUpdater.CancelAsync();
                    _areReadyToClose.WaitOne();
                }
        }

        void _bgwLoader_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (ProgressChanged != null)
                ProgressChanged(e.ProgressPercentage, (int)e.UserState);
        }

        void _bgwLoader_DoWork(object sender, DoWorkEventArgs e)
        {

            if (_bgwUpdater != null)
                if (_bgwUpdater.IsBusy)
                {
                    _bgwUpdater.CancelAsync();
                    _areReadyToClose.WaitOne();
                }

            Logger.LogDisp.NewMessage(LogType.Info,"Started player DB initialization...");

            if (StateChanged != null)
                StateChanged(DBStates.Initiating);

            using (var cmd = new SQLiteCommand("DELETE FROM Accounts;", _conn))
                cmd.ExecuteNonQuery();

            var accDirs = new DirectoryInfo(_db.AccPath).GetDirectories("??-????????").OrderByDescending(d => d.LastAccessTime);
            //Directory.GetDirectories(path, "??-????????").OrderByDescending(d => d.La);
            var i = 0;
            var count = accDirs.Count();
            if ((bool)e.Argument)
                Parallel.ForEach(accDirs, account =>
                {
                    if (_bgwLoader.CancellationPending)
                    {
                        _areReadyToClose.Set();
                        e.Cancel = true;
                        return;
                    }
                    LoadAccountDirectory(account.FullName);
                    _bgwLoader.ReportProgress((i / count) * 100);
                    i++;
                }

                    );
            else
            {


                foreach (var acc in accDirs)
                {
                    if (_bgwLoader.CancellationPending)
                    {
                        _areReadyToClose.Set();
                        e.Cancel = true;
                        return;
                    }
                    LoadAccountDirectory(acc.FullName);
                    _bgwLoader.ReportProgress(
                        (int)(
                        ((double)i / count)
                        * 100
                        ), _db.Queue.Count
                        );
                    i++;
                }
            }

            Logger.LogDisp.NewMessage(LogType.Info, "Player DB initialized.");
        }
        #endregion

        private const string InsertText = "INSERT INTO Accounts "
    + "(CharPath,CharName,AccID,CharCode,Money,Rank,Ship,Location,Base,Equipment,Created,LastOnline) "
    + "VALUES(@CharPath,@CharName,@AccID,@CharCode,@Money,@Rank,@Ship,@Location,@Base,@Equipment,@Created,@LastOnline)";

        #region "UpdateDB"

        private BackgroundWorker _bgwUpdater;
        public void Update(DateTime lastModTime)
        {
            if (IsJobRunning()) return;

            _bgwUpdater = new BackgroundWorker
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = true
            };
            _bgwUpdater.DoWork += _bgwUpdater_DoWork;
            _bgwUpdater.ProgressChanged += _bgwLoader_ProgressChanged;
            _bgwUpdater.RunWorkerCompleted +=_bgwUpdater_RunWorkerCompleted;
            _bgwUpdater.RunWorkerAsync(lastModTime);
            

        }

        void _bgwUpdater_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _areReadyToClose.Reset();

            if (e.Cancelled && _db.ClosePending)
            {
                _areReadyToClose.Set();
                return;
            }
                

            if (StateChanged != null)
                StateChanged(DBStates.Ready);

            
            if (ProgressChanged != null)
                ProgressChanged(100, _db.Queue.Count);
        }

        void _bgwUpdater_DoWork(object sender, DoWorkEventArgs e)
        {
            Logger.LogDisp.NewMessage(LogType.Info, "Started player DB update...");
            var lastModTime = (DateTime)e.Argument;
            var len = _db.AccPath.Length + 12;
            if (StateChanged != null)
                StateChanged(DBStates.UpdatingFormFiles);
            // find all the newer savefiles, get the directory path, get unique directories
            // LINQ magic ;)
            var accDirs =
                new DirectoryInfo(_db.AccPath).GetFiles("??-????????.fl", SearchOption.AllDirectories)
                    .Where(d => d.LastWriteTime > lastModTime)
                    .Select(w => w.FullName.Substring(0, len))
                    .Distinct();

            // add there all the directories whose content had changed (new\del accounts, bans etc)
            accDirs = accDirs.Union(
                new DirectoryInfo(_db.AccPath).GetDirectories("??-????????")
                .Where(w => w.LastWriteTime > lastModTime)
                .Select(w => w.FullName));

            var enumerable = accDirs as IList<string> ?? accDirs.ToList();


            Logger.LogDisp.NewMessage(LogType.Info,
                "Update: found " + enumerable.Count() + " changed accounts.");

            if (StateChanged != null)
                StateChanged(DBStates.Updating);

            var i = 0;
            var count = enumerable.Count;


            // rescan stuff
            foreach (var accDir in enumerable)
            {
                if (_bgwUpdater.CancellationPending)
                {
                    Logger.LogDisp.NewMessage(LogType.Info,"Update aborted.");
                    _areReadyToClose.Reset();
                    _areReadyToClose.Set();
                    e.Cancel = true;
                    return;
                }
                LoadAccountDirectory(accDir);
                _bgwUpdater.ReportProgress(
                    (int)(
                    ((double)i / count)
                    * 100
                    ), _db.Queue.Count
                    );
                i++;
            }
            Logger.LogDisp.NewMessage(LogType.Info, "Player DB update finished.");
        }

        #endregion

        private List<string> GetCharCodesByAccount(string accID)
        {
            accID = NoSQLDB.EscapeString(accID);

            var str = new List<string>();
            using (var cmd = new SQLiteCommand(_conn))
            {
                cmd.CommandText = String.Format("SELECT * FROM Accounts WHERE AccID = '{0}'",accID);
                //cmd.Parameters.AddWithValue("@AccID", accID);
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        str.Add(rdr.GetString(0));
            }

            return str;
        }

        public void LoadAccountDirectory(string path)
        {
            //var accountID = AccountRetriever.GetAccountID(path);
            var accountID = path.Substring(path.Length - 11);
            var charFiles = Directory.GetFiles(path, "??-????????.fl");

            var dbChars = GetCharCodesByAccount(accountID);

            //remove the account dir if there's no charfiles
            if (charFiles.Length == 0)
            {
                Directory.Delete(path, true);
                foreach (var acc in dbChars)
                    _db.RemoveAccountFromDB(accountID, acc.Substring(12));
                return;
            }

            var loginCreds = GetAccIPs(path);

            foreach (var cred in loginCreds)
            {
                _db.LoginDB.AddIP(accountID, cred.Item1, cred.Item2);
                _db.LoginDB.AddIds(accountID, cred.Item3, cred.Item4);
            }

 
                foreach (var md in charFiles.Select(w => AccountRetriever.GetAccount(w,Logger.LogDisp)).Where(md => md != null))
                {
                    var args = new CancelEventArgs();
                    var mdNew = md;
                    if (AccountScanned != null)
                    {
                        //TODO: ugly,ugly, readonly foreach
                        mdNew = AccountScanned(md, args);

                        //while (mdNew == null)
                        //{
                            //mdNew = AccountRetriever.GetMeta(path + @"\" + md.CharID + ".fl");
                            //mdNew = AccountScanned(mdNew, args);
                        //}

                        if (args.Cancel)
                            continue;
                    }
                    
                    AddMetadata(mdNew,accountID);

                    dbChars.Remove(md.CharPath);
                }

            if (dbChars.Count == 0) return;


            foreach (var acc in dbChars)
                _db.RemoveAccountFromDB(accountID, acc.Substring(12));
        }


        private void AddMetadata(Character md, string accountID)
        {
            using (var comm = new SQLiteCommand(InsertText, _db.Queue.Conn))
            {
                comm.Parameters.AddWithValue("@CharPath", md.CharPath);
                comm.Parameters.AddWithValue("@CharName", md.Name);
                comm.Parameters.AddWithValue("@AccID", accountID);
                comm.Parameters.AddWithValue("@CharCode", md.CharID);
                comm.Parameters.AddWithValue("@Money", md.Money);
                comm.Parameters.AddWithValue("@Rank", md.Rank);
                comm.Parameters.AddWithValue("@Ship", md.ShipArch);
                comm.Parameters.AddWithValue("@Location", md.System);
                comm.Parameters.AddWithValue("@Base", md.Base);
                comm.Parameters.AddWithValue("@Equipment", md.Equipment);
                comm.Parameters.AddWithValue("@Created", DateTime.Now);
                comm.Parameters.AddWithValue("@LastOnline", md.LastOnline);
                if (_db.Queue != null)
                    _db.Queue.Execute(comm);
            }
            
        }

        /// <summary>
        /// Get login info for the file account.
        /// </summary>
        /// <param name="accPath">Full account path</param>
        /// <returns>List of IP,AccessTime,ID1,ID2.</returns>
        private static IEnumerable<Tuple<string, DateTime, string, string>> GetAccIPs(string accPath)
        {
            var loginFiles = Directory.GetFiles(accPath, "login_*.ini");
            var ret = new List<Tuple<string, DateTime, string, string>>();
            foreach (var loginFilePath in loginFiles)
            {
                // format: time=1347057642 id=3A44AC9A ip=13.33.33.37 id2=2A7A4F74
                var content = File.ReadAllText(loginFilePath);
                var loginID = "";
                var loginID2 = "";
                var ip = "";

                var values = content.Split(new[] { '\t', ' ' });
                foreach (var parts in values.Select(raw => raw.Split('=')))
                {
                    //string key = parts[0];
                    //string value = parts[1];

                    switch (parts[0])
                    {
                        case "id":
                            loginID += parts[1].Trim();
                            break;
                        case "id2":
                            loginID2 += parts[1].Trim();
                            break;
                        case "ip":
                            ip += parts[1].Trim();
                            break;

                    }
                }

                var accessTime = File.GetLastWriteTime(loginFilePath);
                ret.Add(
                    new Tuple<string, DateTime, string, string>
                        (ip, accessTime, loginID, loginID2)
                        );
                //TODO: should we?
                File.Delete(loginFilePath);
            }
            return ret;
        }

    }
}
