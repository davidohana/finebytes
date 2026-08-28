using Mfr.Models.Rename;

namespace Mfr.Models.RenameList.Fields.Basic
{
    /// <summary>
    /// First 1–10 digit run in the full file name (MFR7 BasicPG).
    /// </summary>
    public sealed class BasicFileNameNumericField()
        : BasicRenameListField(
            propertyKey: Key,
            displayName: "File Name Numeric Value",
            order: 6,
            defaultWidth: 50,
            supportsPreview: false
        )
    {
        /// <summary>
        /// MFR7 property key.
        /// </summary>
        public const string Key = "FileNameNumeric";

        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            return _FirstDigitRun(meta.Prefix + meta.Extension);
        }

        private static string _FirstDigitRun(string fullFileName)
        {
            for (var i = 0; i < fullFileName.Length; i++)
            {
                if (!char.IsAsciiDigit(fullFileName[i]))
                {
                    continue;
                }

                var end = i + 1;
                while (end < fullFileName.Length && char.IsAsciiDigit(fullFileName[end]) && end - i < 10)
                {
                    end++;
                }

                return long.Parse(fullFileName.AsSpan(i, end - i)).ToString();
            }

            return "0";
        }
    }
}
