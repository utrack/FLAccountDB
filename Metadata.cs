﻿using System;
using FLAccountDB.NoSQL;

namespace FLAccountDB
{
    public class Metadata
    {
        public string AccountID;
        public string CharID;
        public string Name;

        public byte Rank;
        public uint Money;

        public uint ShipArch;
        public string System;
        public string Base;
        public string Equipment;
        public DateTime LastOnline;

        public string CharPath;

        public static Metadata ParseMeta(string path, LogDispatcher.LogDispatcher log)
        {
            return AccountRetriever.GetMeta(path,log);
        }


    }
}
