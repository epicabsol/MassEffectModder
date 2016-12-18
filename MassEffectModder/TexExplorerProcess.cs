/*
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

using AmaroK86.ImageFormat;
using StreamHelpers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MassEffectModder
{
    public partial class TexExplorer : Form
    {
        private void extractTextureMod(string filenameMod, string outDir)
        {
            processTextureMod(filenameMod, -1, true, false, outDir);
        }

        private void previewTextureMod(string filenameMod, int previewIndex)
        {
            processTextureMod(filenameMod, previewIndex, false, false, "");
        }

        private void replaceTextureMod(string filenameMod)
        {
            processTextureMod(filenameMod, -1, false, true, "");
        }

        private void listTextureMod(string filenameMod)
        {
            processTextureMod(filenameMod, -1, false, false, "");
        }

        private void processTextureMod(string filenameMod, int previewIndex, bool extract, bool replace, string outDir)
        {
            using (FileStream fs = new FileStream(filenameMod, FileMode.Open, FileAccess.Read))
            {
                if (previewIndex == -1 && !extract && !replace)
                {
                    listViewTextures.BeginUpdate();
                }

                uint tag = fs.ReadUInt32();
                uint version = fs.ReadUInt32();
                if (tag != TextureModTag || version != TextureModVersion)
                {
                    MessageBox.Show("Not a Mod!");
                    return;
                }
                else
                {
                    uint gameType = fs.ReadUInt32();
                    if ((MeType)gameType != _gameSelected)
                    {
                        MessageBox.Show("Mod for different game!");
                        return;
                    }
                }
                int numTextures = fs.ReadInt32();
                for (int i = 0; i < numTextures; i++)
                {
                    string name;
                    uint crc, size, dstLen = 0, decSize = 0;
                    byte[] dst = null;
                    name = fs.ReadStringASCIINull();
                    crc = fs.ReadUInt32();
                    decSize = fs.ReadUInt32();
                    size = fs.ReadUInt32();
                    byte[] src = fs.ReadToBuffer(size);
                    dst = new byte[decSize];
                    dstLen = ZlibHelper.Zlib.Decompress(src, size, dst);

                    _mainWindow.updateStatusLabel("Processing MOD " + Path.GetFileNameWithoutExtension(filenameMod) +
                        " - Texture " + (i + 1) + " of " + numTextures + " - " + name);
                    if (extract)
                    {
                        string filename = name + "_" + string.Format("0x{0:X8}", crc) + ".dds";
                        using (FileStream output = new FileStream(Path.Combine(outDir, Path.GetFileName(filename)), FileMode.Create, FileAccess.Write))
                        {
                            output.Write(dst, 0, (int)dstLen);
                        }
                        continue;
                    }
                    if (previewIndex != -1)
                    {
                        if (i != previewIndex)
                        {
                            continue;
                        }
                        DDSImage image = new DDSImage(new MemoryStream(dst, 0, (int)dstLen));
                        pictureBoxPreview.Image = image.mipMaps[0].bitmap;
                        break;
                    }
                    else
                    {
                        FoundTexture foundTexture;
                        if (_gameSelected == MeType.ME1_TYPE)
                            foundTexture = _textures.Find(s => s.crc == crc && s.name == name);
                        else
                            foundTexture = _textures.Find(s => s.crc == crc);
                        if (foundTexture.crc != 0)
                        {
                            if (replace)
                            {
                                DDSImage image = new DDSImage(new MemoryStream(dst, 0, (int)dstLen));
                                if (!image.checkExistAllMipmaps())
                                {
                                    richTextBoxInfo.Text += "Not all mipmaps exists in texture: " + name + "\n";
                                    continue;
                                }
                                replaceTexture(image, foundTexture.list, foundTexture.name);
                            }
                            else
                            {
                                ListViewItem item = new ListViewItem(foundTexture.name + " (" + foundTexture.packageName + ")");
                                item.Name = i.ToString();
                                listViewTextures.Items.Add(item);
                            }
                        }
                        else
                        {
                            richTextBoxInfo.Text += "Not matched texture: " + name + "\n";
                        }
                    }
                }
                if (previewIndex == -1 && !extract && !replace)
                {
                    listViewTextures.EndUpdate();
                }
            }
        }

        void createTextureMod(string inDir, string outFile)
        {
            string[] files = Directory.GetFiles(inDir, "*.dds");

            int count = 0;
            using (FileStream outFs = new FileStream(outFile, FileMode.Create, FileAccess.Write))
            {
                outFs.WriteUInt32(TextureModTag);
                outFs.WriteUInt32(TextureModVersion);
                outFs.WriteUInt32((uint)_gameSelected);
                outFs.WriteInt32(0); // filled later
                for (int n = 0; n < files.Count(); n++)
                {
                    string file = files[n];
                    _mainWindow.updateStatusLabel("Processing MOD: " + Path.GetFileNameWithoutExtension(outFile));
                    string crcStr = Path.GetFileNameWithoutExtension(file);
                    if (crcStr.Contains("_0x"))
                    {
                        crcStr = Path.GetFileNameWithoutExtension(file).Split('_').Last().Substring(2, 8); // in case filename contain CRC 
                    }
                    else
                    {
                        crcStr = Path.GetFileNameWithoutExtension(file).Split('-').Last().Substring(2, 8); // in case filename contain CRC 
                    }
                    uint crc = uint.Parse(crcStr, System.Globalization.NumberStyles.HexNumber);
                    if (crc == 0)
                    {
                        richTextBoxInfo.Text += "Wrong format of texture filename: " + Path.GetFileName(file) + "\n";
                        continue;
                    }

                    string filename = Path.GetFileNameWithoutExtension(file);
                    int idx = filename.IndexOf(crcStr);
                    string name = filename.Substring(0, idx - "_0x".Length);

                    string textureName = "";
                    List<FoundTexture> foundCrcList = _textures.FindAll(s => s.crc == crc);
                    FoundTexture foundTexture = _textures.Find(s => s.crc == crc && s.name == name);
                    if (foundCrcList.Count == 0)
                    {
                        richTextBoxInfo.Text += "Texture not matched: " + Path.GetFileName(file) + "\n";
                        continue;
                    }

                    int savedCount = count;
                    for (int l = 0; l < foundCrcList.Count; l++)
                    {
                        if (foundTexture.crc == crc)
                        {
                            if (l > 0)
                                break;
                            textureName = foundTexture.name;
                        }
                        else
                        {
                            textureName = foundCrcList[l].name;
                        }
                        using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                        {
                            byte[] src = fs.ReadToBuffer((int)fs.Length);
                            DDSImage image = new DDSImage(new MemoryStream(src));
                            if (!image.checkExistAllMipmaps())
                            {
                                richTextBoxInfo.Text += "Texture does not have all mipmaps: " + Path.GetFileName(file) + "\n";
                                continue;
                            }

                            Package pkg = new Package(GameData.GamePath + foundCrcList[l].list[0].path);
                            Texture texture = new Texture(pkg, foundCrcList[l].list[0].exportID, pkg.getExportData(foundCrcList[l].list[0].exportID));

                            if (texture.mipMapsList.Count > 1 && image.mipMaps.Count() <= 1)
                            {
                                richTextBoxInfo.Text += "DDS file must have mipmaps: " + Path.GetFileName(file) + "\n";
                                continue;
                            }

                            DDSFormat ddsFormat = DDSImage.convertFormat(texture.properties.getProperty("Format").valueName);
                            if (image.ddsFormat != ddsFormat)
                            {
                                richTextBoxInfo.Text += "DDS file not match expected texture format: " + Path.GetFileName(file) + "\n";
                                continue;
                            }

                            if (image.mipMaps[0].origWidth / image.mipMaps[0].origHeight !=
                                texture.mipMapsList[0].width / texture.mipMapsList[0].height)
                            {
                                richTextBoxInfo.Text += "DDS file not match game data texture aspect ratio: " + Path.GetFileName(file) + "\n";
                                continue;
                            }

                            _mainWindow.updateStatusLabel2("Texture " + (n + 1) + " of " + files.Count() + ", Name: " + textureName);

                            byte[] dst = ZlibHelper.Zlib.Compress(src);
                            outFs.WriteStringASCIINull(textureName);
                            outFs.WriteUInt32(crc);
                            outFs.WriteInt32(src.Length);
                            outFs.WriteInt32(dst.Length);
                            outFs.WriteFromBuffer(dst);
                            count++;
                        }
                    }
                    if (count == savedCount)
                    {
                        continue;
                    }
                }
                outFs.SeekBegin();
                outFs.WriteUInt32(TextureModTag);
                outFs.WriteUInt32(TextureModVersion);
                outFs.WriteUInt32((uint)_gameSelected);
                outFs.WriteInt32(count);
            }
            if (count == 0)
                File.Delete(outFile);
        }

        public void extractTextureToDDS(string outputFile, string packagePath, int exportID)
        {
            Package package = new Package(packagePath);
            Texture texture = new Texture(package, exportID, package.getExportData(exportID));
            while (texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.empty))
            {
                texture.mipMapsList.Remove(texture.mipMapsList.First(s => s.storageType == Texture.StorageTypes.empty));
            }
            List<DDSImage.MipMap> mipmaps = new List<DDSImage.MipMap>();
            DDSFormat format = DDSImage.convertFormat(texture.properties.getProperty("Format").valueName);
            for (int i = 0; i < texture.mipMapsList.Count; i++)
            {
                mipmaps.Add(new DDSImage.MipMap(texture.getMipMapDataByIndex(i), format, texture.mipMapsList[i].width, texture.mipMapsList[i].height));
            }
            DDSImage dds = new DDSImage(mipmaps);
            if (File.Exists(outputFile))
                File.Delete(outputFile);
            using (FileStream fs = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write))
            {
                dds.SaveDDSImage(fs);
            }
        }
    }
}
