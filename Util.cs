using DlibDotNet;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TestApplication
{
    public static class Util
    {
        public static Array2D<Byte> ToArray2D(this Bitmap bitmap)
        {
            Int32 stride;
            Byte[] data;
            Int32 width = bitmap.Width;
            Int32 height = bitmap.Height;
            using (Bitmap grayImage = MakeGrayscale3(bitmap))
            {
                BitmapData bits = grayImage.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
                stride = bits.Stride;
                Int32 length = stride * height;
                data = new Byte[length];
                Marshal.Copy(bits.Scan0, data, 0, length);
                grayImage.UnlockBits(bits);
            }
            Array2D<Byte> array = new Array2D<Byte>(height, width);
            Int32 offset = 0;
            for (Int32 y = 0; y < height; y++)
            {
                Int32 curOffset = offset;
                Array2D<Byte>.Row<Byte> curRow = array[y];
                for (Int32 x = 0; x < width; x++)
                {
                    curRow[x] = data[curOffset]; 
                    curOffset += 4;
                }
                offset += stride;
            }
            return array;
        }
        static Bitmap MakeGrayscale3(Bitmap original)
        {
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            using (Graphics g = Graphics.FromImage(newBitmap))
            {
                ColorMatrix colorMatrix = new ColorMatrix(
                   new float[][]
                   {
             new float[] {.3f, .3f, .3f, 0, 0},
             new float[] {.59f, .59f, .59f, 0, 0},
             new float[] {.11f, .11f, .11f, 0, 0},
             new float[] {0, 0, 0, 1, 0},
             new float[] {0, 0, 0, 0, 1}
                   });

                using (ImageAttributes attributes = new ImageAttributes())
                {

                    attributes.SetColorMatrix(colorMatrix);

                    g.DrawImage(original, new System.Drawing.Rectangle(0, 0, original.Width, original.Height),
                                0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
                }
            }
            return newBitmap;
        }
    }
}
