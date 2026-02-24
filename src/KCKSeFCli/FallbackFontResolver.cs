using System.Reflection;

using PdfSharp.Fonts;

namespace KCKSeFCli {
    public class FallbackFontResolver : IFontResolver {
        public FallbackFontResolver() {
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;
            GlobalFontSettings.UseWindowsFontsUnderWsl2 = true;
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream? stream = assembly.GetManifestResourceStream("KCKSeFCli.Resources.WorkSans-Regular.ttf");

            if (stream != null) {
                return new FontResolverInfo("WorkSans-Regular");
            }

            FontResolverInfo? fontResolverInfo = PlatformFontResolver.ResolveTypeface(familyName, isBold, isItalic);
            if (fontResolverInfo == null) {
                throw new System.Exception($"Font {familyName} not found.");
            }
            return fontResolverInfo;
        }

        public byte[] GetFont(string faceName) {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream? stream = assembly.GetManifestResourceStream("KCKSeFCli.Resources.WorkSans-Regular.ttf");

            if (stream != null) {
                using (BinaryReader reader = new BinaryReader(stream)) {
                    return reader.ReadBytes((int)stream.Length);
                }
            }

            return File.ReadAllBytes(faceName);
        }
    }
}
