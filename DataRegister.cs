using System;
using System.Collections.Generic;

namespace NinePatchBlackEdgeConversionTool
{
    internal class DataRegister
    {
        internal static byte[] npTcData { set; get; }
    }
    internal class NinePatch_BlackToImage
    {
        //头部信息
        internal static byte wasDeserialized, numXDivs, numYDivs, numColors;
        internal static byte xDivsz, yDivsz, paddingLeft, paddingRight, paddingTop, paddingBottom;
        internal static stretch stretch = new stretch();
        internal static byte[] header
        {
            get
            {
                return new byte[] { wasDeserialized, numXDivs, numYDivs, numColors };
            }
        }
        internal List<string> colors = new List<string>();

    }
    internal class NinePatch_ImageToBlack
    {
        //头部信息
        internal byte wasDeserialized, numXDivs, numYDivs, numColors;
        internal int xDivsz, yDivsz, paddingLeft, paddingRight, paddingTop, paddingBottom;
        internal stretch stretch;
        internal byte[] header;
        internal NinePatch_ImageToBlack(byte[] data)
        {
            wasDeserialized = data[0];
            numXDivs = data[1];
            numYDivs = data[2];
            numColors = data[3];
            // 读取分割尺寸和填充
            xDivsz = Convert.ToInt32(data[4].ToString("X2") + data[5].ToString("X2") + data[6].ToString("X2") + data[7].ToString("X2"), 16); // 读取 xDivsz
            yDivsz = Convert.ToInt32(data[8].ToString("X2") + data[9].ToString("X2") + data[10].ToString("X2") + data[11].ToString("X2"), 16); // 读取 yDivsz
            paddingLeft = Convert.ToInt32(data[12].ToString("X2") + data[13].ToString("X2") + data[14].ToString("X2") + data[15].ToString("X2"), 16); // 读取 paddingLeft
            paddingRight = Convert.ToInt32(data[16].ToString("X2") + data[17].ToString("X2") + data[18].ToString("X2") + data[19].ToString("X2"), 16); // 读取 paddingRight
            paddingTop = Convert.ToInt32(data[20].ToString("X2") + data[21].ToString("X2") + data[22].ToString("X2") + data[23].ToString("X2"), 16); // 读取 paddingTop
            paddingBottom = Convert.ToInt32(data[24].ToString("X2") + data[25].ToString("X2") + data[26].ToString("X2") + data[27].ToString("X2"), 16); // 读取 paddingBottom
            stretch = GetStretch(data);
            header = Getheader();
        }

        private byte[] Getheader()
        {
            string retstring = wasDeserialized.ToString("X2") + numXDivs.ToString("X2") + numYDivs.ToString("X2") + numColors.ToString("X2");
            if (retstring.Length % 2 != 0)
            {
                throw new ArgumentException("Hex string must have an even length.");
            }

            byte[] byteArray = new byte[retstring.Length / 2];
            for (int i = 0; i < byteArray.Length; i++)
            {
                byteArray[i] = Convert.ToByte(retstring.Substring(i * 2, 2), 16);
            }

            return byteArray;
        }

        private stretch GetStretch(byte[] data)
        {
            int wei = 32;
            stretch ret = new stretch();
            List<stretchTop> Top = new List<stretchTop>();
            for (int i = 0; i < numXDivs / 2; i++)
            {
                Top.Add(new stretchTop()
                {
                    Left = Convert.ToInt32(data[wei + 0].ToString("X2") + data[wei + 1].ToString("X2") + data[wei + 2].ToString("X2") + data[wei + 3].ToString("X2"), 16),
                    Right = Convert.ToInt32(data[wei + 4].ToString("X2") + data[wei + 5].ToString("X2") + data[wei + 6].ToString("X2") + data[wei + 7].ToString("X2"), 16)
                });
                wei += 8;
            }
            ret.stretchTops = Top;
            List<stretchLeft> Left = new List<stretchLeft>();
            for (int i = 0; i < numYDivs / 2; i++)
            {
                Left.Add(new stretchLeft()
                {
                    Top = Convert.ToInt32(data[wei + 0].ToString("X2") + data[wei + 1].ToString("X2") + data[wei + 2].ToString("X2") + data[wei + 3].ToString("X2"), 16),
                    Bottom = Convert.ToInt32(data[wei + 4].ToString("X2") + data[wei + 5].ToString("X2") + data[wei + 6].ToString("X2") + data[wei + 7].ToString("X2"), 16)
                });
                wei += 8;
            }
            ret.stretchLefts = Left;
            return ret;
        }
    }
    internal class stretch
    {
        /// <summary>
        /// 顶部透明区域
        /// </summary>
        internal List<stretchTop> stretchTops = new List<stretchTop>();
        /// <summary>
        /// 左侧透明区域
        /// </summary>
        internal List<stretchLeft> stretchLefts = new List<stretchLeft>();
    }
    internal class stretchTop
    {
        internal int Left { set; get; }
        internal int Right { set; get; }
    }
    internal class stretchLeft
    {
        internal int Top { set; get; }
        internal int Bottom { set; get; }
    }
}
