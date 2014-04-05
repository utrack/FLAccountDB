using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using LogDispatcher;

namespace FLAccountDB.NoSQL
{
    public partial class NoSQLDB
    {

        private readonly SQLiteConnection _conn;
        public readonly DBQueue Queue;
        public readonly string AccPath;
        private bool _closePending;
        #region "Database initiation"
        /// <summary>
        /// Initiate the legacy NoSQL Freelancer storage.
        /// </summary>
        /// <param name="dbPath">Path to the SQLite database file. DB will be created if file is nonexistent.</param>
        /// <param name="accPath">Path to accounts' directory.</param>
        public NoSQLDB(string dbPath, string accPath)
        {
            //Retriever = new MetaRetriever(this);
            AccPath = accPath;

            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);

                _conn = new SQLiteConnection();
                var conString = new SQLiteConnectionStringBuilder
                {
                    DataSource = dbPath
                };
                _conn.ConnectionString = conString.ToString();
                using (_conn)
                    {
                    try
                    {
                        _conn.Open();
                    }
                    catch (Exception e)
                    {
                        LogDispatcher.LogDispatcher.NewMessage(LogType.Fatal, "Can't connect to new data base. Reason: " + e.Message);
                        throw;
                    }
 
                    // Create data base structure
                    var createDataBase = _conn.CreateCommand();    // Useful method
                        createDataBase.CommandText = @"CREATE TABLE Accounts(
         CharPath TEXT PRIMARY KEY ON CONFLICT REPLACE,
         CharName TEXT NOT NULL,
         AccID TEXT NOT NULL,
         CharCode TEXT NOT NULL,
         Money INTEGER NOT NULL,
         Rank INTEGER,
         Ship INTEGER,
         Location TEXT NOT NULL,
         Base TEXT,
         Equipment TEXT,
         Created DATETIME,
         LastOnline DATETIME
);";
                    createDataBase.ExecuteNonQuery();
                    createDataBase.CommandText = "CREATE INDEX CharLookup ON Accounts(CharName ASC)";
                    createDataBase.ExecuteNonQuery();
                    LogDispatcher.LogDispatcher.NewMessage(LogType.Info,"Created new database");
                    }
            }

            // Base created fo sho
            _conn = new SQLiteConnection();
            var cs = new SQLiteConnectionStringBuilder {DataSource = dbPath};
            _conn.ConnectionString = cs.ToString();
            _conn.Open();
            Queue = new DBQueue(_conn);
        }

        public void CloseDB()
        {
            _closePending = true;
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
                    

            
            Queue.Force();
            if (_conn.State == ConnectionState.Open)
                _conn.Close();
            if (StateChanged != null)
                StateChanged(DBStates.Closed);
            
        }


        public bool IsReady()
        {
            return (_conn.State != ConnectionState.Closed) && (_conn.State != ConnectionState.Broken);
        }

        #endregion


        public static string EscapeString(string str)
        {
            return str.Replace("'", "''");
        }

        private List<string> GetCharCodesByAccount(string accID)
        {
            accID = EscapeString(accID);

            var str = new List<string>();
            using (var cmd = new SQLiteCommand(
                "SELECT CharCode FROM Accounts WHERE AccID = '@AccID'",
                _conn))
            {
                cmd.Parameters.AddWithValue("@AccID", accID);
                //TODO: ctor somewhere there is really CPU hungary. The whole method in fact
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        str.Add(rdr.GetString(0));
            }
                
            return str;
        }

        private void RemoveAccountFromDB(string accID, string charID)
        {
            accID = EscapeString(accID);
            charID = EscapeString(charID);
            using (var cmd = new SQLiteCommand(
                "DELETE FROM Accounts WHERE AccID = @AccID And CharCode = @CharCode",
                _conn))
            {
                cmd.Parameters.AddWithValue("@AccID", accID);
                cmd.Parameters.AddWithValue("@CharCode", charID);
                Queue.Execute(cmd);
            }
            
        }

        public bool RemoveAccount(Metadata md)
        {
            RemoveAccountFromDB(md.AccountID,md.CharID);
            var path = Path.Combine(AccPath, md.CharPath);

            if (!File.Exists(path)) return false;

            File.Delete(path);
            return true;
        }


        

        #region "Properties"
        public int PendingWrites
        {
            get { return Queue.Count; }
        }
        #endregion
    }
}
