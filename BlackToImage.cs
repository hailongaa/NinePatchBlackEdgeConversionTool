using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace NinePatchBlackEdgeConversionTool
{
    public class BlackToImage
    {
        string ImagePath;
        /// <summary>
        /// 有黑边 NinePatch图 转无黑边 NinePatch图
        /// </summary>
        /// <param name="_ImagePath">有黑边 NinePatch图 的路径</param>
        public BlackToImage(string _ImagePath)
        {
            ImagePath = _ImagePath;
        }
        public void Save(string outImage)
        {
            //获取顶部黑边的数据
            NinePatch_BlackToImage.stretch.stretchTops = GetTopStretch();
            //获取左侧黑边的数据
            NinePatch_BlackToImage.stretch.stretchLefts = GetLeftStretch();
            NinePatch_BlackToImage.numXDivs = Convert.ToByte((NinePatch_BlackToImage.stretch.stretchTops.Count * 2).ToString("X2"), 16);
            NinePatch_BlackToImage.numYDivs = Convert.ToByte((NinePatch_BlackToImage.stretch.stretchLefts.Count * 2).ToString("X2"), 16);
            NinePatch_BlackToImage.wasDeserialized = 0;
            NinePatch_BlackToImage.xDivsz = 0;
            NinePatch_BlackToImage.yDivsz = 0;
            GetPadding();
            List<Rectangle> ColorRectangle = GetRegions();
            List<byte[]> Colors = new List<byte[]>();
            foreach (Rectangle rectangle in ColorRectangle)
            {
                Colors.Add(HexStringToByteArray(GetColorFromRegion(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height)));
            }
            NinePatch_BlackToImage.numColors = Convert.ToByte((Colors.Count).ToString("X2"), 16);

            List<byte> bytes = new List<byte>();
            foreach (byte b in NinePatch_BlackToImage.header)
                bytes.Add(b);
            foreach (byte b in HexStringToByteArray(NinePatch_BlackToImage.xDivsz.ToString("X8")))
                bytes.Add(b);
            foreach (byte b in HexStringToByteArray(NinePatch_BlackToImage.yDivsz.ToString("X8")))
                bytes.Add(b);
            foreach (byte b in HexStringToByteArray(NinePatch_BlackToImage.paddingLeft.ToString("X8")))
                bytes.Add(b);
            foreach (byte b in HexStringToByteArray(NinePatch_BlackToImage.paddingRight.ToString("X8")))
                bytes.Add(b);
            foreach (byte b in HexStringToByteArray(NinePatch_BlackToImage.paddingTop.ToString("X8")))
                bytes.Add(b);
            foreach (byte b in HexStringToByteArray(NinePatch_BlackToImage.paddingBottom.ToString("X8")))
                bytes.Add(b);
            foreach (byte b in HexStringToByteArray((0).ToString("X8")))
                bytes.Add(b);
            foreach (stretchTop stretchTop in NinePatch_BlackToImage.stretch.stretchTops)
            {
                foreach (byte b in HexStringToByteArray(stretchTop.Left.ToString("X8")))
                    bytes.Add(b);
                foreach (byte b in HexStringToByteArray(stretchTop.Right.ToString("X8")))
                    bytes.Add(b);
            }
            foreach (stretchLeft stretchLeft in NinePatch_BlackToImage.stretch.stretchLefts)
            {
                foreach (byte b in HexStringToByteArray(stretchLeft.Top.ToString("X8")))
                    bytes.Add(b);
                foreach (byte b in HexStringToByteArray(stretchLeft.Bottom.ToString("X8")))
                    bytes.Add(b);
            }
            foreach (byte[] color in Colors)
                foreach (byte b in color)
                    bytes.Add(b);
            byte[] npTc = new byte[bytes.Count];
            for (int i = 0; i < bytes.Count; i++)
            {
                npTc[i] = bytes[i];
            }
            using (Bitmap src = new Bitmap(ImagePath))
            {
                using (Bitmap outimage = new Bitmap(src.Width - 2, src.Height - 2))
                {
                    using (Graphics g = Graphics.FromImage(outimage))
                    {
                        g.Clear(Color.Transparent);
                        g.DrawImage(src, -1, -1, src.Width, src.Height);
                        outimage.SetResolution(72, 72);
                        outimage.Save(outImage, ImageFormat.Png);
                    }
                }
                InsertNpTcChunk(outImage, outImage + "a.png", npTc);
            }
            File.Delete(outImage);
            File.Move(outImage + "a.png", outImage);
        }
        /// <summary>
        /// 将 npTc Chunk 插入到 PNG 文件中
        /// </summary>
        /// <param name="inputFilePath">输入 PNG 文件路径</param>
        /// <param name="outputFilePath">输出 PNG 文件路径</param>
        /// <param name="npTcData">npTc Chunk 的数据</param>
        private void InsertNpTcChunk(string inputFilePath, string outputFilePath, byte[] npTcData)
        {
            using (FileStream fs = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
            {
                byte[] pngHeader = new byte[8];
                fs.Read(pngHeader, 0, 8);

                using (FileStream outputFs = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
                {
                    // 写入 PNG 文件头部
                    outputFs.Write(pngHeader, 0, 8);

                    // 复制原始的 Chunk 直到找到 IEND Chunk
                    while (fs.Position < fs.Length)
                    {
                        byte[] lengthBytes = new byte[4];
                        byte[] typeBytes = new byte[4];

                        fs.Read(lengthBytes, 0, 4);
                        fs.Read(typeBytes, 0, 4);

                        uint chunkLength = BitConverter.ToUInt32(lengthBytes.Reverse().ToArray(), 0);
                        byte[] chunkData = new byte[chunkLength];
                        fs.Read(chunkData, 0, (int)chunkLength);
                        byte[] crcBytes = new byte[4];
                        fs.Read(crcBytes, 0, 4);

                        // 将原始的 Chunk 写入到输出文件
                        outputFs.Write(lengthBytes, 0, 4);
                        outputFs.Write(typeBytes, 0, 4);
                        outputFs.Write(chunkData, 0, (int)chunkLength);
                        outputFs.Write(crcBytes, 0, 4);

                        // 如果是 IEND Chunk，则在 IEND 之前插入 npTc Chunk
                        if (typeBytes.SequenceEqual(Encoding.ASCII.GetBytes("IEND")))
                        {
                            WriteChunk(outputFs, "npTc", npTcData);
                            break; // 插入 npTc Chunk 后，退出循环
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 将 Chunk 写入到 PNG 文件中
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="type">Chunk 类型</param>
        /// <param name="data">Chunk 数据</param>
        private static void WriteChunk(FileStream fs, string type, byte[] data)
        {
            byte[] typeBytes = Encoding.ASCII.GetBytes(type);
            byte[] lengthBytes = BitConverter.GetBytes(data.Length);
            Array.Reverse(lengthBytes);

            // 计算 CRC 校验
            byte[] crcBytes = CalculateCrc(typeBytes);

            // 写入 Chunk 的长度、类型、数据和 CRC
            fs.Write(lengthBytes, 0, 4);
            fs.Write(typeBytes, 0, 4);
            fs.Write(data, 0, data.Length);
            fs.Write(crcBytes, 0, 4);
        }
        /// <summary>
        /// 计算 CRC 校验值
        /// </summary>
        /// <param name="data">需要计算 CRC 的数据</param>
        /// <returns>CRC 校验值的字节数组</returns>
        private static byte[] CalculateCrc(byte[] data)
        {
            uint crc = 0xffffffff;
            uint[] crcTable = GenerateCrcTable();
            foreach (byte b in data)
            {
                uint tableIndex = (crc ^ b) & 0xff;
                crc = (crc >> 8) ^ crcTable[tableIndex];
            }
            crc ^= 0xffffffff;
            return BitConverter.GetBytes(crc).Reverse().ToArray();
        }
        /// <summary>
        /// 生成 CRC 校验表
        /// </summary>
        /// <returns>CRC 校验表</returns>
        private static uint[] GenerateCrcTable()
        {
            uint[] table = new uint[256];
            const uint polynomial = 0xedb88320;
            for (uint i = 0; i < 256; i++)
            {
                uint crc = i;
                for (uint j = 8; j > 0; j--)
                {
                    if ((crc & 1) == 1)
                        crc = (crc >> 1) ^ polynomial;
                    else
                        crc >>= 1;
                }
                table[i] = crc;
            }
            return table;
        }
        // 将16进制字符串转换为 byte[]
        private static byte[] HexStringToByteArray(string hex)
        {
            if (hex.Length % 2 != 0)
            {
                throw new ArgumentException("Hex string must have an even length.");
            }

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }
        private List<Rectangle> GetRegions()
        {
            using (Bitmap bitmap = new Bitmap(ImagePath))
            {
                int imageWidth = bitmap.Width - 2;
                int imageHeight = bitmap.Height - 2;
                List<Rectangle> regions = new List<Rectangle>();
                List<int> X = new List<int>();
                for (int x = -1; x <= NinePatch_BlackToImage.stretch.stretchTops.Count; x++)
                {
                    if (x == -1)
                    {
                        X.Add(0);
                    }
                    else if (x == NinePatch_BlackToImage.stretch.stretchTops.Count)
                    {
                        X.Add(imageHeight);
                    }
                    else
                    {
                        X.Add(NinePatch_BlackToImage.stretch.stretchTops[x].Left);
                        X.Add(NinePatch_BlackToImage.stretch.stretchTops[x].Right);
                    }
                }
                List<int> Y = new List<int>();
                for (int y = -1; y <= NinePatch_BlackToImage.stretch.stretchLefts.Count; y++)
                {
                    if (y == -1)
                    {
                        Y.Add(0);
                    }
                    else if (y == NinePatch_BlackToImage.stretch.stretchLefts.Count)
                    {
                        Y.Add(imageHeight);
                    }
                    else
                    {
                        Y.Add(NinePatch_BlackToImage.stretch.stretchLefts[y].Top);
                        Y.Add(NinePatch_BlackToImage.stretch.stretchLefts[y].Bottom);
                    }
                }
                for (int y = 0; y < Y.Count - 1; y++)
                {
                    for (int x = 0; x < X.Count - 1; x++)
                    {
                        regions.Add(new Rectangle(X[x], Y[y], X[x + 1] - X[x], Y[y + 1] - Y[y]));
                    }
                }

                return regions;
            }
        }
        private string GetColorFromRegion(int startX, int startY, int width, int height)
        {
            using (Bitmap bitmap1 = new Bitmap(ImagePath))
            using (Bitmap bitmap = new Bitmap(bitmap1.Width - 2, bitmap1.Height - 2))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                g.DrawImage(bitmap1, -1, -1, bitmap1.Width, bitmap1.Height);
                // 锁定图像的指定区域到系统内存
                Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                BitmapData bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                // 获取图像数据的起始地址
                IntPtr ptr = bitmapData.Scan0;

                // 计算图像数据的总字节数
                int bytes = Math.Abs(bitmapData.Stride) * bitmap.Height;

                // 创建一个字节数组来存储图像数据
                byte[] pixelValues = new byte[bytes];

                // 将图像数据复制到字节数组
                Marshal.Copy(ptr, pixelValues, 0, bytes);

                // 解锁图像
                bitmap.UnlockBits(bitmapData);

                // 记录像素信息
                bool hasTransparentPixel = false;
                Dictionary<int, int> colorCount = new Dictionary<int, int>();

                for (int y = startY; y < startY + height && y < bitmap.Height; y++)
                {
                    for (int x = startX; x < startX + width && x < bitmap.Width; x++)
                    {
                        int position = (y * bitmapData.Stride) + (x * 4);

                        byte blue = pixelValues[position];
                        byte green = pixelValues[position + 1];
                        byte red = pixelValues[position + 2];
                        byte alpha = pixelValues[position + 3];

                        // 检查是否有透明像素
                        if (alpha != 255)
                        {
                            hasTransparentPixel = true;
                        }

                        int argb = (alpha << 24) | (red << 16) | (green << 8) | blue;

                        if (colorCount.ContainsKey(argb))
                        {
                            colorCount[argb]++;
                        }
                        else
                        {
                            colorCount[argb] = 1;
                        }
                    }
                }

                if (hasTransparentPixel)
                {
                    return "00000001"; // 返回透明颜色编码
                }

                if (colorCount.Count == 1)
                {
                    // 如果只有一种颜色，返回其16进制编码
                    foreach (var color in colorCount.Keys)
                    {
                        return ColorToHex(Color.FromArgb(color));
                    }
                }

                return "00000001"; // 如果有多种颜色，返回透明颜色编码
            }
        }
        // 将Color对象转换为16进制字符串
        static string ColorToHex(Color color)
        {
            return $"{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }
        private void GetPadding()
        {
            using (Bitmap bitmap = new Bitmap(ImagePath))
            {
                // 获取图像宽度
                int width = bitmap.Width;
                // 获取图像高度
                int height = bitmap.Height;
                // 标记是否在黑色区域内
                bool inBlackRegion = false;
                // 记录黑色区域的起点和终点
                int startX = -1;
                int endX = -1;
                int startY = -1;
                int endY = -1;
                for (int x = 0; x < bitmap.Width; x++)
                {
                    // 获取当前像素的颜色
                    Color pixelColor = bitmap.GetPixel(x, height - 1);
                    // 判断是否是黑色像素（Alpha值为255且RGB值都为0）
                    if (pixelColor.A == 255 && pixelColor.R == 0 && pixelColor.G == 0 && pixelColor.B == 0)
                    {
                        // 如果之前不在黑色区域，现在进入黑色区域
                        if (!inBlackRegion)
                        {
                            // 记录黑色区域的起点
                            startX = x;
                            inBlackRegion = true;
                        }
                        // 更新黑色区域的终点
                        endX = x;
                    }
                    else
                    {
                        // 如果之前在黑色区域，现在离开黑色区域
                        if (inBlackRegion)
                        {
                            // 输出黑色区域的起点和终点坐标
                            NinePatch_BlackToImage.paddingLeft = Convert.ToByte((startX - 1).ToString("X2"), 16);
                            NinePatch_BlackToImage.paddingRight = Convert.ToByte((bitmap.Width - 2 - endX).ToString("X2"), 16);
                            // 重置起点和终点
                            startX = -1;
                            endX = -1;
                            inBlackRegion = false;
                        }
                    }
                }
                inBlackRegion = false;
                for (int y = 0; y < bitmap.Height; y++)
                {
                    // 获取当前像素的颜色
                    Color pixelColor = bitmap.GetPixel(width - 1, y);
                    // 判断是否是黑色像素（Alpha值为255且RGB值都为0）
                    if (pixelColor.A == 255 && pixelColor.R == 0 && pixelColor.G == 0 && pixelColor.B == 0)
                    {
                        // 如果之前不在黑色区域，现在进入黑色区域
                        if (!inBlackRegion)
                        {
                            // 记录黑色区域的起点
                            startY = y;
                            inBlackRegion = true;
                        }
                        // 更新黑色区域的终点
                        endY = y;
                    }
                    else
                    {
                        // 如果之前在黑色区域，现在离开黑色区域
                        if (inBlackRegion)
                        {
                            // 输出黑色区域的起点和终点坐标

                            NinePatch_BlackToImage.paddingTop = Convert.ToByte((startY - 1).ToString("X2"), 16);
                            NinePatch_BlackToImage.paddingBottom = Convert.ToByte((bitmap.Height - 2 - endY).ToString("X2"), 16);
                            // 重置起点和终点
                            startY = -1;
                            endY = -1;
                            inBlackRegion = false;
                        }
                    }
                }
            }
        }

        private List<stretchLeft> GetLeftStretch()
        {
            using (Bitmap bitmap = new Bitmap(ImagePath))
            {
                List<stretchLeft> stretchLefts = new List<stretchLeft>();
                // 获取图像宽度
                int height = bitmap.Height;
                // 标记是否在黑色区域内
                bool inBlackRegion = false;
                // 记录黑色区域的起点和终点
                int startY = -1;
                int endY = -1;
                for (int y = 0; y < bitmap.Height; y++)
                {
                    // 获取当前像素的颜色
                    Color pixelColor = bitmap.GetPixel(0, y);
                    // 判断是否是黑色像素（Alpha值为255且RGB值都为0）
                    if (pixelColor.A == 255 && pixelColor.R == 0 && pixelColor.G == 0 && pixelColor.B == 0)
                    {
                        // 如果之前不在黑色区域，现在进入黑色区域
                        if (!inBlackRegion)
                        {
                            // 记录黑色区域的起点
                            startY = y;
                            inBlackRegion = true;
                        }
                        // 更新黑色区域的终点
                        endY = y;
                    }
                    else
                    {
                        // 如果之前在黑色区域，现在离开黑色区域
                        if (inBlackRegion)
                        {
                            stretchLeft stretchLeft = new stretchLeft();
                            stretchLeft.Top = startY - 1;
                            stretchLeft.Bottom = endY;
                            stretchLefts.Add(stretchLeft);
                            // 输出黑色区域的起点和终点坐标

                            // 重置起点和终点
                            startY = -1;
                            endY = -1;
                            inBlackRegion = false;
                        }
                    }
                }
                return stretchLefts;
            }
        }

        private List<stretchTop> GetTopStretch()
        {
            using (Bitmap bitmap = new Bitmap(ImagePath))
            {
                List<stretchTop> stretchTops = new List<stretchTop>();
                // 获取图像宽度
                int width = bitmap.Width;
                // 标记是否在黑色区域内
                bool inBlackRegion = false;
                // 记录黑色区域的起点和终点
                int startX = -1;
                int endX = -1;
                for (int x = 0; x < bitmap.Width; x++)
                {
                    // 获取当前像素的颜色
                    Color pixelColor = bitmap.GetPixel(x, 0);
                    // 判断是否是黑色像素（Alpha值为255且RGB值都为0）
                    if (pixelColor.A == 255 && pixelColor.R == 0 && pixelColor.G == 0 && pixelColor.B == 0)
                    {
                        // 如果之前不在黑色区域，现在进入黑色区域
                        if (!inBlackRegion)
                        {
                            // 记录黑色区域的起点
                            startX = x;
                            inBlackRegion = true;
                        }
                        // 更新黑色区域的终点
                        endX = x;
                    }
                    else
                    {
                        // 如果之前在黑色区域，现在离开黑色区域
                        if (inBlackRegion)
                        {
                            stretchTop stretchTop = new stretchTop();
                            stretchTop.Left = startX - 1;
                            stretchTop.Right = endX;
                            stretchTops.Add(stretchTop);
                            // 输出黑色区域的起点和终点坐标

                            // 重置起点和终点
                            startX = -1;
                            endX = -1;
                            inBlackRegion = false;
                        }
                    }
                }
                return stretchTops;
            }
        }
    }
}
