using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FLDataFile;

namespace FLAccountDB.NoSQL
{
    public class IniBan
    {
        public List<WTuple<string, string>> Bans
        {
            get; set;
        }

        private readonly DataFile _file;
        private readonly string _filePath;
        public IniBan(string path)
        {
            _filePath = path;
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
                        set.Name,
                        String.Join(", ",set)
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

            _file.Save(_filePath);
        }

        public void RemoveBan(string ident)
        {
            Bans.RemoveAll(w=>w.Item1 == ident);
            var set = _file.GetSetting("ban", ident);

            set.Comments = set[0] + " = " + String.Join(", ", set.Skip(1));
            set.Clear();

            _file.Save(_filePath);
        }

    }
}
