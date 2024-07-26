# NinePatchBlackEdgeConversionTool
## c#程序将NinePatch图片从有黑边转换为无黑边，也可以从无黑边转换为有黑边
### 使用方法：
### 1、 有黑边 NinePatch图 转无黑边 NinePatch图
```cs
        BlackToImage blackToImage = new BlackToImage("你的有黑边 NinePatch图 的路径");
        blackToImage.Save("你的需要导出的无黑边 NinePatch图 的路径");
```
### 2、 无黑边 NinePatch图 转有黑边 NinePatch图
```cs
        ImageToBlack imageToBlack = new ImageToBlack("你的无黑边 NinePatch图 的路径");
        imageToBlack.Save("你的需要导出的有黑边 NinePatch图 的路径");
```
