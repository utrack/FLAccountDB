using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FLDataFile;

namespace FLAccountDB.NoSQL
{
    class IniBan
    {
        public List<WTuple<string, string>> Bans
        {
            get; set;
        }

        private readonly DataFile _file;
        public IniBan(string path)
        {
            Bans = new List<WTuple<string, string>>();

            if (!File.Exists(path))
            {
                _file = new DataFile();
                _file.Sections.Add(new Section("bans"));
                return;
            }

            _file = new DataFile(path);

            foreach (var set in _file.GetSettings("bans"))
            {
                Bans.Add(
                    new WTuple<string, string>(
                        set[0],
                        String.Join(", ",set.Skip(1))
                        )
                        );
            }
        }



        public bool IsBanned(string ident)
        {
            return Bans.FirstOrDefault(tuple => tuple.Item1 == ident) != null;
        }

        public void AddBan(string ident, string reason)
        {
            Bans.Add(new WTuple<string, string>(ident,reason));
            _file.Sections.First(w=>w.Name == "bans").Settings.Add(
                new Setting(reason,ident));
        }

        public void RemoveBan(string ident)
        {
            Bans.RemoveAll(w=>w.Item1 == ident);
            var set = _file.GetSetting("ban", ident);

            set.Comments = set[0] + " = " + String.Join(", ", set.Skip(1));
            set.Clear();
        }

    }
}
