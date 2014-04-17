using System;

namespace FLAccountDB.LoginDB
{
    public class IPData
    {

        public IPData(string accID, string ip, DateTime date)
        {
            AccID = accID;
            IP = ip;
            Date = date;
        }

        public IPData()
        {
            
        }

        public string AccID { get; set; }
        public string IP { get; set; }

        public DateTime Date { get; set; }
    }
}
