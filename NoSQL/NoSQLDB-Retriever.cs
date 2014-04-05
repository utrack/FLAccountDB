using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;

namespace FLAccountDB.NoSQL
{

    public enum RetrieveType
    {
        Full,
        Name,
        Account,
        Equipment
    }

    public partial class NoSQLDB
    {
        private const string SelectGroupByName = "SELECT * FROM Accounts WHERE CharName IN (@CharNames)";
        private const string SelectGroupByAccount = "SELECT * FROM Accounts WHERE AccID = '@AccID'";
        private const string SelectGroupByItem = "SELECT * FROM Accounts WHERE Equipment LIKE '%@Equip%'";


        /// <summary>
        /// Executes the issued command and returns list of resulting acc metadata.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private List<Metadata> GetMeta(string command)
        {
            if (_conn == null) return null;
            if (_conn.State == ConnectionState.Closed) return null;
            var ret = new List<Metadata>();
            using (var cmd = new SQLiteCommand(command, _conn))
            {
                cmd.CommandText = command;
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var md = new Metadata
                    {
                        CharPath = reader.GetString(0),
                        Name = reader.GetString(1),
                        AccountID = reader.GetString(2),
                        CharID = reader.GetString(3),
                        Money = (uint)reader.GetInt32(4),
                        Rank = (byte)reader.GetInt32(5),
                        ShipArch = (uint)reader.GetInt32(6),
                        System = reader.GetString(7),
                        Base = reader.GetString(8),
                        Equipment = reader.GetString(9),
                        LastOnline = reader.GetDateTime(11)
                    };

                    ret.Add(md);
                }

            }

            return ret;
        }

        public List<Metadata> GetAccountChars(string accID)
        {
            return GetMeta(SelectGroupByAccount.Replace("@AccID", accID));
        }

        public List<Metadata> GetMetasByItem(uint hash)
        {
            return GetMeta(SelectGroupByItem.Replace("@Equip", hash.ToString(CultureInfo.InvariantCulture)));
        }

        public List<Metadata> GetMetasByNames(List<string> names)
        {
            return GetMeta(
                SelectGroupByName.
                    Replace("@CharNames",
                    string.Join(
                        ",", 
                        names.Select(
                            w => 
                            "'" + EscapeString(w) + "'")
                               )
                           )
                          );
        }

        public int CountRows(string table)
        {
            int rowCount;
            using (var cmd = new SQLiteCommand(_conn))
            {
                cmd.CommandText = "select count(CharName) from '" + table +"';";
                cmd.CommandType = CommandType.Text;
                

                rowCount = Convert.ToInt32(cmd.ExecuteScalar());
            }
            return rowCount;

        }

    }
}
