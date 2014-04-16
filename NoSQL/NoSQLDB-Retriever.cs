using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using LogDispatcher;

namespace FLAccountDB.NoSQL
{

    public partial class NoSQLDB
    {

        /// <summary>
        /// Executes the issued command and returns list of resulting acc metadata.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public List<Metadata> GetMeta(string command)
        {
            _log.NewMessage(LogType.Debug, "GetMeta Query: {0}",command);
            if (_conn == null) return null;
            if (_conn.State == ConnectionState.Closed) return null;
            var ret = new List<Metadata>();
            using (var cmd = new SQLiteCommand(command, _conn))
            {
                cmd.CommandText = command;
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var md = new Metadata();
                    //{
                    md.CharPath = reader.GetString(0);
                    md.Name = reader.GetString(1);
                    md.AccountID = reader.GetString(2);
                    md.CharID = reader.GetString(3);
                    md.Money = (uint) reader.GetInt32(4);
                    md.Rank = (byte) reader.GetInt32(5);
                    md.ShipArch = (uint) reader.GetInt32(6);
                    md.System = reader.GetString(7);
                    md.Base = reader.GetValue(8).ToString();
                    md.Equipment = reader.GetString(9);
                    md.LastOnline = reader.GetDateTime(11);
                    //};

                    ret.Add(md);
                }

            }

            return ret;
        }

        public event RequestReady OnGetFinish;
        public event EventHandler OnGetFinishWindow;
        public delegate void RequestReady(List<Metadata> meta); 
        public int GetScalar(string command)
        {
            _log.NewMessage(LogType.Debug, "GetScalar Query: {0}", command);
            int count;
            using (var cmd = new SQLiteCommand(_conn))
            {
                cmd.CommandText = command;
                cmd.CommandType = CommandType.Text;


                count = Convert.ToInt32(cmd.ExecuteScalar());
            }
            return count;
        }


        private const string SelectGroupByNames = "SELECT * FROM Accounts WHERE CharName IN (@CharNames)";
        private const string SelectGroupByName = "SELECT * FROM Accounts WHERE CharName LIKE '%@CharName%'";
        private const string SelectGroupByAccount = "SELECT * FROM Accounts WHERE AccID = '@AccID'";
        private const string SelectGroupBySystem = "SELECT * FROM Accounts WHERE Location LIKE '%@System%'";
        private const string SelectGroupByItem = "SELECT * FROM Accounts WHERE Equipment LIKE '%@Equip%'";
        private const string SelectGroupByCharCode = "SELECT * FROM Accounts WHERE CharCode = '@CharCode'";


        public void GetAccountChars(string accID)
        {
            accID = EscapeString(accID);
            var bgw = new BackgroundWorker();
            bgw.RunWorkerCompleted += _bgw_RunWorkerCompleted;
            bgw.DoWork += _bgw_DoWork;
            bgw.RunWorkerAsync(SelectGroupByAccount.Replace("@AccID", accID));
            //return GetMeta();
        }

        void _bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = GetMeta((string) e.Argument);
        }

        void _bgw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (OnGetFinish != null)
                OnGetFinish((List<Metadata>)e.Result);

            if (OnGetFinishWindow != null)
                OnGetFinishWindow(null, null);
        }

        public void GetMetasByItem(uint hash)
        {
            var bgw = new BackgroundWorker();
            bgw.RunWorkerCompleted += _bgw_RunWorkerCompleted;
            bgw.DoWork += _bgw_DoWork;
            bgw.RunWorkerAsync(
                SelectGroupByItem.Replace("@Equip", hash.ToString(CultureInfo.InvariantCulture))
                );
        }

        public void GetMetasByNames(List<string> names)
        {
            var str = SelectGroupByNames.
                Replace("@CharNames",
                    string.Join(
                        ",",
                        names.Select(
                            w =>
                                "'" + EscapeString(w) + "'")
                        )
                );

            var bgw = new BackgroundWorker();
            bgw.RunWorkerCompleted += _bgw_RunWorkerCompleted;
            bgw.DoWork += _bgw_DoWork;
            bgw.RunWorkerAsync(str);
        }

        public void GetMetasByName(string name)
        {
            name = EscapeString(name);
            var bgw = new BackgroundWorker();
            bgw.RunWorkerCompleted += _bgw_RunWorkerCompleted;
            bgw.DoWork += _bgw_DoWork;
            bgw.RunWorkerAsync(SelectGroupByName.Replace("@CharName", name));
            //return GetMeta();
        }

        public void GetMetasByCharID(string name)
        {
            name = EscapeString(name);
            var bgw = new BackgroundWorker();
            bgw.RunWorkerCompleted += _bgw_RunWorkerCompleted;
            bgw.DoWork += _bgw_DoWork;
            bgw.RunWorkerAsync(SelectGroupByCharCode.Replace("@CharCode", name));
            //return GetMeta();
        }

        public void GetMetasBySystem(string system)
        {
            system = EscapeString(system);
            var bgw = new BackgroundWorker();
            bgw.RunWorkerCompleted += _bgw_RunWorkerCompleted;
            bgw.DoWork += _bgw_DoWork;
            bgw.RunWorkerAsync(SelectGroupBySystem.Replace("@System", system));
        }

        public int CountRows(string table)
        {
            return GetScalar("select count(CharName) from '" + table + "';");
        }

    }



}
