﻿using System;
using FLAccountDB.NoSQL;

namespace FLAccountDB.Data
{
    public class Metadata
    {
        public string AccountID;
        public string CharID;
        public string Name;

        public byte Rank;
        public uint Money;
        public bool IsBanned;
        public uint ShipArch;
        public string System;
        public string Base;
        public string Equipment;
        public DateTime LastOnline;

        public string CharPath;

        public static Metadata ParseMeta(string path)
        {
            return AccountRetriever.GetMeta(path);
        }


    }
}
