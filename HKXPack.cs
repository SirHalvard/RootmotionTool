using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RootmotionTool
{
    public class HKXPack
    {
        private Process HKXPackSouls { get; set; }
        public HKXPack(string hkxpacksoulsLocation)
        {
            HKXPackSouls = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = hkxpacksoulsLocation,
                    CreateNoWindow = true,
                    UseShellExecute = false
                }
            };
        }
        public string Unpack(string filePath)
        {
            PackUnpack(filePath);
            return Path.ChangeExtension(filePath, ".xml");
        }

        public string Pack(string filePath)
        {
            PackUnpack(filePath);
            return Path.ChangeExtension(filePath, ".hkx");
        }

        private void PackUnpack(string filePath)
        {
            HKXPackSouls.StartInfo.Arguments = filePath;
            HKXPackSouls.Start();
            HKXPackSouls.WaitForExit(120000);
            HKXPackSouls.Dispose();
        }
    }
}
