using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SNESassetsWPF.Rendering
{
    public static class BitmapFactory
    {
        public static WriteableBitmap FromRenderResult(RenderResult r)
        {
            var wb = new WriteableBitmap(
                r.Width,
                r.Height,
                96, 96,
                PixelFormats.Bgra32,
                null);

            wb.WritePixels(
                new Int32Rect( 0 , 0 , r.Width , r.Height ) ,
                r.Buffer ,
                r.Width * 4 ,
                0 );

            return wb;
        }

        public static void SavePng(RenderResult r , string path)
        {
            var wb = FromRenderResult(r);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add( BitmapFrame.Create( wb ) );

            using var fs = File.Create(path);
            encoder.Save( fs );
        }
    }
}
