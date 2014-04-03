using System;
using System.Data.SQLite;
using System.Timers;
using LogDispatcher;

namespace FLAccountDB
{
    public class DBQueue
    {
        public SQLiteConnection Conn;
        public SQLiteTransaction Transaction;
        public int Count;

        private int _threshold;
        private readonly Timer _timer;
        public DBQueue(SQLiteConnection conn, int timeout = 15000, int threshold = 1000)
        {
            _threshold = threshold;
            Conn = conn;
            
            Transaction = Conn.BeginTransaction();
            _timer = new Timer(timeout);
            _timer.Elapsed += _timer_Elapsed;
            _timer.Enabled = true;
            _timer.Start();
            GC.KeepAlive(_timer);

        }

        public void SetThreshold(int value)
        {
            _threshold = value;
        }

        public void SetTimeout(int value)
        {
            _timer.Interval = value;
        }

        public void Execute(SQLiteCommand cmd)
        {
            lock (Conn)
                lock (Transaction)
                {
                    cmd.Connection = Conn;
                    cmd.Transaction = Transaction;

                    cmd.ExecuteNonQuery();
                }
            Count++;
            if (Count > _threshold)
                Force();

        }

        public void Force()
        {
            _timer_Elapsed(null,null);
        }

        void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Count > 0)
            {
                
                lock (Transaction)
                {
                    if (Transaction != null)
                    {
                        LogDispatcher.LogDispatcher.NewMessage(LogType.Garbage, "DB Committed, changes: " + Count);
                        Transaction.Commit();
                        Transaction = Conn.BeginTransaction();
                    }

                }

            }

            Count = 0;
        }
    }
}
