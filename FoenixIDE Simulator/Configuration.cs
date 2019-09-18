using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoenixIDE.Simulator
{
    public enum ConfigurationSection
    {
        Audio,
        Display,
        SDCard,
        Common
    }

    public enum configurationKeys
    {
        OPLSystem,
        OPLParallelPort,
        StartUpHexFile
    }

    public enum OPLSystem
    {
        DOSBox,
        Nuked,
        OPL3LPT,
        OPL2LPT
    }

    public class Configuration
    {
        public const string configFilename = @".\Configuration.ini";
        private IniData configurationData;

        public OPLSystem OPLSystem
        {
            get
            {
                return (OPLSystem)Enum.Parse(typeof(OPLSystem), configurationData[ConfigurationSection.Audio.ToString()][configurationKeys.OPLSystem.ToString()]);
            }
            set
            {
                configurationData[ConfigurationSection.Audio.ToString()][configurationKeys.OPLSystem.ToString()] = value.ToString();
            }
        }

        public int OPLParallelPort
        {
            get
            {
                return Convert.ToInt32(configurationData[ConfigurationSection.Audio.ToString()][configurationKeys.OPLParallelPort.ToString()], 16);
            }
            set
            {
                configurationData[ConfigurationSection.Audio.ToString()][configurationKeys.OPLSystem.ToString()] = value.ToString("X");
            }
        }

        public string StartUpHexFile
        {
            get
            {
                return configurationData[ConfigurationSection.Common.ToString()][configurationKeys.StartUpHexFile.ToString()];
            }
            set
            {
                configurationData[ConfigurationSection.Common.ToString()][configurationKeys.StartUpHexFile.ToString()] = value;
            }
        }

        //https://csharpindepth.com/articles/singleton 
        private static Configuration instance = null;
        private static readonly object padlock = new object();
        public static Configuration Current
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new Configuration();
                    }
                    return instance;
                }
            }
        }

        private Configuration()
        {
            Reset();
        }

        public void Reset()
        {
            if (File.Exists(configFilename) == false)
            {
                FileStream configFile = File.Create(configFilename);
                configFile.Close();
            }

            configurationData = new FileIniDataParser().ReadFile(configFilename);

            // Common
            if (configurationData.Sections[ConfigurationSection.Common.ToString()] == null)
            {
                configurationData.Sections.AddSection(ConfigurationSection.Common.ToString());
            }

            if (configurationData.Sections[ConfigurationSection.Common.ToString()][configurationKeys.StartUpHexFile.ToString()] == null)
            {
                configurationData[ConfigurationSection.Common.ToString()].AddKey(configurationKeys.StartUpHexFile.ToString(), "kernel.hex");
            }


            // Audio
            if (configurationData.Sections[ConfigurationSection.Audio.ToString()] == null)
            {
                configurationData.Sections.AddSection(ConfigurationSection.Audio.ToString());
            }

            if (configurationData.Sections[ConfigurationSection.Audio.ToString()][configurationKeys.OPLSystem.ToString()] == null)
            {
                configurationData[ConfigurationSection.Audio.ToString()].AddKey(configurationKeys.OPLSystem.ToString(), OPLSystem.OPL3LPT.ToString());
            }

            if (configurationData.Sections[ConfigurationSection.Audio.ToString()][configurationKeys.OPLParallelPort.ToString()] == null)
            {
                configurationData[ConfigurationSection.Audio.ToString()].AddKey(configurationKeys.OPLParallelPort.ToString(), "0x378");
            }

            configurationData.Sections.AddSection(ConfigurationSection.Display.ToString());
            configurationData.Sections.AddSection(ConfigurationSection.SDCard.ToString());
        }

        public void Save()
        {
            new FileIniDataParser().WriteFile(configFilename, configurationData);
        }




    }
}
