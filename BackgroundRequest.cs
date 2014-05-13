using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using FLAccountDB.Data;
using FLAccountDB.LoginDB;
using LogDispatcher;

namespace FLAccountDB
{
    public static class BackgroundRequest
    {
        static event EventHandler OnMetaReady;
        static event EventHandler OnIPReady;
        static event EventHandler OnIDReady;
        public static readonly OneShotHandlerQueue<EventArgs> IPDataReady = new OneShotHandlerQueue<EventArgs>();
        public static readonly OneShotHandlerQueue<EventArgs> IDDataReady = new OneShotHandlerQueue<EventArgs>();
        public static readonly OneShotHandlerQueue<EventArgs> MetaDataReady = new OneShotHandlerQueue<EventArgs>();

        static BackgroundRequest()
        {
            OnMetaReady += MetaDataReady.Handle;
            OnIPReady += IPDataReady.Handle;
            OnIDReady += IDDataReady.Handle;
        }

        public static void GetIPData(SQLiteConnection conn, string command)
        {
            var bgw = new BackgroundWorker();
            //bgw.RunWorkerCompleted +=   ;
            bgw.DoWork += _bgw_DoWork_IP;
            bgw.RunWorkerAsync(
                new object[]
            {
                conn,
                command
            });
        }

        static void _bgw_DoWork_IP(object sender, DoWorkEventArgs e)
        {
                OnIPReady(GetIP((string)((object[])e.Argument)[1], (SQLiteConnection)((object[])e.Argument)[0]), null);
        }


        /// <summary>
        /// Executes the issued command in current thread and returns list of resulting IP data.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="conn"></param>
        /// <returns></returns>
        public static List<IPData> GetIP(string command, SQLiteConnection conn)
        {
            Logger.LogDisp.NewMessage(LogType.Debug, "GetIP Query: {0}", command);
            if (conn == null) return null;
            if (conn.State == ConnectionState.Closed) return null;
            var ret = new List<IPData>();
            using (var cmd = new SQLiteCommand(command, conn))
            {
                cmd.CommandText = command;
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var md = new IPData
                    {
                        AccID = reader.GetString(0),
                        IP = reader.GetString(1),
                        Date = reader.GetDateTime(2)
                    };

                    ret.Add(md);
                }

            }

            return ret;
        }

        public static void GetIDData(SQLiteConnection conn, string command)
        {
            var bgw = new BackgroundWorker();
            //bgw.RunWorkerCompleted +=   ;
            bgw.DoWork += _bgw_DoWork_ID;
            bgw.RunWorkerAsync(
                new object[]
            {
                conn,
                command
            });
        }

        private static void _bgw_DoWork_ID(object sender, DoWorkEventArgs e)
        {
            OnIDReady(GetID((string)((object[])e.Argument)[1], (SQLiteConnection)((object[])e.Argument)[0]), null);
        }

        static void _bgw_DoWork(object sender, DoWorkEventArgs e)
        {
                OnMetaReady(GetMetaForeground((string)((object[])e.Argument)[1], (SQLiteConnection)((object[])e.Argument)[0]),null);
        }

        public static void GetMetas(SQLiteConnection conn, string command)
        {
            var bgw = new BackgroundWorker();
            //bgw.RunWorkerCompleted +=   ;
            bgw.DoWork += _bgw_DoWork;
            bgw.RunWorkerAsync(
                new object[]
            {
                conn,
                command
            });
        }

        /// <summary>
        /// Executes the issued command in current thread and returns list of resulting ID data.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="conn"></param>
        /// <returns></returns>
        public static List<IDData> GetID(string command, SQLiteConnection conn)
        {
            Logger.LogDisp.NewMessage(LogType.Debug, "GetIP Query: {0}", command);
            if (conn == null) return null;
            if (conn.State == ConnectionState.Closed) return null;
            var ret = new List<IDData>();
            using (var cmd = new SQLiteCommand(command, conn))
            {
                cmd.CommandText = command;
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var md = new IDData
                    {
                        AccID = reader.GetString(0),
                        ID1 = reader.GetString(1),
                        ID2 = reader.GetString(2)
                    };

                    ret.Add(md);
                }

            }

            return ret;
        }

        /// <summary>
        /// Executes the issued command and returns list of resulting acc metadata.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="conn"></param>
        /// <returns></returns>
        public static List<Metadata> GetMetaForeground(string command,SQLiteConnection conn)
        {
            Logger.LogDisp.NewMessage(LogType.Debug, "GetMeta Query: {0}", command);
            if (conn == null) return null;
            if (conn.State == ConnectionState.Closed) return null;
            var ret = new List<Metadata>();
            using (var cmd = new SQLiteCommand(command, conn))
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
                    md.Money = (uint)reader.GetInt32(4);
                    md.Rank = (byte)reader.GetInt32(5);
                    md.ShipArch = (uint)reader.GetInt32(6);
                    md.System = reader.GetString(7);
                    md.Base = reader.GetValue(8).ToString();
                    md.Equipment = reader.GetString(9);
                    md.LastOnline = reader.GetDateTime(11);
                    md.IsBanned = reader.GetBoolean(14);
                    //};

                    ret.Add(md);
                }

            }

            return ret;
        }


    }
}
