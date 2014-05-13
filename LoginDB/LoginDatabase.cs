using System;
using System.Data.SQLite;
using LogDispatcher;

namespace FLAccountDB.LoginDB
{
    public class LoginDatabase
    {
        private readonly DBQueue _queue;
        private readonly SQLiteConnection _conn;

        //readonly OneShotHandlerQueue<EventArgs> _evQueue = new OneShotHandlerQueue<EventArgs>();
        public readonly OneShotHandlerQueue<EventArgs> IPDataReady = new OneShotHandlerQueue<EventArgs>();
        public readonly OneShotHandlerQueue<EventArgs> IDDataReady = new OneShotHandlerQueue<EventArgs>();
        private event EventHandler OnIPReady;
        private event EventHandler OnIDReady;

        public LoginDatabase( SQLiteConnection conn,DBQueue queue)
        {
            _queue = queue;
            _conn = conn;
            //BackgroundRequest.IPReady += _evQueue.Handle;
            OnIPReady += IPDataReady.Handle;
            OnIDReady += IDDataReady.Handle;
            Logger.LogDisp.NewMessage(LogType.Info,"Login DB initialized.");
        }

        private const string AddIPString = "INSERT INTO LoginIP "
            + "(AccID,IP,LogTime) "
            + "VALUES(@AccID,@IP,@LogTime);";

        public void AddIP(string accID, string ip, DateTime logTime)
        {
            using (var comm = new SQLiteCommand(AddIPString, _queue.Conn))
            {
                comm.Parameters.AddWithValue("@AccID", accID);
                comm.Parameters.AddWithValue("@LogTime", logTime);
                comm.Parameters.AddWithValue("@IP", ip);
                if (_queue != null)
                    _queue.Execute(comm);
            }
        }


        private const string AddIDString = "INSERT INTO LoginID "
    + "(AccID,ID1,ID2) "
    + "VALUES(@AccID,@ID1,@ID2);";

        public void AddIds(string accID, string id1, string id2)
        {
            using (var comm = new SQLiteCommand(AddIDString, _queue.Conn))
            {
                comm.Parameters.AddWithValue("@AccID", accID);
                comm.Parameters.AddWithValue("@ID1", id1);
                comm.Parameters.AddWithValue("@ID2", id2);
                if (_queue != null)
                    _queue.Execute(comm);
            }
        }

        private const string SelectIPbyID = "SELECT * FROM LoginIP WHERE AccID = '@AccID'";
        public void GetIPByAccID(string accID)
        {
            BackgroundRequest.IPDataReady.Add((sender, e) => OnIPReady(sender,null));
            BackgroundRequest.GetIPData(_conn,SelectIPbyID.Replace("@AccID", accID));
        }

        private const string SelectIDbyID = "SELECT * FROM LoginID WHERE AccID = '@AccID'";
        public void GetIDByAccID(string accID)
        {
            BackgroundRequest.IDDataReady.Add((sender, e) => OnIDReady(sender, null));
            BackgroundRequest.GetIDData(_conn, SelectIDbyID.Replace("@AccID", accID));
        }

        private const string SelectIDbyIP = "SELECT * FROM LoginIP WHERE IP = '@IP'";
        
        
        public void GetAccIdbyIP(string ip)
        {
            BackgroundRequest.IPDataReady.Add((sender, e) => OnIPReady(sender, null));
            BackgroundRequest.GetIPData(_conn, SelectIDbyIP.Replace("@IP", ip));
        }

    }
}
