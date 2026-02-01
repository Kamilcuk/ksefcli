using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;

namespace KSeFCli;

public class DateConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string s)
        {
            // Try parsing using standard C# DateTime.Parse
            if (DateTime.TryParse(s, out DateTime result))
            {
                return result;
            }

            // Try parsing with specific formats if standard parsing fails
            string[] formats = {
                "yyyy-MM-dd",
                "yyyy-MM-dd HH:mm:ss",
                "dd-MM-yyyy",
                "dd-MM-yyyy HH:mm:ss",
                "yyyy/MM/dd",
                "yyyy/MM/dd HH:mm:ss"
            };
            if (DateTime.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                return result;
            }

            // If C# parsing fails, try using the 'date' shell command for relative dates
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "date",
                    Arguments = $@"-d ""{s}"" +%s",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start date process.");
                process.WaitForExit(); // Synchronous for TypeConverter

                if (process.StandardOutput is null)
                {
                    throw new InvalidOperationException("Failed to access StandardOutput from date process.");
                }
                string output = process.StandardOutput.ReadToEnd(); // Synchronous for TypeConverter
                if (long.TryParse(output.Trim(), out long unixTimestamp))
                {
                    DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
                    return dateTimeOffset.LocalDateTime; // Convert to local time
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error parsing date with shell command: {ex.Message}");
            }

            throw new FormatException($"Could not parse date string: {s}");
        }

        return base.ConvertFrom(context, culture, value);
    }
}
