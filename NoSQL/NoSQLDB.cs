using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using FLAccountDB.LoginDB;
using LogDispatcher;

namespace FLAccountDB.NoSQL
{



    public class NoSQLDB
    {

        private readonly SQLiteConnection _conn;
        public readonly DBQueue Queue;

        public readonly LoginDatabase LoginDB;
        public readonly BanDB Bans;
        public readonly string AccPath;
        public bool ClosePending;



        public event StateChange StateChanged;
        public delegate void StateChange(DBStates state);

        public readonly Scanner Scan;
        public readonly DBCrawler Retriever;
        #region "Database initiation"


        private const string CreateDBString = @"CREATE TABLE @Table(
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
         LastOnline DATETIME,
         OnLineTime INTEGER,
         IsAdmin INTEGER NOT NULL,
         IsBanned INTEGER NOT NULL
);";

        /// <summary>
        /// Initiate the legacy NoSQL Freelancer storage.
        /// </summary>
        /// <param name="dbPath">Path to the SQLite database file. DB will be created if file is nonexistent.</param>
        /// <param name="accPath">Path to accounts' directory.</param>
        /// <param name="log"></param>
        public NoSQLDB(string dbPath, string accPath, LogDispatcher.LogDispatcher log)
        {
            //One-shot event assignment; NoSQLDB-Retriever
            Logger.LogDisp = log;
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
                    try
                    {
                        _conn.Open();
                    }
                    catch (Exception e)
                    {
                        Logger.LogDisp.NewMessage(LogType.Fatal, "NoSQLDB: Can't connect to new player DB. Reason: " + e.Message);
                        throw;
                    }
 
                    // Create data base structure
                    var createDataBase = _conn.CreateCommand();    // Useful method
                createDataBase.CommandText = CreateDBString.Replace("@Table", "Accounts");
                createDataBase.ExecuteNonQuery();

                createDataBase.CommandText = CreateDBString.Replace("@Table", "DelAccounts");
                createDataBase.ExecuteNonQuery();


                    createDataBase.CommandText = @"CREATE TABLE LoginIP(
         AccID TEXT NOT NULL,
         IP TEXT NOT NULL,
         LogTime DATETIME NOT NULL,
         PRIMARY KEY (AccID, IP) ON CONFLICT REPLACE
);";
                    createDataBase.ExecuteNonQuery();

                    createDataBase.CommandText = @"CREATE TABLE LoginID(
         AccID TEXT NOT NULL,
         ID1 TEXT NOT NULL,
         ID2 TEXT NOT NULL
);";
                    createDataBase.ExecuteNonQuery();

                    createDataBase.CommandText = @"CREATE TABLE Bans(
         AccID TEXT NOT NULL PRIMARY KEY ON CONFLICT REPLACE,
         Reason TEXT NOT NULL,
         DateStarting DATETIME NOT NULL,
         DateFinishing DATETIME NOT NULL
);";
                    createDataBase.ExecuteNonQuery();

                    createDataBase.CommandText = "CREATE INDEX AccLookup ON LoginIP(AccID ASC);";
                    createDataBase.ExecuteNonQuery();

                    createDataBase.CommandText = "CREATE INDEX CharLookup ON Accounts(CharName ASC);";
                    createDataBase.ExecuteNonQuery();

                    _conn.Close();
                    Logger.LogDisp.NewMessage(LogType.Warning, "Created new player DB.");
                    
            }

            // Base created fo sho
            _conn = new SQLiteConnection();
            var cs = new SQLiteConnectionStringBuilder {DataSource = dbPath};
            _conn.ConnectionString = cs.ToString();
            _conn.Open();
            Logger.LogDisp.NewMessage(LogType.Info, "NoSQLDB: Connected.");


            Queue = new DBQueue(_conn,  "NoSQLDB.Main");

            LoginDB = new LoginDatabase( _conn, Queue);
            Bans = new BanDB(this,Queue);


            Scan = new Scanner(_conn,this);

            Retriever = new DBCrawler(_conn);

            Scan.StateChanged += Scan_StateChanged;

            if (StateChanged != null)
                StateChanged(DBStates.Ready);
        }

        void Scan_StateChanged(DBStates state)
        {
            if (StateChanged != null)
                StateChanged(state);
        }

        public void CloseDB()
        {
            ClosePending = true;
            
            Scan.Cancel();
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

        
        /// <summary>
        /// Removes account from metadata DB.
        /// </summary>
        /// <param name="accID"></param>
        /// <param name="charID"></param>
        public void RemoveAccountFromDB(string accID, string charID)
        {
            accID = EscapeString(accID);
            charID = EscapeString(charID);


            using (var cmd = new SQLiteCommand(
            "INSERT INTO DelAccounts SELECT * FROM Accounts WHERE AccID = @AccID And CharCode = @CharCode",
            _conn))
            {
                cmd.Parameters.AddWithValue("@AccID", accID);
                cmd.Parameters.AddWithValue("@CharCode", charID);
                Queue.Execute(cmd);
            } 

            using (var cmd = new SQLiteCommand(
                "DELETE FROM Accounts WHERE AccID = @AccID And CharCode = @CharCode",
                _conn))
            {
                cmd.Parameters.AddWithValue("@AccID", accID);
                cmd.Parameters.AddWithValue("@CharCode", charID);

                Queue.Execute(cmd);
            } 
        }

        /// <summary>
        /// Removes account from both metadata DB and game DB.
        /// </summary>
        /// <param name="charPath"></param>
        /// <param name="accID"></param>
        /// <param name="charID"></param>
        /// <returns></returns>
        public bool RemoveAccount(string charPath,string accID, string charID)
        {
            RemoveAccountFromDB(accID,charID);
            var path = Path.Combine(AccPath, charPath + @".fl");

            if (!File.Exists(path)) return false;

            File.Delete(path);
            return true;
        }
    }
}
