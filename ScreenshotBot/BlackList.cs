using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EasyCapture;

namespace ScreenshotBot
{
    class BlackList
    {
        IniFile ini;
        string entry;
        string[] items;

        public BlackList(string entry, IniFile ini)
        {
            this.entry = entry; this.ini = ini;
            Read(); 
        }

        void Read()
        {
            items = ini.IniReadValue("BLACKLISTS", entry).Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
        }

        void Sync()
        {
            string ini_entry = "";
            foreach (string item in items) ini_entry += item + ",";
            ini.IniWriteValue("BLACKSLISTS", entry, ini_entry.Substring(0, ini_entry.Length - 1));
        }

        void Add(string item)
        {
            List<string> st = new List<string>(items);
            st.Add(item);
            items = st.ToArray();
            Sync();
        }

        public void Remove(string item)
        {
            List<string> st = new List<string>(items);
            st.Remove(item);
            items = st.ToArray();
            Sync();
        }

        public bool isBlackListed(string item)
        {
            return items.Contains(item);
        }
    }
}
