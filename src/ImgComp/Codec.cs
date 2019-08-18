using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace imgcomp
{
    public class Codec
    {
        UInt16[] ImageToRGB565(Image<Rgba32> img)
        {
            UInt16[] data = new UInt16[img.Width * img.Height];
            int p = 0;
            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    int val = (img[x, y].B >> 3) | ((img[x, y].G >> 2) << 5) | ((img[x, y].R >> 3) << 11);
                    data[p++] = (UInt16)(val & 0xFFFF);
                }
            }
            return data;
        }
        public byte[] Encode(string filename, int threshold)
        {
            string dir = Path.GetDirectoryName(filename);
            string name = Path.GetFileNameWithoutExtension(filename);
            string imagePath = Path.Combine(dir, name + ".bmp");
            string maskPath = Path.Combine(dir, name + ".m.bmp");

            using (Image<Rgba32> img = Image.Load(filename))
            {
                UInt16 width = (UInt16)img.Width;
                UInt16 height = (UInt16)img.Height;

                using (var maskImg = img.Clone())
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int mask = img[x, y].A;
                            if (mask >= threshold)
                            {
                                maskImg[x, y] = NamedColors<Rgba32>.White;
                                img[x, y] = new Rgba32(img[x, y].R, img[x, y].G, img[x, y].B, 255);
                            }
                            else
                            {
                                maskImg[x, y] = NamedColors<Rgba32>.Black;
                                img[x, y] = new Rgba32(0, 0, 0, 255);
                            }
                        }
                    }
                    var colorData = ImageToRGB565(img);
                    var maskData = ImageToRGB565(maskImg);
                    return Encode(width, height, colorData, maskData);
                }
            }
        }

        public (UInt16 width, UInt16 height, UInt16[] colorData, UInt16[] maskData) Decode(byte[] encoded)
        {
            List<UInt16> color = new List<UInt16>();
            List<UInt16> mask = new List<UInt16>();
            UInt16[] ringBuffer = new UInt16[8];
            int ringBufferPointer = 0;

            UInt16 width = (UInt16)(encoded[0] | encoded[1] << 8);
            UInt16 height = (UInt16)(encoded[2] | encoded[3] << 8);

            int p = 4;
            while (p < encoded.Length)
            {
                byte d =(byte)(encoded[p] & 0x3F);
                switch (encoded[p] >> 6)
                {
                    case 0:
                        for(int i=0; i<d+1; i++)
                        {
                            color.Add(0);
                            mask.Add(0);
                        }
                        p = p + 1;
                        break;
                    case 1:
                        int rp = d >> 3;
                        int rl = d & 7;
                        for(int i=0; i < rl + 1; i++)
                        {
                            color.Add(ringBuffer[(rp + i) % ringBuffer.Length]);
                            mask.Add(0xFFFF);
                        }
                        p = p + 1;
                        break;
                    case 2:
                        UInt16 c = (UInt16)(encoded[p + 1] | (encoded[p + 2] << 8));
                        for(int i=0; i< d+1; i++)
                        {
                            color.Add(c);
                            mask.Add(0xFFFF);
                        }
                        p = p + 3;
                        break;
                    case 3:
                        p++;
                        for(int i=0; i < d + 1; i++)
                        {
                            UInt16 tmp = (UInt16)(encoded[p] | (encoded[p + 1] << 8));
                            color.Add(tmp);
                            mask.Add(0xFFFF);
                            ringBuffer[ringBufferPointer] = tmp;
                            ringBufferPointer = (ringBufferPointer + 1) % ringBuffer.Length;
                            p += 2;
                        }
                        break;
                    default:
                        break;
                }
            }
            return (width, height, color.ToArray(), mask.ToArray());
        }
        //16bit色データ,1ビットマスクデータをいい感じに圧縮する
        //* リングバッファは0で初期化する
        //次のデータへのポインタ(0の時終了)
        //下記の処理単位の回数
        //00xx xxxx x+1個透明が続く
        //01xxx yyy 展開したリングバッファのx番目から連続するy+1ピクセルを展開する
        //10xx xxxx 続く1つの値をx+1回繰り返す。リングバッファはそのまま
        //11xx xxxx 続くx+1個をそのまま展開するとともにリングバッファに展開する
        public byte[] Encode(UInt16 width, UInt16 height, UInt16[] colorData, UInt16[] maskData)
        {
            UInt16[] ringBuffer = new UInt16[8];
            int ringBufferPointer = 0;
            int p = 0;
            int commandCount = 0;
            List<byte> result = new List<byte>();
            result.Add((byte)(width & 0xFF));
            result.Add((byte)(width >> 8));
            result.Add((byte)(height & 0xFF));
            result.Add((byte)(height >> 8));
            int len = colorData.Length;
            while (p < len)
            {
                // 透明だった場合は何個連続するかを数える
                if (maskData[p] == 0)
                {
                    int c = 1;
                    for (int i = 1; i <64; i++)
                    {
                        if (p + i < len && maskData[p + i] == 0)
                        {
                            c++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    result.Add((byte)((c - 1) & 0x3F));
                    p += c;
                    commandCount++;
                    continue;
                }
                // 連続する不透明な部分の長さを調べる
                int colLen = GetColorDataLength(colorData, maskData, p);

                // 同じ値が連続していないかを調べる
                int sameLen = GetSameValueLength(colorData, p, colLen);
                if (sameLen > 1)
                {

                    result.Add((byte)(0x80 | (sameLen - 1)));
                    result.Add((byte)(colorData[p] & 0xFF));
                    result.Add((byte)(colorData[p] >> 8));
                    p += sameLen;
                    commandCount++;
                    continue;
                }

                // リングバッファに一致する部分があるかどうかを探す
                (int rPos, int rLen) = SearchRingBuffer(ringBuffer, colorData, p, colLen);
                if (rLen > 0)
                {
                    result.Add((byte)(0x40 | (rPos << 3) | (rLen - 1)));
                    p += rLen;
                    commandCount++;
                    continue;
                }

                // リングバッファに格納する。長さ1のものはここで扱う。
                if (colLen > 64)
                {
                    colLen = 64;
                }

                result.Add((byte)(0xC0 | (colLen - 1)));
                commandCount++;
                for (int i = 0; i < colLen; i++)
                {
                    result.Add((byte)(colorData[p] & 0xFF));
                    result.Add((byte)(colorData[p] >> 8));
                    ringBuffer[ringBufferPointer] = colorData[p];
                    ringBufferPointer = (ringBufferPointer + 1) % ringBuffer.Length;
                    p++;
                }
            }
            return result.ToArray();
        }
        int GetSameValueLength(UInt16[] colorData, int position, int maxLen)
        {
            int count = 1;
            for (int i = position+1; i < colorData.Length && count < 64 && count < maxLen; i++)
            {
                if (colorData[position] != colorData[i])
                {
                    break;
                }
                count++;
            }
            return count;
        }
        int GetColorDataLength(UInt16[] colorData, UInt16[] maskData, int position)
        {
            int count = 0;
            for (int i = position; i < maskData.Length; i++)
            {
                if (maskData[i] == 0)
                {
                    break;
                }
                // 途中から連続する色が出てくる場合は手前で打ち切る
                if (i > position && colorData[position] != colorData[i] && colorData[i-1] == colorData[i])
                {
                    count--;
                    break;
                }
                count++;
            }
            return count;
        }
        (int, int) SearchRingBuffer(UInt16[] ringBuffer, UInt16[] colorData, int position, int maxLen)
        {
            int len = 8;
            if (maxLen < len)
            {
                len = maxLen;
            }
            for (int i = len; i >= 1; i--)
            {
                for (int j = 0; j < ringBuffer.Length; j++)
                {
                    bool match = true;
                    for (int k = 0; k < i; k++)
                    {
                        if (ringBuffer[(j + k) % ringBuffer.Length] != colorData[position + k])
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                    {
                        return (j, i);
                    }
                }
            }
            return (0, 0);
        }
    }
}
