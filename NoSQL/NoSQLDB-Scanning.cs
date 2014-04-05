using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogDispatcher;

namespace FLAccountDB.NoSQL
{

    public enum DBStates
    {
        Initiating,
        Updating,
        UpdatingFormFiles,
        Ready,
        Closed
    }

    public partial class NoSQLDB
    {

        


        private BackgroundWorker _bgwLoader;

        public event PercentageChanged ProgressChanged;
        public delegate void PercentageChanged(int percent,int qCount);

        public event StateChange StateChanged;
        public delegate void StateChange(DBStates state);

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
        /// <param name="aggressive">Use aggressive scan? Very CPU-hungry but about twice as fast.</param>
        public void LoadDB(bool aggressive = false)
        {
            if (IsJobRunning()) return;

            _bgwLoader = new BackgroundWorker
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = true
            };
            _bgwLoader.DoWork += _bgwLoader_DoWork;
            _bgwLoader.ProgressChanged += _bgwLoader_ProgressChanged;
            _bgwLoader.RunWorkerAsync(aggressive);
            _bgwLoader.RunWorkerCompleted += _bgwLoader_RunWorkerCompleted;
            
        }

        void _bgwLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _areReadyToClose.Reset();
            if (StateChanged != null)
                StateChanged(DBStates.Ready);

            if (e.Cancelled && _closePending)
                _areReadyToClose.Set();
            if (ProgressChanged != null)
                ProgressChanged(100, Queue.Count);
            

        }

        void _bgwLoader_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (ProgressChanged != null)
                ProgressChanged(e.ProgressPercentage,(int)e.UserState);
        }

        void _bgwLoader_DoWork(object sender, DoWorkEventArgs e)
        {

            if (StateChanged != null)
                StateChanged(DBStates.Initiating);

            var accDirs = new DirectoryInfo(AccPath).GetDirectories("??-????????").OrderByDescending(d => d.LastAccessTime);
            //Directory.GetDirectories(path, "??-????????").OrderByDescending(d => d.La);
            var i = 0;
            var count = accDirs.Count();
            if ((bool) e.Argument)
                Parallel.ForEach(accDirs, account =>
                {
                    if (_bgwLoader.CancellationPending)
                    {
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
                        e.Cancel = true;
                        return;
                    }
                    LoadAccountDirectory(acc.FullName);
                    _bgwLoader.ReportProgress(
                        (int)(
                        ((double)i / count)
                        *100
                        ),Queue.Count
                        );
                    i++;
                }
            }
                
                
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
            _bgwUpdater.RunWorkerAsync(lastModTime);
            _bgwUpdater.RunWorkerCompleted += _bgwUpdater_RunWorkerCompleted;

        }

        void _bgwUpdater_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _areReadyToClose.Reset();
            if (StateChanged != null)
                StateChanged(DBStates.Ready);
            
            if (e.Cancelled && _closePending)
                _areReadyToClose.Set();
            if (ProgressChanged != null)
                ProgressChanged(100, Queue.Count);
        }

        void _bgwUpdater_DoWork(object sender, DoWorkEventArgs e)
        {
            var lastModTime = (DateTime) e.Argument;
            var len = AccPath.Length + 12;
            if (StateChanged != null)
                StateChanged(DBStates.UpdatingFormFiles);
            // find all the newer savefiles, get the directory path, get unique directories
            // LINQ magic ;)
            var accDirs =
                new DirectoryInfo(AccPath).GetFiles("??-????????.fl", SearchOption.AllDirectories)
                    .Where(d => d.LastWriteTime > lastModTime)
                    .Select(w => w.FullName.Substring(0, len))
                    .Distinct();

            // add there all the directories whose content had changed (new\del accounts, bans etc)
            accDirs = accDirs.Union(
                new DirectoryInfo(AccPath).GetDirectories("??-????????")
                .Where(w => w.LastWriteTime > lastModTime)
                .Select(w => w.FullName));

            var enumerable = accDirs as IList<string> ?? accDirs.ToList();


            LogDispatcher.LogDispatcher.NewMessage(LogType.Info,
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
                    e.Cancel = true;
                    return;
                }
                LoadAccountDirectory(accDir);
                _bgwUpdater.ReportProgress(
                    (int)(
                    ((double)i / count)
                    * 100
                    ), Queue.Count
                    );
                i++;
            }
        }

        #endregion



        private void LoadAccountDirectory(string path)
        {
            //var accountID = AccountRetriever.GetAccountID(path);
            var accountID = path.Substring(path.Length - 11);
            var charFiles = Directory.GetFiles(path, "??-????????.fl");

            //remove the account dir if there's no charfiles
            if (charFiles.Length == 0) Directory.Delete(path, true);

            var dbChars = GetCharCodesByAccount(accountID);

            using (var comm = new SQLiteCommand(InsertText, Queue.Conn))
                foreach (var md in charFiles.Select(AccountRetriever.GetMeta).Where(md => md != null))
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
                    if (Queue != null)
                    Queue.Execute(comm);

                    dbChars.Remove(md.CharID);
                }

            if (dbChars.Count == 0) return;

            foreach (var acc in dbChars)
                RemoveAccountFromDB(accountID, acc);
        }


    }
}
