﻿/*
 * MassEffectModder
 *
 * Copyright (C) 2014-2016 Pawel Kolodziejski <aquadran at users.sourceforge.net>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.

 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.
 *
 */

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MassEffectModder
{
    public class GameData
    {
        static private string _path = null;
        static public MeType gameType;
        private ConfIni _configIni;
        static public List<string> packageFiles;

        public bool DLCDataCacheDone = false;

        public GameData(MeType type, ConfIni configIni)
        {
            gameType = type;
            _configIni = configIni;

            var key = "ME" + (int)gameType;
            var path = configIni.Read(key, "GameDataPath");
            if (path != null && path != "")
            {
                _path = path.TrimEnd(Path.DirectorySeparatorChar);
                return;
            }

            string softwareKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\";
            string key64 = @"\Wow6432Node\";
            string gameKey = @"BioWare\Mass Effect";

            if (type == MeType.ME2_TYPE)
                gameKey += @" 2";
            else if (type == MeType.ME3_TYPE)
                gameKey += @" 3";

            path = (string)Registry.GetValue(softwareKey + gameKey, "Path", null);
            if (path == null)
                path = (string)Registry.GetValue(softwareKey + key64 + gameKey, "Path", null);
            if (path != null)
            {
                _path = path.TrimEnd(Path.DirectorySeparatorChar);
                configIni.Write(key, _path, "GameDataPath");
                return;
            }

            OpenFileDialog selectExe = new OpenFileDialog();
            selectExe.Title = "Please select the Mass Effect " + (int)gameType + " executable file";
            switch (gameType)
            {
                case MeType.ME1_TYPE:
                    selectExe.Filter = "ME1 exe file|MassEffect.exe";
                    selectExe.FileName = "MassEffect.exe";
                    break;
                case MeType.ME2_TYPE:
                    selectExe.Filter = "ME2 exe file|MassEffect2.exe";
                    selectExe.FileName = "MassEffect2.exe";
                    break;
                case MeType.ME3_TYPE:
                    selectExe.Filter = "ME3 exe file|MassEffect3.exe";
                    selectExe.FileName = "MassEffect3.exe";
                    break;
            }
            if (selectExe.ShowDialog() == DialogResult.OK)
            {
                if (gameType == MeType.ME3_TYPE)
                    _path = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(selectExe.FileName)));
                else
                    _path = Path.GetDirectoryName(Path.GetDirectoryName(selectExe.FileName));
            }
            if (_path != null)
                _configIni.Write(key, _path, "GameDataPath");
        }

        static public string GamePath
        {
            get
            {
                return _path;
            }
        }

        static public string MainData
        {
            get
            {
                if (_path != null)
                {
                    switch (gameType)
                    {
                        case MeType.ME1_TYPE:
                        case MeType.ME2_TYPE:
                            return Path.Combine(_path, @"BioGame\CookedPC");
                        case MeType.ME3_TYPE:
                            return Path.Combine(_path, @"BioGame\CookedPCConsole");
                        default:
                            return null;
                    }
                } 
                else
                    return null;
            }
        }

        static public string bioGamePath
        {
            get
            {
                if (_path != null)
                {
                    switch (gameType)
                    {
                        case MeType.ME1_TYPE:
                        case MeType.ME2_TYPE:
                        case MeType.ME3_TYPE:
                            return Path.Combine(_path, @"BioGame");
                        default:
                            return null;
                    }
                }
                else
                    return null;
            }
        }

        static public string RelativeGameData(string path)
        {
            if (_path == null || !path.Contains(_path))
                return null;
            else
                return path.Substring(_path.Length);
        }

        public string DLCData
        {
            get
            {
                if (_path != null)
                {
                    switch (gameType)
                    {
                        case MeType.ME1_TYPE:
                            return Path.Combine(_path, @"DLC");
                        case MeType.ME2_TYPE:
                        case MeType.ME3_TYPE:
                            return Path.Combine(_path, @"BioGame\DLC");
                        default:
                            return null;
                    }
                }
                else
                    return null;
            }
        }

        public string DLCDataCache
        {
            get
            {
                if (gameType == MeType.ME3_TYPE)
                    return Path.Combine(_path, @"BioGame\DLCCache");
                else
                    return null;
            }
        }

        public string GameExePath
        {
            get
            {
                if (gameType == MeType.ME1_TYPE)
                    return Path.Combine(_path, @"Binaries\MassEffect.exe");
                else if (gameType == MeType.ME2_TYPE)
                    return Path.Combine(_path, @"Binaries\MassEffect2.exe");
                else if (gameType == MeType.ME3_TYPE)
                    return Path.Combine(_path, @"Binaries\Win32\MassEffect3.exe");
                else
                    return null;
            }
        }

        public string GameUserPath
        {
            get
            {
                string dir;

                if (gameType == MeType.ME1_TYPE)
                    dir = @"BioWare\Mass Effect";
                else if (gameType == MeType.ME2_TYPE)
                    dir = @"BioWare\Mass Effect 2";
                else if (gameType == MeType.ME3_TYPE)
                    dir = @"BioWare\Mass Effect 3";
                else
                    return null;

                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), dir);
            }
        }

        public string ConfigIniPath
        {
            get
            {
                if (gameType == MeType.ME1_TYPE)
                    return Path.Combine(GameUserPath, @"Config");
                else if (gameType == MeType.ME2_TYPE)
                    return Path.Combine(GameUserPath, @"BioGame\Config");
                else if (gameType == MeType.ME3_TYPE)
                    return Path.Combine(GameUserPath, @"BioGame\Config");
                else
                    return null;
            }
        }

        public string EngineConfigIniPath
        {
            get
            {
                if (gameType == MeType.ME1_TYPE)
                    return Path.Combine(ConfigIniPath, @"BIOEngine.ini");
                else if (gameType == MeType.ME2_TYPE)
                    return Path.Combine(ConfigIniPath, @"GamerSettings.ini");
                else if (gameType == MeType.ME3_TYPE)
                    return Path.Combine(ConfigIniPath, @"GamerSettings.ini");
                else
                    return null;
            }
        }

        public string EntitlementCacheIniPath
        {
            get
            {
                if (gameType == MeType.ME2_TYPE)
                    return Path.Combine(ConfigIniPath, @"BioPersistentEntitlementCache.ini");
                else
                    return null;
            }
        }

        public bool getPackages()
        {
            if (packageFiles != null && packageFiles.Count != 0)
                return true;

            if (gameType == MeType.ME1_TYPE)
            {
                packageFiles = Directory.GetFiles(MainData, "*.*",
                SearchOption.AllDirectories).Where(s => s.EndsWith(".upk",
                    StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".u", StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".sfm", StringComparison.OrdinalIgnoreCase)).ToList();
                packageFiles.AddRange(Directory.GetFiles(DLCData, "*.*",
                    SearchOption.AllDirectories).Where(s => s.EndsWith(".upk",
                        StringComparison.OrdinalIgnoreCase) ||
                        s.EndsWith(".u", StringComparison.OrdinalIgnoreCase) ||
                        s.EndsWith(".sfm", StringComparison.OrdinalIgnoreCase)));
                packageFiles.RemoveAll(s => s.Contains("LocalShaderCache-PC-D3D-SM3.upk"));
                packageFiles.RemoveAll(s => s.Contains("RefShaderCache-PC-D3D-SM3.upk"));
            }
            else if (gameType == MeType.ME2_TYPE)
            {
                packageFiles = Directory.GetFiles(MainData, "*.pcc", SearchOption.AllDirectories).ToList();
                packageFiles.AddRange(Directory.GetFiles(DLCData, "*.pcc", SearchOption.AllDirectories));
            }
            else if (gameType == MeType.ME3_TYPE)
            {
                if (!Directory.Exists(DLCDataCache))
                {
                    MessageBox.Show("DLCCache directory is missing, you need exract DLC packages first.");
                    return false;
                }
                packageFiles = Directory.GetFiles(MainData, "*.pcc", SearchOption.AllDirectories).ToList();
                if (Directory.Exists(DLCDataCache))
                    packageFiles.AddRange(Directory.GetFiles(DLCDataCache, "*.pcc", SearchOption.AllDirectories));
                packageFiles.RemoveAll(s => s.Contains("GuidCache"));
            }
            return true;
        }

        void ClosePackagesList()
        {
            packageFiles.Clear();
        }
    }
}
