using System;
using System.Collections.Generic;
using System.IO;
using imgcomp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;

namespace png2bmp
{
    class Program
    {

        static void Main(string[] args)
        {
            if(args.Length < 1)
            {
                Console.WriteLine("imgcomp target_png_file [threshold(0-255)]");
                return;
            }
            try
            {
                int threshold = 128;
                if(args.Length == 2)
                {
                    threshold = System.Convert.ToInt32(threshold);
                }
                Codec codec = new Codec();
                string path = args[0];
                var data = codec.Encode(path, 128);
                File.WriteAllBytes(path + ".bin", data);
                (var width, var height, var colD, var maskD) = codec.Decode(data);
                SaveBmp565(path + ".bmp", width, height, colD);
                SaveBmp565(path + ".m.bmp", width, height, maskD);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        static void SaveBmp565(string filename, int width, int height, UInt16[] img)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Create))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    int offset = 70;
                    int dataSize = width * height * 2;
                    int fileSize = offset + dataSize;
                    int resolution = 0x2e23;

                    // RGB565 Bitmap Header
                    bw.Write(new byte[] { 0x42, 0x4d });
                    bw.Write(fileSize);
                    bw.Write((Int32)0);
                    bw.Write(offset);
                    bw.Write((Int32)0x38);
                    bw.Write(width);
                    bw.Write(height);
                    bw.Write((Int16)1);
                    bw.Write((Int16)16);    // bit per pixel
                    bw.Write((Int32)3);     // why?
                    bw.Write(dataSize);
                    bw.Write(resolution);
                    bw.Write(resolution);
                    bw.Write(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF8, 0x00, 0x00,
                        0xE0, 0x07, 0x00,0x00,0x1F,0x00,0x00,0x00,0x00,0x00,0x00,0x00 });
                    for(int y=height-1; y>=0; y--)
                    {
                        for(int x=0; x<width; x++)
                        {
                            bw.Write(img[y*width+x]);
                        }
                    }
                }
            }
        }
    }
}
