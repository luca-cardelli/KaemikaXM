using System;
using System.Collections.Generic;
using System.Text;
using SkiaSharp; // System.Drawing.Image -> SkiaSharp.SKImage
using System.IO;
using System.Diagnostics.Contracts;

namespace QuickGraph
{
    public static class QuickGraphResourceManager
    {
        public static SKImage GetLogo()
        {
            return GetImage("quickgraph");
        }

        public static SKImage GetBanner()
        {
            return GetImage("quickgraph.banner");
        }

        private static SKImage GetImage(string name)
        {
            Contract.Requires(name != null);

            throw new Exception("###REQUIRESCONVERSION");
            //using (Stream stream = typeof(QuickGraphResourceManager).Assembly.GetManifestResourceStream(String.Format("QuickGraph.{0}.png", name)))
            //    return SKImage.FromStream(stream);
        }

        public static void DumpResources(string path)
        {
            Contract.Requires(path != null);

            throw new Exception("###REQUIRESCONVERSION");
            //GetLogo().Save(Path.Combine(path, "quickgraph.png"), System.Drawing.Imaging.ImageFormat.Png);
            //GetBanner().Save(Path.Combine(path, "quickgraph.banner.png"), System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}
