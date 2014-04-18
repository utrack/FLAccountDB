using System;
using System.Collections.Generic;
using System.Globalization;
using FLAccountDB.NoSQL;

namespace FLAccountDB.Data
{

    public class ReputationItem
    {
        public ReputationItem(string nickname, float rep)
        {
            Nickname = nickname;
            Value = rep;
        }
        public string Nickname { get; set; }
        public float Value { get; set; }
    }

    public class Character : Metadata
    {
        public bool IsAdmin;
        public bool IsBanned;
        public bool IsOnline;
        public List<ReputationItem> Reputation = new List<ReputationItem>();
        //Dictionary<string, float>

        

        /// <summary>
        /// Player's primary IFF\Faction
        /// </summary>
        public string ReputationIFF;


        public Dictionary<uint, byte> Visits = new Dictionary<uint, byte>();
        public List<uint> VisitedBases = new List<uint>();
        public List<uint> VisitedSystems = new List<uint>();

        public DateTime Created;

        public float Health;
        public List<WTuple<uint, uint>> Cargo = new List<WTuple<uint, uint>>();
        
        /// <summary>
        /// Stores player's equipment. Tuple: ID, Hardpoint name, Health
        /// </summary>
        public List<Tuple<uint,string,float>> EquipmentList = new List<Tuple<uint, string, float>>();


        public string LastBase;
        public float[] Position;
        public float[] Rotation;

        new public string Equipment
        {
            get
            {
                var ret = "";
                foreach (var eq in EquipmentList)
                {
                    ret += " ";
                    ret += eq.Item1.ToString(CultureInfo.InvariantCulture);
                }
                return ret;
            }
        }


        public static Character ParseCharacter(string path, LogDispatcher.LogDispatcher log)
        {
            return AccountRetriever.GetAccount(path,log);
        }
    }
}
