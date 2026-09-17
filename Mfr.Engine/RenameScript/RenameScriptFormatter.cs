using System.Text;

namespace Mfr.Engine.RenameScript
{
    /// <summary>
    /// Formats rename-script IR into bat or PowerShell text (UTF-8 body; caller chooses BOM).
    /// </summary>
    public static class RenameScriptFormatter
    {
        private const string _AppName = "Magic File Renamer";
        private const string _AppUrl = "http://www.finebytes.com/mfr";

        private static readonly (FileAttributes Flag, char Letter, string Name)[] _RahsFlags =
        [
            (FileAttributes.ReadOnly, 'R', "ReadOnly"),
            (FileAttributes.Archive, 'A', "Archive"),
            (FileAttributes.Hidden, 'H', "Hidden"),
            (FileAttributes.System, 'S', "System"),
        ];

        /// <summary>
        /// Renders <paramref name="opGroups"/> as a complete script for <paramref name="format"/>.
        /// </summary>
        /// <param name="opGroups">Per-item op lists from <see cref="RenameScriptCollector.Collect"/>.</param>
        /// <param name="format">Bat or PowerShell dialect.</param>
        /// <returns>Script text including header; ends with a trailing newline when non-empty body ops exist.</returns>
        public static string Format(IReadOnlyList<IReadOnlyList<RenameScriptOp>> opGroups, RenameScriptFormat format)
        {
            ArgumentNullException.ThrowIfNull(opGroups);

            return format switch
            {
                RenameScriptFormat.Bat => _Format(opGroups, _AppendBatHeader, _AppendBatOp),
                RenameScriptFormat.PowerShell => _Format(opGroups, _AppendPowerShellHeader, _AppendPowerShellOp),
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported rename script format."),
            };
        }

        private static string _Format(
            IReadOnlyList<IReadOnlyList<RenameScriptOp>> opGroups,
            Action<StringBuilder> appendHeader,
            Action<StringBuilder, RenameScriptOp> appendOp
        )
        {
            var sb = new StringBuilder();
            appendHeader(sb);
            foreach (var group in opGroups)
            {
                foreach (var op in group)
                {
                    appendOp(sb, op);
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static void _AppendBatHeader(StringBuilder sb)
        {
            sb.AppendLine("REM **********************************************************");
            sb.AppendLine("REM *");
            sb.AppendLine($"REM This file was created by {_AppName}");
            sb.AppendLine($"REM {_AppUrl}");
            sb.AppendLine("REM *");
            sb.AppendLine("REM **********************************************************");
            sb.AppendLine();
        }

        private static void _AppendPowerShellHeader(StringBuilder sb)
        {
            sb.AppendLine("# **********************************************************");
            sb.AppendLine("# *");
            sb.AppendLine($"# This file was created by {_AppName}");
            sb.AppendLine($"# {_AppUrl}");
            sb.AppendLine("# *");
            sb.AppendLine("# **********************************************************");
            sb.AppendLine();
        }

        private static void _AppendBatOp(StringBuilder sb, RenameScriptOp op)
        {
            switch (op)
            {
                case RenameSameFolder rename:
                    sb.AppendLine($"ren {_BatQuote(rename.SourceFullPath)} {_BatQuote(rename.DestinationFileName)}");
                    break;
                case MoveWithParent move:
                    sb.AppendLine(
                        $"if not exist {_BatQuote(move.DestinationDirectory)} mkdir {_BatQuote(move.DestinationDirectory)}"
                    );
                    sb.AppendLine($"move {_BatQuote(move.SourceFullPath)} {_BatQuote(move.DestinationFullPath)}");
                    break;
                case SetRahsAttributes attrs:
                    sb.AppendLine($"attrib {_FormatBatAttribArgs(attrs)}{_BatQuote(attrs.TargetFullPath)}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, "Unknown rename script op.");
            }
        }

        private static void _AppendPowerShellOp(StringBuilder sb, RenameScriptOp op)
        {
            switch (op)
            {
                case RenameSameFolder rename:
                    sb.AppendLine(
                        $"Rename-Item -LiteralPath {_PsQuote(rename.SourceFullPath)} -NewName {_PsQuote(rename.DestinationFileName)}"
                    );
                    break;
                case MoveWithParent move:
                    sb.AppendLine(
                        $"if (-not (Test-Path -LiteralPath {_PsQuote(move.DestinationDirectory)})) {{ New-Item -ItemType Directory -Path {_PsQuote(move.DestinationDirectory)} | Out-Null }}"
                    );
                    sb.AppendLine(
                        $"Move-Item -LiteralPath {_PsQuote(move.SourceFullPath)} -Destination {_PsQuote(move.DestinationFullPath)}"
                    );
                    break;
                case SetRahsAttributes attrs:
                    _AppendPowerShellAttrs(sb, attrs);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, "Unknown rename script op.");
            }
        }

        private static void _AppendPowerShellAttrs(StringBuilder sb, SetRahsAttributes attrs)
        {
            var path = _PsQuote(attrs.TargetFullPath);
            sb.AppendLine($"$item = Get-Item -LiteralPath {path} -Force");
            _AppendPowerShellFlagOps(sb, attrs.SetFlags, set: true);
            _AppendPowerShellFlagOps(sb, attrs.ClearFlags, set: false);
        }

        private static void _AppendPowerShellFlagOps(StringBuilder sb, FileAttributes flags, bool set)
        {
            foreach (var (flag, _, name) in _RahsFlags)
            {
                if ((flags & flag) == 0)
                {
                    continue;
                }

                if (set)
                {
                    sb.AppendLine($"$item.Attributes = $item.Attributes -bor [System.IO.FileAttributes]::{name}");
                }
                else
                {
                    sb.AppendLine(
                        $"$item.Attributes = $item.Attributes -band (-bnot [System.IO.FileAttributes]::{name})"
                    );
                }
            }
        }

        private static string _FormatBatAttribArgs(SetRahsAttributes attrs)
        {
            var sb = new StringBuilder();
            foreach (var (flag, letter, _) in _RahsFlags)
            {
                if ((attrs.SetFlags & flag) != 0)
                {
                    sb.Append('+');
                    sb.Append(letter);
                    sb.Append(' ');
                }
                else if ((attrs.ClearFlags & flag) != 0)
                {
                    sb.Append('-');
                    sb.Append(letter);
                    sb.Append(' ');
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Quotes a cmd.exe path/name, doubling embedded double quotes.
        /// </summary>
        private static string _BatQuote(string value)
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        /// <summary>
        /// Quotes a PowerShell single-quoted literal, doubling embedded single quotes.
        /// </summary>
        private static string _PsQuote(string value)
        {
            return $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";
        }
    }
}
