using System;
using System.Globalization;
using System.Linq;
using System.Text;
using FLDataFile;
using LogDispatcher;

namespace FLAccountDB.Data
{
    static class AccountRetriever
    {
        private static readonly NumberFormatInfo Nfi = new NumberFormatInfo
        {NumberDecimalSeparator = "."};

        /// <summary>
        /// Returns a Player object associated with the charfile.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public static Character GetAccount(string path,LogDispatcher.LogDispatcher log)
        {
            var flFile = new DataFile(path);
            var player = new Character
            {
                Created = System.IO.File.GetCreationTime(path)
            };

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
                                new ReputationItem(set[1],
                                    float.Parse(set[0], Nfi))
                                );
                            break;
                        case "description":
                            break;
                        case "tstamp":
                            long high = uint.Parse(set[0]);
                            long low = uint.Parse(set[1]);
                            player.LastOnline = DateTime.FromFileTimeUtc(high << 32 | low);
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
                            uint res;
                        if (uint.TryParse(set[0], out res))
                        {
                            player.ShipArch = res;
                            break;
                        }

                        if (Logger.LogDisp != null)
                            Logger.LogDisp.NewMessage(LogType.Warning, "Garbage shiparch: " + set[0] + " for " + flFile.Path);
                            return null;
                        case "base_hull_status":
                            player.Health = float.Parse(set[0], Nfi);
                            break;
                        case "equip":
                            player.EquipmentList.Add(
                                new Tuple<uint, string, float>(
                                    uint.Parse(set[0]),
                                    set[1],
                                    float.Parse(set[2], Nfi)
                                    )
                                    );
                            break;
                        case "cargo":
                            if (set[1].StartsWith("-"))
                                log.NewMessage(LogType.Error,"Player {0} bad setting: {1}",player.Name,set.String());
                            else
                                player.Cargo.Add(new WTuple<uint, uint>(uint.Parse(set[0]), uint.Parse(set[1])));
                            break;
                        case "last_base":
                            player.LastBase = set[0];
                            break;
                            //TODO: voice, com_body etc
                        case "pos":
                            player.Position = new[]
                            {
                                float.Parse(set[0],Nfi),
                                float.Parse(set[1],Nfi),
                                float.Parse(set[2],Nfi)
                            };
                            break;
                        case "rotate":
                            player.Rotation = new[]
                            {
                                float.Parse(set[0],Nfi),
                                float.Parse(set[1],Nfi),
                                float.Parse(set[2],Nfi)
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
                    case "total_time_played":
                        player.OnlineTime = Convert.ToUInt32(float.Parse(set[0], Nfi));
                        break;
                }
            }

            player.AccountID = path.Substring(path.Length - 26, 11);
            player.CharID = path.Substring(path.Length - 14, 11);
            player.CharPath = path.Substring(path.Length - 26, 23);

