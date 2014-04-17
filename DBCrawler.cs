using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using FLAccountDB.Data;
using FLAccountDB.LoginDB;
using FLAccountDB.NoSQL;
using LogDispatcher;

namespace FLAccountDB
{
    public class DBCrawler
    {

        private readonly SQLiteConnection _conn;
        private readonly LoginDatabase _db;
        public event RequestReady GetFinish;
        public event EventHandler GetFinishWindow;
        public delegate void RequestReady(List<Metadata> meta);

        public DBCrawler(SQLiteConnection conn,LoginDatabase db)
        {
            _conn = conn;
            _db = db;
        }

        public int GetScalar(string command)
        {
            Logger.LogDisp.NewMessage(LogType.Debug, "GetScalar Query: {0}", command);
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
            accID = NoSQLDB.EscapeString(accID);

            BackgroundRequest.MetaDataReady.Add(
                (sender, e) =>
                {
                    if (GetFinish != null)
                        GetFinish((List<Metadata>)sender);
                    if (GetFinishWindow != null)
                        GetFinishWindow(null, null);
                });
            BackgroundRequest.GetMetas(_conn, SelectGroupByAccount.Replace("@AccID", accID));
            //return GetMeta();
        }

        public void GetAccountsByIP(string ip)
        {
            //accID = EscapeString(accID);

            _db.IPDataReady.Add((sender, e) =>
            {
                var remdoubles = ((List<IPData>)sender).GroupBy(x => x.AccID).Select(y => y.First());
                var ret = new List<Metadata>();
                foreach (var id in remdoubles)
                {
                    ret.AddRange(
                        BackgroundRequest.GetMetaForeground(
                        SelectGroupByAccount.Replace("@AccID", id.AccID),
                        _conn));
                }

                if (GetFinish != null)
                    GetFinish(ret);
                if (GetFinishWindow != null)
                    GetFinishWindow(null, null);

            }
                );

            _db.GetAccIdbyIP(ip);
            //return GetMeta();
        }


        public void GetMetasByItem(uint hash)
        {
            BackgroundRequest.MetaDataReady.Add(
                (sender, e) =>
                {
                    if (GetFinish != null)
                        GetFinish((List<Metadata>)sender);
                    if (GetFinishWindow != null)
                        GetFinishWindow(null, null);
                });
            BackgroundRequest.GetMetas(_conn, SelectGroupByItem.Replace("@Equip", hash.ToString(CultureInfo.InvariantCulture)));
        }

        public void GetMetasByNames(List<string> names)
        {
            BackgroundRequest.MetaDataReady.Add(
                (sender, e) =>
                {
                    if (GetFinish != null)
                        GetFinish((List<Metadata>)sender);
                    if (GetFinishWindow != null)
                        GetFinishWindow(null, null);
                });
            BackgroundRequest.GetMetas(_conn, SelectGroupByNames.
                Replace("@CharNames",
                    string.Join(
                        ",",
                        names.Select(
                            w =>
                                "'" + NoSQLDB.EscapeString(w) + "'")
                        )
                ));
        }

        public void GetMetasByName(string name)
        {
            name = NoSQLDB.EscapeString(name);
            BackgroundRequest.MetaDataReady.Add(
                (sender, e) =>
                {
                    if (GetFinish != null)
                        GetFinish((List<Metadata>)sender);
                    if (GetFinishWindow != null)
                        GetFinishWindow(null, null);
                });
            BackgroundRequest.GetMetas(_conn, SelectGroupByName.Replace("@CharName", name));
            //return GetMeta();
        }

        public void GetMetasByCharID(string name)
        {
            name = NoSQLDB.EscapeString(name);
            BackgroundRequest.MetaDataReady.Add(
                (sender, e) =>
                {
                    if (GetFinish != null)
                        GetFinish((List<Metadata>)sender);
                    if (GetFinishWindow != null)
                        GetFinishWindow(null, null);
                });
            BackgroundRequest.GetMetas(_conn, SelectGroupByCharCode.Replace("@CharCode", name));
            //return GetMeta();
        }

        public void GetMetasBySystem(string system)
        {
            system = NoSQLDB.EscapeString(system);
            BackgroundRequest.MetaDataReady.Add(
                (sender, e) =>
                {
                    if (GetFinish != null)
                        GetFinish((List<Metadata>)sender);
                    if (GetFinishWindow != null)
                        GetFinishWindow(null, null);
                });
            BackgroundRequest.GetMetas(_conn, SelectGroupBySystem.Replace("@System", system));
        }

        public int CountRows(string table)
        {
            return GetScalar("select count(CharName) from '" + table + "';");
        }

    }
}
