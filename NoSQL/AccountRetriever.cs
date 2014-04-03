using System;
using System.Text;
using LogDispatcher;

namespace FLAccountDB.NoSQL
{
    static class AccountRetriever
    {

        /// <summary>
        /// Returns a Player object associated with the charfile.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Character GetAccount(string path)
        {
            var flFile = new FLDataFile.DataFile(path);
            var player = new Character();

            foreach (var set in flFile.GetFirstOf("Player").Settings)
                {
                    switch (set.Name)
                    {
                        case "money":
                            player.Money = uint.Parse(set[0]);
                            break;
                        case "name":
                            var name = "";
                            while (set[0].Length > 0)
                             {
                                 name += (char)Convert.ToUInt16(set[0].Substring(0, 4), 16);
                                 set[0] = set[0].Remove(0, 4);
                             }
                            player.Name = name;
                            break;
                        case "rank":
                            player.Rank = byte.Parse(set[0]);
                            break;
                        case "house":
                            player.Reputation.Add(
                                set[1],float.Parse(set[0]));
                            break;
                        case "description":
                        case "tstamp":
                            break;
                        case "rep_group":
                            player.ReputationIFF = set[0];
                            break;
                        case "system":
                            player.System = set[0];
                            break;
                        case "base":
                            player.Base = set[0];
                            break;
                        case "ship_archetype":
                            player.ShipArch = uint.Parse(set[0]);
                            break;
                        case "hull_status":
                            player.Health = float.Parse(set[0]);
                            break;
                        case "equip":
                            player.EquipmentList.Add(
                                new Tuple<uint, string, float>(
                                    uint.Parse(set[0]),
                                    set[1],
                                    float.Parse(set[2])
                                    )
                                    );
                            break;
                        case "cargo":
                            player.Cargo.Add(uint.Parse(set[0]), uint.Parse(set[1]));
                            break;
                        case "last_base":
                            player.LastBase = set[0];
                            break;
                            //TODO: voice, com_body etc
                        case "pos":
                            player.Position = new[]
                            {
                                float.Parse(set[0]),
                                float.Parse(set[1]),
                                float.Parse(set[2])
                            };
                            break;
                        case "rotate":
                            player.Rotation = new[]
                            {
                                float.Parse(set[0]),
                                float.Parse(set[1]),
                                float.Parse(set[2])
                            };
                            break;
                        case "visit":
                            player.Visits.Add(uint.Parse(set[0]),byte.Parse(set[1]));
                            break;

                    }
                    
            }


            foreach (var set in flFile.GetFirstOf("mPlayer").Settings)
            {
                switch (set.Name)
                {
                    case "sys_visited":
                        player.VisitedSystems.Add(uint.Parse(set[0]));
                        break;
                    case "base_visited":
                        player.VisitedBases.Add(uint.Parse(set[0]));
                        break;
                }
            }

            return player;
        }


        //private static StringBuilder _equipList = new StringBuilder();
        /// <summary>
        /// Returns a Player object associated with the charfile.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Metadata GetMeta(string path)
        {
            var flFile = new FLDataFile.DataFile(path);
            var player = new Metadata
            {
                LastOnline = DateTime.Now
            };

            //_equipList.Clear()
            var equipList = new StringBuilder();
            foreach (var set in flFile.GetFirstOf("Player").Settings)
            {
                switch (set.Name)
                {
                    case "money":
                        player.Money = uint.Parse(set[0]);
                        break;
                    case "name":
                        var name = "";
                        while (set[0].Length > 0)
                        {
                            name += (char)Convert.ToUInt16(set[0].Substring(0, 4), 16);
                            set[0] = set[0].Remove(0, 4);
                        }
                        player.Name = name;
                        break;
                    case "rank":
                        player.Rank = byte.Parse(set[0]);
                        break;
                    case "system":
                        player.System = set[0];
                        break;
                    case "base":
                        player.Base = set[0];
                        break;
                    case "ship_archetype":
                        uint res;
                        if (uint.TryParse(set[0], out res))
                        {
                            player.ShipArch = res;
                            break;
                        }
                        LogDispatcher.LogDispatcher.NewMessage(LogType.Warning,"Garbage shiparch: " + set[0] + " for " + flFile.Path);

                        return null;

                        //break;
                    case "equip":
                        equipList.Append(" ");
                        equipList.Append(set[0]);
                        break;
                    case "tstamp":
                        long high = uint.Parse(set[0]);
                        long low = uint.Parse(set[1]);
                        player.LastOnline = DateTime.FromFileTimeUtc(high << 32 | low);
                        break;
                }

            }


            player.CharID = path.Substring(path.Length - 14, 11);
            player.CharPath = path.Substring(path.Length - 26, 23);
            player.Equipment = equipList.ToString();
            return player;
        }



        //public static string GetAccountID(string accDirPath)
        //{
        //    // shameless copypaste here
        //    var accountIdFilePath = accDirPath + Path.DirectorySeparatorChar + "name";

        //    // Read a 'name' file into memory.
        //    var fs = File.OpenRead(accountIdFilePath);
        //    var buf = new byte[fs.Length];
        //    fs.Read(buf, 0, (int)fs.Length);
        //    fs.Close();

        //    // Decode the account ID
        //    var accountID = "";
        //    for (var i = 0; i < buf.Length; i += 2)
        //    {
        //        switch (buf[i])
        //        {
        //            case 0x43:
        //                accountID += '-';
        //                break;
        //            case 0x0f:
        //                accountID += 'a';
        //                break;
        //            case 0x0c:
        //                accountID += 'b';
        //                break;
        //            case 0x0d:
        //                accountID += 'c';
        //                break;
        //            case 0x0a:
        //                accountID += 'd';
        //                break;
        //            case 0x0b:
        //                accountID += 'e';
        //                break;
        //            case 0x08:
        //                accountID += 'f';
        //                break;
        //            case 0x5e:
        //                accountID += '0';
        //                break;
        //            case 0x5f:
        //                accountID += '1';
        //                break;
        //            case 0x5c:
        //                accountID += '2';
        //                break;
        //            case 0x5d:
        //                accountID += '3';
        //                break;
        //            case 0x5a:
        //                accountID += '4';
        //                break;
        //            case 0x5b:
        //                accountID += '5';
        //                break;
        //            case 0x58:
        //                accountID += '6';
        //                break;
        //            case 0x59:
        //                accountID += '7';
        //                break;
        //            case 0x56:
        //                accountID += '8';
        //                break;
        //            case 0x57:
        //                accountID += '9';
        //                break;
        //            default:
        //                accountID += '?';
        //                break;
        //        }
        //    }

        //    return accountID;
        //}

    }
}
