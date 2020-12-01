using System.Globalization;
using System.Net.Http;
using System.Collections.Generic;
using System;
using XiaWorld;
using SevenZip;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using System.Text;
using XiaSaveFileLib;
using Assets.USecurity;

namespace XiaSaveFileLib
{
    ///<summary>Main class of this library. Loads and saves Game Save Files the way the Game does.
    ///<para>Requires Assembly-CSharp.dll and Assembly-CSharp-firstpass.dll from the game files to run</para>
    ///<para>Note that a SaveGame that is run in teaching mode should not be loaded or saved or it will probably throw some errors </para> </summary> 
    public class SaveFileManager
    {
        ///<summary> Saves a xiaSaveFileLib.SaveFile to the string path
        ///<para>Note that path has to be the full path to where the file should be saved
        ///eg: ./Saves/MySave.save </para> </summary>
        public static async Task SaveFileAsync(SaveFile file, string path)
        {
            MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(file.FileContent));
            try
            {
                await BuildSaveFileAsync((Stream)memoryStream, path, file.Header);
            }
            catch (Exception e)
            {
                throw new XiaException("Failed to build save file \n" + e.Message, e);
            }
            finally
            {
                memoryStream.Dispose();
                memoryStream.Close();
            }
        }

        public static async Task SaveFileAsync(SaveFile file, Stream outStream)
        {
            var memory = new MemoryStream(Encoding.UTF8.GetBytes(file.FileContent));
            try
            {
                await BuildSaveFileAsync(memory, outStream, file.Header);
            }
            catch (Exception e)
            {
                throw new XiaException("Failed to build save file \n" + e.Message, e);
            }
            finally
            {
                memory.Dispose();
                memory.Close();
            }
        }

        private static async Task BuildSaveFileAsync(Stream st, string path, SaveFile.FileHeader header)
        {
            SaveMgr.SaveHead saveHead = new SaveMgr.SaveHead();
            saveHead.V = header.Version;
            saveHead.M = header.Mode;
            saveHead.Ms = header.Mods;
            saveHead.U = header.WriteTime;

            string head = Assets.USecurity.AES.Encrypt(JsonConvert.SerializeObject((object)saveHead), "bh89757");
            if (IntPtr.Size == 4)
                GC.Collect();
            await Task.Run(() => SevenZipHelper.Zip(st, path, head));
        }

        private static async Task BuildSaveFileAsync(Stream inStream, Stream outStream, SaveFile.FileHeader header)
        {
            var saveHead = new SaveMgr.SaveHead();
            saveHead.V = header.Version;
            saveHead.M = header.Mode;
            saveHead.Ms = header.Mods;
            saveHead.U = header.WriteTime;

            string head = Assets.USecurity.AES.Encrypt(JsonConvert.SerializeObject((object)saveHead), "bh89757");
            await Task.Run(() => SevenZipHelper.Zip(inStream, outStream, head));
        }

        ///<summary>Loads a xiaSaveFileLib.SaveFile object from the given path string.
        ///<para>head is the amount of bytes the Savehead object takes in the encoded file, should always be 2000 </para></summary>
        public static async Task<SaveFile> LoadSaveFileAsync(string path, int head = 2000)
        {
            string decodedPath = path.Replace(".save", ".osave");
            SaveMgr.SaveHead saveHead = new SaveMgr.SaveHead();

            try
            {
                saveHead = await DecodeAsync(path, decodedPath, head);
            }
            catch (Exception e)
            {
                File.Delete(decodedPath);
                throw new XiaException("Failed to decode File " + path, e);
            }

            SaveFile file = null;
            try
            {
                using (var reader = File.OpenText(decodedPath))
                {
                    var jsonString = await reader.ReadToEndAsync();
                    file = new SaveFile();
                    file.Header = new SaveFile.FileHeader()
                    {
                        Decompressed = saveHead.T,
                        Mode = saveHead.M,
                        Version = saveHead.V,
                        WriteTime = saveHead.U,
                        Mods = saveHead.Ms,
                        FileName = Path.GetFileNameWithoutExtension(decodedPath)
                    };
                    file.FileContent = jsonString;
                }

            }
            catch (Exception e)
            {
                throw new XiaException("Failed to parse file " + path, e);
            }
            finally { File.Delete(decodedPath); }

            return file;
        }

        public static async Task<SaveFile> LoadSaveFileAsync(Stream inStream, string fileName, int head = 2000)
        {
            var memory = new MemoryStream();
            var saveHead = await DecodeAsync(inStream, memory, head);

            String content;
            using (var reader = new StreamReader(memory))
                content = await reader.ReadToEndAsync();

            var file = new SaveFile()
            {
                Header = new SaveFile.FileHeader()
                {
                    Decompressed = saveHead.T,
                    Mode = saveHead.M,
                    Version = saveHead.V,
                    WriteTime = saveHead.U,
                    Mods = saveHead.Ms,
                    FileName = fileName
                },
                FileContent = content
            };

            return file;
        }

        public static async Task<string> GetSaveFileAsJson(string path, int head = 2000)
        {
            var file = await LoadSaveFileAsync(path, head);

            return JsonConvert.SerializeObject(file);
        }

        ///<summary>Decodes a file using SevenZip </summary>
        private static async Task<SaveMgr.SaveHead> DecodeAsync(string OriPath, string destPath, int head = 2000)
        {
            int len = 0;
            SaveMgr.SaveHead fileHead = await Task.Run(() => GameUlt.GetFileHead(OriPath, out len));
            head = len > head ? len : head;

            await Task.Run(() => SevenZipHelper.Unzip(OriPath, destPath, head));

            return fileHead;
        }

        private static async Task<SaveMgr.SaveHead> DecodeAsync(Stream inStream, Stream outStream, int head = 2000)
        {
            int len = 0;
            var fileHead = GetFileHead(inStream, out len);
            head = len > head ? len : head;

            await Task.Run(() => SevenZipHelper.Unzip(inStream, outStream, head));
            outStream.Position = 0;
            return fileHead;
        }

        private static SaveMgr.SaveHead GetFileHead(Stream stream, out int len)
        {
            var initialPosition = stream.Position;
            len = 0;
            List<byte> byteList = new List<byte>();
            for (int i = stream.ReadByte(); i > -1 && i != 0 && (i != 32 && i != 93 && i != 123); i = stream.ReadByte())
                byteList.Add((byte)i);
            len = byteList.Count;
            var saveHead = JsonConvert.DeserializeObject<SaveMgr.SaveHead>(AES.Decrypt2Str(Encoding.Default.GetString(byteList.ToArray()).TrimEnd(new char[1]), "bh89757"));
            stream.Position = initialPosition;

            return saveHead;
        }
    }
}