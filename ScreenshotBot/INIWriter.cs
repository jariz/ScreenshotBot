using System;
using System.Runtime.InteropServices;
using System.Text;
using IniParser.Model;
using IniParser.Parser;
using IniParser;
using System.IO;

namespace EasyCapture
{
    /// <summary>
    /// Create a New INI file to store or load data
    /// </summary>
    public class IniFile
    {
        public string path;
        public FileIniDataParser parser;
        public IniData data;

        /// <summary>
        /// INIFile Constructor.
        /// </summary>
        /// <PARAM name="INIPath"></PARAM>
        public IniFile(string INIPath)
        {
            parser = new FileIniDataParser();
            data = parser.ReadFile(INIPath);
        }
        /// <summary>
        /// Write Data to the INI File
        /// </summary>
        /// <PARAM name="Section"></PARAM>
        /// Section name
        /// <PARAM name="Key"></PARAM>
        /// Key Name
        /// <PARAM name="Value"></PARAM>
        /// Value Name
        public void IniWriteValue(string Section, string Key, string Value)
        {
            data[Section][Key] = Value;
            parser.SaveFile(path, data);
        }

        /// <summary>
        /// Read Data Value From the Ini File
        /// </summary>
        /// <PARAM name="Section"></PARAM>
        /// <PARAM name="Key"></PARAM>
        /// <PARAM name="Path"></PARAM>
        /// <returns></returns>
        public string IniReadValue(string Section, string Key)
        {
            if(!data.Sections.ContainsSection(Section)) return "";
            if(!data[Section].ContainsKey(Key)) return "";
            return data[Section][Key];

        }
    }
}