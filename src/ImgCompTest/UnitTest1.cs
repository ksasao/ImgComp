using imgcomp;
using NUnit.Framework;
using System;
using System.Reflection;

namespace Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestTransparent()
        {
            UInt16[] color = { 0, 0, 0, 0, 0 };
            UInt16[] mask = { 0, 0, 0, 0, 0 };
            Codec codec = new Codec();
            byte[] data = codec.Encode(1,5,color, mask);
            Assert.AreEqual(new byte[] { 1,0,5,0, 4 }, data);
        }
        [Test]
        public void TestTransparent64()
        {
            UInt16[] color = new UInt16[64];
            UInt16[] mask = new UInt16[64];
            Codec codec = new Codec();
            byte[] data = codec.Encode(1,64,color, mask);
            Assert.AreEqual(new byte[] { 1,0,64,0, 63 }, data);
        }

        [Test]
        public void TestTransparent65()
        {
            UInt16[] color = new UInt16[65];
            UInt16[] mask = new UInt16[65];
            Codec codec = new Codec();
            byte[] data = codec.Encode(1,65,color, mask);
            Assert.AreEqual(new byte[] {1,0,65,0, 63, 0 }, data);
        }
        [Test]
        public void TestTransparent128()
        {
            UInt16[] color = new UInt16[128];
            UInt16[] mask = new UInt16[128];
            Codec codec = new Codec();
            byte[] data = codec.Encode(1,65,color, mask);
            Assert.AreEqual(new byte[] { 1,0,65,0,63, 63 }, data);
        }
        [Test]
        public void TestGetColorDataLength()
        {
            UInt16[] color = new UInt16[] { 75, 75, 75, 75, 75 };
            UInt16[] mask = new UInt16[] { 255, 255, 255, 255, 255 };
            Codec codec = new Codec();

            // private method
            Type type = codec.GetType();
            MethodInfo methodInfo = type.GetMethod("GetColorDataLength", BindingFlags.NonPublic | BindingFlags.Instance);
            int len = (int)methodInfo.Invoke(codec, new object[] { color, mask, 0 });
            Assert.AreEqual(5, len);
        }
        [Test]
        public void TestGetColorDataLength2()
        {
            UInt16[] color = new UInt16[] { 75, 75, 75, 75, 75 };
            UInt16[] mask = new UInt16[] { 255, 255, 255, 255, 255 };
            Codec codec = new Codec();

            // private method
            Type type = codec.GetType();
            MethodInfo methodInfo = type.GetMethod("GetColorDataLength", BindingFlags.NonPublic | BindingFlags.Instance);
            int len = (int)methodInfo.Invoke(codec, new object[] { color, mask, 4 });
            Assert.AreEqual(1,len);
        }
        [Test]
        public void TestGetSameValueLength()
        {
            UInt16[] color = new UInt16[] { 75, 75, 75, 75, 75 };
            UInt16[] mask = new UInt16[] { 255, 255, 255, 255, 255 };
            Codec codec = new Codec();

            // private method
            Type type = codec.GetType();
            MethodInfo methodInfo = type.GetMethod("GetSameValueLength", BindingFlags.NonPublic | BindingFlags.Instance);
            int len = (int)methodInfo.Invoke(codec, new object[] { color, 0, 5 });
            Assert.AreEqual(5, len);
        }

        ////10xx xxxx 続く1つの値をx+1回繰り返す。リングバッファはそのまま
        [Test]
        public void TestSame5()
        {
            UInt16[] color = new UInt16[]{ 75,75,75,75,75 };
            UInt16[] mask = new UInt16[] { 255, 255, 255, 255, 255 };
            Codec codec = new Codec();
            byte[] data = codec.Encode(1,5,color, mask);
            Assert.AreEqual(new byte[] { 1,0,5,0,0x80 | (5-1), 75, 0 }, data);
        }
        [Test]
        public void TestSame64()
        {
            UInt16[] color = new UInt16[64];
            UInt16[] mask = new UInt16[64];
            for(int i = 0; i < color.Length; i++)
            {
                color[i] = 0xFFCC;
                mask[i] = 1;
            }
            Codec codec = new Codec();
            byte[] data = codec.Encode(1,5,color, mask);
            Assert.AreEqual(new byte[] { 1,0,5,0,0x80 | (64 - 1), 0xCC, 0xFF }, data);
        }
        [Test]
        public void TestSame65()
        {
            UInt16[] color = new UInt16[65];
            UInt16[] mask = new UInt16[65];
            for (int i = 0; i < color.Length; i++)
            {
                color[i] = 0xFFCC;
                mask[i] = 1;
            }
            Codec codec = new Codec();
            byte[] data = codec.Encode(1,65,color, mask);
            Assert.AreEqual(new byte[] { 1,0,65,0,0x80 | (64 - 1), 0xCC, 0xFF, 0xC0,0xCC,0xFF}, data);
        }
        [Test]
        public void TestSameTransParent()
        {
            UInt16[] color = new UInt16[10];
            UInt16[] mask = new UInt16[10];
            for (int i = 0; i < 5; i++)
            {
                color[i] = 0xFFCC;
                mask[i] = 1;
            }
            Codec codec = new Codec();
            byte[] data= codec.Encode(1,10,color, mask);
            Assert.AreEqual(new byte[] { 1,0,10,0,0x80 | (5 - 1), 0xCC, 0xFF, 5-1 }, data);
        }
        [Test]
        public void TestBuffered()
        {
            UInt16[] color = new UInt16[5] { 1, 2, 3, 4, 5 };
            UInt16[] mask = new UInt16[5] { 1, 1, 1, 1, 1 };
            Codec codec = new Codec();
            byte[] data = codec.Encode(1,5,color, mask);
            Assert.AreEqual(new byte[] { 1,0,5,0,0xC0 | (5 - 1), 1,0, 2,0, 3,0, 4,0, 5,0 }, data);
        }
        [Test]
        public void TestBufferedDouble()
        {
            UInt16[] color = new UInt16[11] { 1, 2, 3, 4, 5, 0, 1, 2, 3, 4, 5 };
            UInt16[] mask = new UInt16[11] { 1, 1, 1, 1, 1, 0,  1, 1, 1, 1, 1 };
            Codec codec = new Codec();
            byte[] data = codec.Encode(1,11,color, mask);
            Assert.AreEqual(new byte[] { 1,0,11,0, 0xC0 | (5 - 1), 1, 0, 2, 0, 3, 0, 4, 0, 5, 0, 0, 0x40 + (5-1) }, data);
        }
        [Test]
        public void TestBufferedDouble2()
        {
            UInt16[] color = new UInt16[13] { 1, 2, 3, 4, 5, 0, 1, 2, 3, 4, 5, 4, 5 };
            UInt16[] mask = new UInt16[13] { 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1 };
            Codec codec = new Codec();
            byte[] data = codec.Encode(1,13,color, mask);
            Assert.AreEqual(new byte[] { 1,0,13,0,0xC0 | (5 - 1), 1, 0, 2, 0, 3, 0, 4, 0, 5, 0, 0, 0x40 | (5 - 1), 0x40 | (3<<3)|(2-1)}, data); // 最後はリングバッファの3番目から2文字
        }
        [Test]
        public void TestBufferedDouble3()
        {
            UInt16[] color = new UInt16[14] { 1, 2, 3, 4, 5, 6, 6, 1, 2, 3, 4, 5, 4, 5 };
            UInt16[] mask = new UInt16[14] { 1, 1, 1, 1, 1, 1,1, 1, 1, 1, 1, 1, 1, 1 };
            Codec codec = new Codec();
            byte[] data = codec.Encode(1,14,color, mask);
            Assert.AreEqual(new byte[] { 1,0,14,0,
                0xC0 | (5 - 1), 1, 0, 2, 0, 3, 0, 4, 0, 5, 0,
                0x81, 6, 0, 
                0x40 | (5 - 1),
                0x40 | (3 << 3) | (2 - 1) }, data); // 最後はリングバッファの3番目から2文字
        }
    }
}