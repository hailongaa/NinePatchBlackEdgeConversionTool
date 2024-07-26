using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace NinePatchBlackEdgeConversionTool
{
    public class ImageToBlack
    {
        string ImagePath;
        /// <summary>
        /// 无黑边 NinePatch图 转有黑边 NinePatch图
        /// </summary>
        /// <param name="_ImagePath">无黑边 NinePatch图 的路径</param>
        public ImageToBlack(string _ImagePath)
        {
            ImagePath = _ImagePath;
        }
        /// <summary>
        /// 保存有黑边的NinePatch图
        /// </summary>
        /// <param name="outImage">保存路径</param>
        public void Save(string outImage)
        {
            try
            {
                using (FileStream fs = new FileStream(ImagePath, FileMode.Open, FileAccess.Read))
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    // 读取并验证PNG文件头
                    byte[] pngSignature = reader.ReadBytes(8);
                    string signature = Encoding.ASCII.GetString(pngSignature);
                    if (signature != "?PNG\r\n\u001a\n")
                    {
                        throw new Exception($"不是一个有效的PNG文件。{signature}");
                    }

                    // 读取Chunks并输出每个Chunk的名字
                    while (fs.Position < fs.Length)
                    {
                        // 读取Chunk的长度（前4字节）
                        uint chunkLength = ReadBigEndianUInt32(reader);
                        // 读取Chunk的类型（接下来的4字节）
                        string chunkType = Encoding.ASCII.GetString(reader.ReadBytes(4));
                        //if (chunkType == "npTc")
                        //{
                        // 输出Chunk的名字
                        Console.WriteLine($"Chunk 名字: {chunkType}");
                        // 读取Chunk的数据
                        byte[] chunkData = reader.ReadBytes((int)chunkLength);
                        if (chunkType == "npTc")
                        {
                            DataRegister.npTcData = chunkData;
                            NinePatch_ImageToBlack ninePatch = new NinePatch_ImageToBlack(DataRegister.npTcData);
                            using (Bitmap bitmap = new Bitmap(ImagePath))
                            {
                                using (Bitmap outBitmap = new Bitmap(bitmap.Width + 2, bitmap.Height + 2))
                                {
                                    using (Graphics g = Graphics.FromImage(outBitmap))
                                    {
                                        g.Clear(Color.Transparent);
                                        g.DrawImage(bitmap, 1, 1, bitmap.Width, bitmap.Height);
                                        Pen pen = new Pen(Color.Black, 1);
                                        g.DrawLine(pen, outBitmap.Width - 1, ninePatch.paddingTop + 1, outBitmap.Width - 1, outBitmap.Height - 2 - ninePatch.paddingBottom);
                                        g.DrawLine(pen, ninePatch.paddingLeft + 1, outBitmap.Height - 1, outBitmap.Width - 2 - ninePatch.paddingRight, outBitmap.Height - 1);
                                        foreach (var top in ninePatch.stretch.stretchTops)
                                        {
                                            g.DrawLine(pen, top.Left + 1, 0, top.Right, 0);
                                        }
                                        foreach (var left in ninePatch.stretch.stretchLefts)
                                        {
                                            g.DrawLine(pen, 0, left.Top + 1, 0, left.Bottom);
                                        }
                                        outBitmap.SetResolution(72, 72);
                                        outBitmap.Save(outImage, ImageFormat.Png);
                                    }
                                }
                            }
                        }
                        // 跳过当前Chunk的CRC（4字节）
                        reader.BaseStream.Seek(4, SeekOrigin.Current);

                        // 检查是否为结束标志
                        if (chunkType == "IEND")
                        {
                            Console.WriteLine("到达文件结束标志 IEND。");
                            break; // 结束循环，因为IEND Chunk表示文件的结束
                        }
                    }

                }


            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
        }

        private static uint ReadBigEndianUInt32(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(4);
            return (uint)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);
        }
    }
}
