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

    public class IDData
    {

        public IDData(string accID, string id1, string id2)
        {
            AccID = accID;
            ID1 = id1;
            ID2 = id2;
        }

        public IDData()
        {

        }

        public string AccID { get; set; }
        public string ID1 { get; set; }
        public string ID2 { get; set; }
    }
}
