# NinePatchBlackEdgeConversionTool
c#程序将NinePatch图片从有黑边转换为无黑边，也可以从无黑边转换为有黑边\r\n
使用方法：\r\n
1、 有黑边 NinePatch图 转无黑边 NinePatch图\r\n
        BlackToImage blackToImage = new BlackToImage("你的有黑边 NinePatch图 的路径");\r\n
        blackToImage.Save("你的需要导出的无黑边 NinePatch图 的路径");\r\n
1、 无黑边 NinePatch图 转有黑边 NinePatch图\r\n
        ImageToBlack imageToBlack = new ImageToBlack("你的无黑边 NinePatch图 的路径");\r\n
        imageToBlack.Save("你的需要导出的有黑边 NinePatch图 的路径");\r\n