            return player;
        }

        /// <summary>
        /// Returns a Player object associated with the charfile.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Metadata GetMeta(string path)
        {
            var flFile = new DataFile(path);
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
                        if (Logger.LogDisp != null)
                            Logger.LogDisp.NewMessage(LogType.Warning, "Garbage shiparch: " + set[0] + " for " + flFile.Path);

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

        public static bool SaveCharacter(Character ch, string path)
        {
            var oldFile = new DataFile(path);
            var newFile = new DataFile();
            newFile.Sections.Add(new Section("Player"));
            var pSect = newFile.GetFirstOf("Player");

            pSect.Settings.Add(new Setting("description")
            {
                oldFile.GetSetting("Player", "description")[0]
            });


            

            pSect.Settings.Add(new Setting("tstamp")
            {
                ((ch.LastOnline.ToFileTime() >> 32) & 0xFFFFFFFF).ToString(CultureInfo.InvariantCulture),
                (ch.LastOnline.ToFileTime() & 0xFFFFFFFF).ToString(CultureInfo.InvariantCulture)
            });
            //TODO: name
            pSect.Settings.Add(oldFile.GetSetting("Player","name"));

            pSect.Settings.Add(new Setting("rank")
            {
                ch.Rank.ToString(CultureInfo.InvariantCulture)
            });

            foreach (var set in ch.Reputation.Select(rep => new Setting("house")
            {
                String.Format(Nfi, "{0:0.000;-0.000}", rep.Value), 
                rep.Nickname
            }))
            {
                pSect.Settings.Add(set);
            }

            pSect.Settings.Add(new Setting("rep_group")
            {
                ch.ReputationIFF
            });

            pSect.Settings.Add(new Setting("money")
            {
                ch.Money.ToString(CultureInfo.InvariantCulture)
            });

            pSect.Settings.Add(oldFile.GetSetting("Player","num_kills"));
            pSect.Settings.Add(oldFile.GetSetting("Player", "num_misn_successes"));
            pSect.Settings.Add(oldFile.GetSetting("Player", "num_misn_failures"));

            pSect.Settings.Add(oldFile.GetSetting("Player", "voice"));
            pSect.Settings.Add(oldFile.GetSetting("Player", "com_body"));
            pSect.Settings.Add(oldFile.GetSetting("Player", "com_head"));
            pSect.Settings.Add(oldFile.GetSetting("Player", "com_lefthand"));
            pSect.Settings.Add(oldFile.GetSetting("Player", "com_righthand"));

            pSect.Settings.Add(oldFile.GetSetting("Player", "body"));
            pSect.Settings.Add(oldFile.GetSetting("Player", "head"));
            pSect.Settings.Add(oldFile.GetSetting("Player", "lefthand"));
            pSect.Settings.Add(oldFile.GetSetting("Player", "righthand"));


            //TODO: position if in space
            pSect.Settings.Add(new Setting("system")
            {
                ch.System
            });

            if (ch.Base != null)
                pSect.Settings.Add(new Setting("base")
                {
                    ch.Base
                });
            else
            {
                //in space
                pSect.Settings.Add(new Setting("pos")
                {
                    String.Format(Nfi,"{0:0.0}",ch.Position[0]),
                    String.Format(Nfi,"{0:0.0}",ch.Position[1]),
                    String.Format(Nfi,"{0:0.0}",ch.Position[2])
                });

                pSect.Settings.Add(new Setting("rotate")
                {
                    "0","0","0"
                });

            }

            pSect.Settings.Add(new Setting("ship_archetype")
            {
                ch.ShipArch.ToString(CultureInfo.InvariantCulture)
            });

            foreach (var eq in ch.EquipmentList)
            {
                pSect.Settings.Add(new Setting("equip")
            {
                eq.Item1.ToString(CultureInfo.InvariantCulture),eq.Item2,String.Format(Nfi, "{0:0.00}", eq.Item3)
            });

            }

            foreach (var cg in ch.Cargo.Where(cg => !((cg.Item1 == 0) | (cg.Item2 == 0))))
            {
                pSect.Settings.Add(new Setting("cargo")
                {
                    cg.Item1.ToString(CultureInfo.InvariantCulture),
                    cg.Item2.ToString(CultureInfo.InvariantCulture),
                    "",
                    "",
                    "0"
                });
            }

            pSect.Settings.Add(new Setting("last_base")
            {
                ch.LastBase
            });

            // base_hull_status

            pSect.Settings.Add(new Setting("base_hull_status")
            {
                String.Format(Nfi, "{0:0.00}", ch.Health)
            });

            //todo: base_collision_group


            foreach (var eq in ch.EquipmentList)
            {
                pSect.Settings.Add(new Setting("base_equip")
            {
                eq.Item1.ToString(CultureInfo.InvariantCulture),eq.Item2,String.Format(Nfi, "{0:0.00}", eq.Item3)
            });

            }


            foreach (var cg in ch.Cargo.Where(cg => !((cg.Item1 == 0) | (cg.Item2 == 0))))
            {
                pSect.Settings.Add(new Setting("base_cargo")
            {
                cg.Item1.ToString(CultureInfo.InvariantCulture),
                cg.Item2.ToString(CultureInfo.InvariantCulture),
                "",
                "",
                "0"
            });

            }

            pSect.Settings.AddRange(oldFile.GetSettings("Player", "wg"));


            foreach (var visit in ch.Visits)
            {
                pSect.Settings.Add(new Setting("visit")
            {
                visit.Key.ToString(CultureInfo.InvariantCulture),
                visit.Value.ToString(CultureInfo.InvariantCulture)
            });
            }

            pSect.Settings.Add(oldFile.GetSetting("Player","interface"));

            //todo: mPlayer, flhook
            newFile.Sections.Add(oldFile.GetFirstOf("mPlayer"));
            newFile.Sections.Add(oldFile.GetFirstOf("flhook"));

            newFile.Save(path);
            Logger.LogDisp.NewMessage(LogType.Info, "Saved profile {0}: {1}", ch.Name, path);
            return true;
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
