using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RootmotionTool
{
    public class BND4Extract
    {
        public class ExtractedFile
        {
            public BND4.File File { get; set; }

            public string Path { get; set; }
 
            public ExtractedFile()
            {
            }

            public ExtractedFile(BND4.File bnd4File)
            {
                File = bnd4File;
            }

            public ExtractedFile(string path)
            {
                Path = path;
            }

            public ExtractedFile(BND4.File bnd4File, string path)
            {
                File = bnd4File;
                Path = path;
            }
        }

        BND4Extract()
        {
        }

        public static ExtractedFile Extract(BND4 bndfile, int ID, string filePath)
        {
            var file = new ExtractedFile(bndfile.Files.First(f => f.ID == ID), filePath);
            System.IO.File.WriteAllBytes(file.Path, file.File.Bytes);
            return file;
        }
    }
}
