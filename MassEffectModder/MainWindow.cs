/*
 * MassEffectModder
 *
 * Copyright (C) 2014-2017 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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

using StreamHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace MassEffectModder
{
    public partial class MainWindow : Form
    {
        const uint ExportModTag = 0x444F4D45;
        const uint ExportModVersion = 1;
        const uint ExportModHeaderLength = 16;

        public ConfIni _configIni;

        public MainWindow()
        {
            InitializeComponent();
            _configIni = new ConfIni();
        }

        public void enableGameDataMenu(bool enable)
        {
            toolStripMenuME1.Enabled = enable;
            toolStripMenuME2.Enabled = enable;
            toolStripMenuME3.Enabled = enable;
        }

        public void updateStatusLabel(string text)
        {
            toolStripStatusLabel.Text = text;
            Application.DoEvents();
        }

        public void updateStatusLabel2(string text)
        {
            toolStripStatusLabel2.Text = text;
            Application.DoEvents();
        }

        public TexExplorer CreateTextureExplorer(MeType type)
        {
            TexExplorer explorer = new TexExplorer(this, type);
            explorer.Text = "Mass Effect " + (int)type;
            explorer.MdiParent = this;
            explorer.WindowState = FormWindowState.Maximized;
            explorer.Show();
            return explorer;
        }

        private void massEffect1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            CreateTextureExplorer(MeType.ME1_TYPE).Run();
        }

        private void massEffect2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            CreateTextureExplorer(MeType.ME2_TYPE).Run();
        }

        private void massEffect3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            CreateTextureExplorer(MeType.ME3_TYPE).Run();
        }

        private void updateME1ConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME1_TYPE, _configIni);
            string path = gameData.EngineConfigIniPath;
            bool exist = File.Exists(path);
            if (!exist)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            ConfIni engineConf = new ConfIni(path);
            LODSettings.updateLOD(MeType.ME1_TYPE, engineConf);
            MessageBox.Show("Game configuration " + path + " updated." +
                "\n\nAfter this it's required to remove empty mipmaps to prevent black textures issue in the game.");
            enableGameDataMenu(true);
        }

        public bool GetPackages(GameData gameData)
        {
            if (!Directory.Exists(GameData.GamePath))
            {
                MessageBox.Show("Wrong game path!");
                return false;
            }
            updateStatusLabel("Finding packages in game data...");
            if (!gameData.getPackages())
            {
                MessageBox.Show("Unable get packages from game data.");
                updateStatusLabel("");
                return false;
            }
            if (GameData.gameType != MeType.ME1_TYPE)
            {
                if (!gameData.getTfcTextures())
                {
                    updateStatusLabel("");
                    return false;
                }
            }
            updateStatusLabel("Done.");
            return true;
        }

        private void replaceExportDataMod(MeType gameType)
        {
            GameData gameData = new GameData(gameType, _configIni);
            if (!Directory.Exists(GameData.GamePath))
            {
                MessageBox.Show("Wrong game path!");
                return;
            }
            using (OpenFileDialog modFile = new OpenFileDialog())
            {
                modFile.Title = "Please select Mod file";
                modFile.Filter = "MOD file | *.mem";
                if (modFile.ShowDialog() != DialogResult.OK)
                    return;
                updateStatusLabel("Processing mod: " + modFile.FileName);
                using (FileStream fs = new FileStream(modFile.FileName, FileMode.Open))
                {
                    uint tag = fs.ReadUInt32();
                    uint version = fs.ReadUInt32();
                    if (tag == TexExplorer.TextureModTag)
                    {
                        MessageBox.Show("This is textures Mod!");
                        return;
                    }
                    if (tag != ExportModTag || version != ExportModVersion)
                    {
                        MessageBox.Show("Mod not compatible!");
                        return;
                    }
                    else
                    {
                        if ((MeType)fs.ReadUInt32() != gameType)
                        {
                            MessageBox.Show("Mod for different game!");
                            return;
                        }
                        int numEntries = fs.ReadInt32();
                        for (int i = 0; i < numEntries; i++)
                        {
                            string package = fs.ReadStringASCIINull();
                            int expId = fs.ReadInt32();
                            uint uncSize = fs.ReadUInt32();
                            uint compSize = fs.ReadUInt32();
                            byte[] src = fs.ReadToBuffer(compSize);
                            byte[] dst = new byte[uncSize];
                            ZlibHelper.Zlib.Decompress(src, (uint)src.Length, dst);
                            string[] packages = Directory.GetFiles(GameData.MainData, package, SearchOption.AllDirectories);
                            if (packages.Count() != 0)
                            {
                                Package pkg = new Package(packages[0]);
                                pkg.setExportData(expId, dst);
                                pkg.SaveToFile();
                            }
                        }
                    }
                }
                updateStatusLabel("Mod applied");
            }
        }

        public void repackME12(MeType gametype)
        {
            GameData gameData = new GameData(gametype, _configIni);
            if (!Directory.Exists(GameData.GamePath))
            {
                MessageBox.Show("Wrong game path!");
                return;
            }
            GetPackages(gameData);
            for (int i = 0; i < GameData.packageFiles.Count; i++)
            {
                updateStatusLabel("Repack file " + (i + 1) + " of " + GameData.packageFiles.Count);
                Package package = new Package(GameData.packageFiles[i]);
                if (package.compressed && package.compressionType != Package.CompressionType.Zlib)
                    package.SaveToFile(true);
            }
            updateStatusLabel("Done");
            updateStatusLabel2("");
        }

        private void repackME1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            repackME12(MeType.ME1_TYPE);
            enableGameDataMenu(true);
        }

        private void modME1ExportDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            replaceExportDataMod(MeType.ME1_TYPE);
            enableGameDataMenu(true);
        }

        private void repackME2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            repackME12(MeType.ME2_TYPE);
            enableGameDataMenu(true);
        }

        private void updateME2ConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME2_TYPE, _configIni);
            string path = gameData.EngineConfigIniPath;
            bool exist = File.Exists(path);
            if (!exist)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            ConfIni engineConf = new ConfIni(path);
            LODSettings.updateLOD(MeType.ME2_TYPE, engineConf);
            MessageBox.Show("Game configuration " + path + " updated." +
                "\n\nAfter this it's required to remove empty mipmaps to prevent black textures issue in the game.");
            enableGameDataMenu(true);
        }

        private void modME2ExportDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            replaceExportDataMod(MeType.ME2_TYPE);
            enableGameDataMenu(true);
        }

        private void extractME3DLCPackagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME3_TYPE, _configIni);
            if (!Directory.Exists(GameData.GamePath))
            {
                MessageBox.Show("Wrong game path!");
                return;
            }
            if (!Directory.Exists(GameData.DLCData))
            {
                MessageBox.Show("There is nothing to unpack.");
                return;
            }
            ME3DLC.unpackAllDLC(this, null);
            updateStatusLabel("Done");
            updateStatusLabel2("");
            enableGameDataMenu(true);
        }

        private void PackME3DLC(string inPath, string DLCname)
        {
            GameData gameData = new GameData(MeType.ME3_TYPE, _configIni);
            if (!Directory.Exists(GameData.GamePath))
            {
                MessageBox.Show("Wrong game path!");
                return;
            }
            string outPath = Path.Combine(Path.Combine(GameData.GamePath, "BIOGame", "DLCTemp"), DLCname, "CookedPCConsole", "Default.sfar");
            ME3DLC dlc = new ME3DLC(this);
            dlc.fullRePack(inPath, outPath, DLCname);
        }

        private void PackAllME3DLC()
        {
            GameData gameData = new GameData(MeType.ME3_TYPE, _configIni);
            if (!Directory.Exists(GameData.GamePath))
            {
                MessageBox.Show("Wrong game path!");
                return;
            }
            if (!Directory.Exists(GameData.DLCData))
            {
                MessageBox.Show("There is nothing to pack.");
                return;
            }
            List<string> dlcs = Directory.GetFiles(GameData.DLCData, "Mount.dlc", SearchOption.AllDirectories).ToList();
            if (dlcs.Count() == 0)
            {
                MessageBox.Show("There is nothing to pack.");
                return;
            }
            List<string> DLCs = Directory.GetDirectories(GameData.DLCData).ToList();
            for (int i = 0; i < DLCs.Count; i++)
            {
                List<string> files = Directory.GetFiles(DLCs[i], "Mount.dlc", SearchOption.AllDirectories).ToList();
                if (files.Count == 0)
                    DLCs.RemoveAt(i--);
            }
            long diskFreeSpace = Misc.getDiskFreeSpace(GameData.GamePath);
            long diskUsage = 0;
            for (int i = 0; i < DLCs.Count; i++)
            {
                diskUsage += Misc.getDirectorySize(DLCs[i]);
            }
            diskUsage = (long)(diskUsage / 1.5);
            if (diskUsage < diskFreeSpace)
            {
                for (int i = 0; i < DLCs.Count; i++)
                {
                    string DLCname = Path.GetFileName(DLCs[i]);
                    updateStatusLabel("SFAR packing - DLC " + (i + 1) + " of " + DLCs.Count);
                    PackME3DLC(DLCs[i], DLCname);
                }

                string tmpDlcDir = Path.Combine(GameData.GamePath, "BIOGame", "DLCTemp");
                DLCs = Directory.GetFiles(GameData.DLCData, "Default.sfar", SearchOption.AllDirectories).ToList();
                for (int i = 0; i < DLCs.Count; i++)
                {
                    if (new FileInfo(DLCs[i]).Length <= 32)
                    {
                        string source = Path.GetDirectoryName(Path.GetDirectoryName(DLCs[i]));
                        Directory.Move(source, tmpDlcDir + "\\" + Path.GetFileName(source));
                    }
                }

                Directory.Delete(GameData.DLCData, true);
                Directory.Move(tmpDlcDir, GameData.DLCData);
                updateStatusLabel("Done");
            }
            else
            {
                MessageBox.Show("You need about " + Misc.getBytesFormat(diskUsage) + " free disk space");
            }
            updateStatusLabel2("");
            enableGameDataMenu(true);
        }

        private void packME3DLCPackagesLZMAToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            PackAllME3DLC();
            enableGameDataMenu(true);
        }

        private void updateME3ConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME3_TYPE, _configIni);
            string path = gameData.EngineConfigIniPath;
            bool exist = File.Exists(path);
            if (!exist)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            ConfIni engineConf = new ConfIni(path);
            LODSettings.updateLOD(MeType.ME3_TYPE, engineConf);
            MessageBox.Show("Game configuration " + path + " updated." +
                "\n\nAfter this it's required to remove empty mipmaps to prevent black textures issue in the game.");
            enableGameDataMenu(true);
        }

        private void modME3ExportDataToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            replaceExportDataMod(MeType.ME3_TYPE);
            enableGameDataMenu(true);
        }

        private void changeGamePathME1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME1_TYPE, _configIni, true);
            enableGameDataMenu(true);
        }

        private void changeGamePathME2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME2_TYPE, _configIni, true);
            enableGameDataMenu(true);
        }

        private void changeGamePathME3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME3_TYPE, _configIni, true);
            enableGameDataMenu(true);
        }

        private void removeLODSetME1MenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME1_TYPE, _configIni);
            string path = gameData.EngineConfigIniPath;
            bool exist = File.Exists(path);
            if (exist)
            {
                ConfIni engineConf = new ConfIni(path);
                LODSettings.removeLOD(MeType.ME1_TYPE, engineConf);
                MessageBox.Show("Game configuration: " + path + " updated.");
            }
            else
            {
                MessageBox.Show("Game configuration: " + path + " not exist, nothing done.");
            }
            enableGameDataMenu(true);
        }

        private void removeLODSetME2MenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME2_TYPE, _configIni);
            string path = gameData.EngineConfigIniPath;
            bool exist = File.Exists(path);
            if (exist)
            {
                ConfIni engineConf = new ConfIni(path);
                LODSettings.removeLOD(MeType.ME2_TYPE, engineConf);
                MessageBox.Show("Game configuration: " + path + " updated.");
            }
            else
            {
                MessageBox.Show("Game configuration: " + path + " not exist, nothing done.");
            }
            enableGameDataMenu(true);
        }

        private void removeLODSetME3MenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME2_TYPE, _configIni);
            string path = gameData.EngineConfigIniPath;
            bool exist = File.Exists(path);
            if (exist)
            {
                ConfIni engineConf = new ConfIni(path);
                LODSettings.removeLOD(MeType.ME3_TYPE, engineConf);
                MessageBox.Show("Game configuration: " + path + " updated.");
            }
            else
            {
                MessageBox.Show("Game configuration: " + path + " not exist, nothing done.");
            }
            enableGameDataMenu(true);
        }

        private void toolStripMenuItemUpdateTOCs_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME3_TYPE, _configIni);
            if (Directory.Exists(GameData.GamePath))
            {
                CachePackageMgr.updateMainTOC();
                CachePackageMgr.updateDLCsTOC();
            }
            else
            {
                MessageBox.Show("Wrong game path!");
            }

            enableGameDataMenu(true);
        }

        void removeTreeFile(MeType game)
        {
            enableGameDataMenu(false);
            DialogResult result = MessageBox.Show("This operation removing textures scan file to allow re-scan game data." +
            "\n\nAfter that you need restore game to vanilla state and install any original/modded DLC files before re-scan." +
            "\n\nAre you really sure?", "Remove Textures Scan File", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                GameData gameData = new GameData(game, _configIni);
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        Assembly.GetExecutingAssembly().GetName().Name);
                string filename = Path.Combine(path, "me" + (int)GameData.gameType + "map.bin");
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                    MessageBox.Show("File deleted.");
                }
                else
                {
                    MessageBox.Show("File not found.");
                }
            }
            enableGameDataMenu(true);
        }

        private void toolStripME1RemoveTreeMenuItem_Click(object sender, EventArgs e)
        {
            removeTreeFile(MeType.ME1_TYPE);
        }

        private void toolStripME2RemoveTreeMenuItem_Click(object sender, EventArgs e)
        {
            removeTreeFile(MeType.ME2_TYPE);
        }

        private void toolStripME3RemoveTreeMenuItem_Click(object sender, EventArgs e)
        {
            removeTreeFile(MeType.ME3_TYPE);
        }
    }
}
