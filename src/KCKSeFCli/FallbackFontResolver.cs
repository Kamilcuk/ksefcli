using PdfSharp.Fonts;
using System;
using System.IO;
using System.Reflection;

namespace KCKSeFCli
{
    public class FallbackFontResolver : IFontResolver
    {
        public FallbackFontResolver()
        {
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;
            GlobalFontSettings.UseWindowsFontsUnderWsl2 = true;
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream("KCKSeFCli.Resources.WorkSans-Regular.ttf");

            if (stream != null)
            {
                return new FontResolverInfo("WorkSans-Regular");
            }
            
            var fontResolverInfo = PlatformFontResolver.ResolveTypeface(familyName, isBold, isItalic);
            if (fontResolverInfo == null)
            {
                throw new System.Exception($"Font {familyName} not found.");
            }
            return fontResolverInfo;
        }

        public byte[] GetFont(string faceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream("KCKSeFCli.Resources.WorkSans-Regular.ttf");

            if (stream != null)
            {
                using (var reader = new BinaryReader(stream))
                {
                    return reader.ReadBytes((int)stream.Length);
                }
            }

            return File.ReadAllBytes(faceName);
        }
    }
}
