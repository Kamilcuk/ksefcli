using System.Text;

using KSeF.Client.Api.Services;

using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace KCKSeFCli;


public static class AddQrToPdf
{

    public static string[] WrapText(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return [];
        }

        StringBuilder sb = new();
        for (int i = 0; i < input.Length; i++)
        {
            sb.Append(input[i]);
            if ((i + 1) % 80 == 0 && i != input.Length - 1)
            {
                sb.Append('\n');
            }
        }
        return sb.ToString().Split('\n');
    }

    // Helper to convert XGraphics Y to PDF Y
    private static double FlipY(double y, double height, PdfPage page)
    {
        return page.Height.Point - y - height;
    }

    public static byte[] AddQrCode(byte[] inputPdfBytes, string qrCodeUrl, string? label = null)
    {
        GlobalFontSettings.UseWindowsFontsUnderWindows = true;
        GlobalFontSettings.UseWindowsFontsUnderWsl2 = true;

        using MemoryStream inputStream = new MemoryStream(inputPdfBytes);
        using MemoryStream outputStream = new MemoryStream();
        byte[] qrCodeBytes = QrCodeService.GenerateQrCode(qrCodeUrl, 5);

        using (PdfDocument doc = PdfReader.Open(inputStream, PdfDocumentOpenMode.Modify))
        {
            PdfPage newPage = doc.AddPage();
            using (XGraphics gfx = XGraphics.FromPdfPage(newPage))
            {
                double currentY = 50;
                XFont font = new XFont("Arial", 7, XFontStyleEx.Regular);
                XFont linkFont = new XFont("Arial", 7, XFontStyleEx.Underline);
                XBrush brush = XBrushes.Black;
                XBrush linkBrush = XBrushes.Blue;

                // 1. ADD LABEL (MULTILINE)
                if (!string.IsNullOrEmpty(label))
                {
                    foreach (string line in WrapText(label))
                    {
                        XSize size = gfx.MeasureString(line, font);
                        gfx.DrawString(line, font, brush, (newPage.Width.Point - size.Width) / 2, currentY);
                        currentY += size.Height + 2;
                    }
                    currentY += 5;
                }

                // 2. ADD CLICKABLE URL TEXT
                string[] urlLines = WrapText(qrCodeUrl); // Użyj tej samej logiki 80 znaków
                foreach (string line in urlLines)
                {
                    XSize size = gfx.MeasureString(line, linkFont);
                    double xPos = (newPage.Width.Point - size.Width) / 2;

                    // Rysowanie tekstu
                    gfx.DrawString(line, linkFont, linkBrush, xPos, currentY);

                    // Nakładanie warstwy klikalnej na tę konkretną linię
                    XRect lineRect = new XRect(
    xPos,
    FlipY(currentY - size.Height, size.Height, newPage),  // flip
    size.Width,
    size.Height
);

                    newPage.AddWebLink(new PdfRectangle(lineRect), qrCodeUrl);

                    currentY += size.Height + 2;
                }
                currentY += 10;

                // 3. ADD QR CODE
                using MemoryStream stream = new MemoryStream(qrCodeBytes);
                using XImage img = XImage.FromStream(stream);
                double imageX = (newPage.Width.Point - img.PointWidth) / 2;
                gfx.DrawImage(img, imageX, currentY);

                // Opcjonalnie: Link również na obrazku QR
                XRect qrRect = new XRect(
   imageX,
   FlipY(currentY, img.PointHeight, newPage),  // flip
   img.PointWidth,
   img.PointHeight
);
                newPage.AddWebLink(new PdfRectangle(qrRect), qrCodeUrl);
            }
            doc.Save(outputStream);
        }
        return outputStream.ToArray();
    }
}
