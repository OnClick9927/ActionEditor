using System;
using System.Text;
using System.Text.RegularExpressions;

namespace ActionAttribute
{
    public enum TextFieldMode
    {
        Default,
        Password
    }

    public enum TextCase
    {
        Preserve,
        UpperInvariant,
        LowerInvariant
    }

    /// <summary>
    /// Configures string drawing, normalization, length limits and validation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TextAttribute : ActionValidationAttribute
    {
        private enum ValidationIssue
        {
            None,
            WrongType,
            InvalidPattern,
            Pattern,
            Lines
        }

        public readonly TextFieldMode mode;

        public string Placeholder { get; set; }
        public int MaxLength { get; set; } = int.MaxValue;
        public bool Trim { get; set; }
        public bool CollapseWhitespace { get; set; }
        public TextCase Case { get; set; }
        public bool AllowEmpty { get; set; } = true;
        public bool IgnoreCase { get; set; }
        public int MinLines { get; set; }
        public int MaxLines { get; set; } = int.MaxValue;
        public string PatternMessage { get; set; }
        public string LinesMessage { get; set; }

        private string pattern;
        [NonSerialized] private Regex regex;
        [NonSerialized] private bool cachedIgnoreCase;
        [NonSerialized] private bool invalidPattern;

        public string Pattern
        {
            get => pattern;
            set
            {
                pattern = value;
                regex = null;
                invalidPattern = false;
            }
        }

        public TextAttribute(TextFieldMode mode = TextFieldMode.Default,
            string message = null,
            InspectorMessageType type = InspectorMessageType.Error)
            : base(message, type)
        {
            this.mode = mode;
        }

        public string Apply(string value)
        {
            if (value == null) return null;
            if (Trim) value = value.Trim();
            if (CollapseWhitespace) value = Collapse(value);
            switch (Case)
            {
                case TextCase.UpperInvariant:
                    value = value.ToUpperInvariant();
                    break;
                case TextCase.LowerInvariant:
                    value = value.ToLowerInvariant();
                    break;
            }

            int length = Math.Max(0, MaxLength);
            return value.Length > length ? value.Substring(0, length) : value;
        }

        public override bool IsValid(ActionAttributeContext context)
        {
            return GetIssue(context.Value) == ValidationIssue.None;
        }

        public override string GetMessage(ActionAttributeContext context)
        {
            if (!string.IsNullOrEmpty(message)) return message;
            switch (GetIssue(context.Value))
            {
                case ValidationIssue.InvalidPattern:
                    return $"{context.MemberName} 的匹配表达式无效。";
                case ValidationIssue.Pattern:
                    return string.IsNullOrEmpty(PatternMessage)
                        ? $"{context.MemberName} 的格式不正确。"
                        : PatternMessage;
                case ValidationIssue.Lines:
                    if (!string.IsNullOrEmpty(LinesMessage)) return LinesMessage;
                    int min = EffectiveMinLines;
                    int max = EffectiveMaxLines;
                    return max == int.MaxValue
                        ? $"{context.MemberName} 至少需要 {min} 行文本。"
                        : $"{context.MemberName} 的文本行数必须在 {min} 到 {max} 之间。";
                case ValidationIssue.WrongType:
                    return $"{context.MemberName} 必须是字符串。";
                default:
                    return null;
            }
        }

        private int EffectiveMinLines => Math.Max(0, MinLines);

        private int EffectiveMaxLines => Math.Max(EffectiveMinLines, MaxLines);

        private ValidationIssue GetIssue(object value)
        {
            if (!(value is string text)) return ValidationIssue.WrongType;
            if (pattern != null &&
                (!string.IsNullOrEmpty(text) || !AllowEmpty))
            {
                EnsureRegex();
                if (invalidPattern) return ValidationIssue.InvalidPattern;
                try
                {
                    if (!regex.IsMatch(text)) return ValidationIssue.Pattern;
                }
                catch (RegexMatchTimeoutException)
                {
                    regex = null;
                    invalidPattern = true;
                    return ValidationIssue.InvalidPattern;
                }
            }

            if (MinLines > 0 || MaxLines < int.MaxValue)
            {
                int count = CountLines(text);
                if (count < EffectiveMinLines || count > EffectiveMaxLines)
                    return ValidationIssue.Lines;
            }
            return ValidationIssue.None;
        }

        private void EnsureRegex()
        {
            if ((regex != null || invalidPattern) &&
                cachedIgnoreCase == IgnoreCase) return;
            cachedIgnoreCase = IgnoreCase;
            invalidPattern = false;
            try
            {
                RegexOptions options = RegexOptions.CultureInvariant;
                if (IgnoreCase) options |= RegexOptions.IgnoreCase;
                regex = new Regex(pattern ?? string.Empty, options,
                    TimeSpan.FromMilliseconds(100));
            }
            catch (ArgumentException)
            {
                regex = null;
                invalidPattern = true;
            }
        }

        private static int CountLines(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int count = 1;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n') count++;
                else if (text[i] == '\r')
                {
                    count++;
                    if (i + 1 < text.Length && text[i + 1] == '\n') i++;
                }
            }
            return count;
        }

        private static string Collapse(string value)
        {
            StringBuilder builder = null;
            bool previousWhitespace = false;
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                bool whitespace = char.IsWhiteSpace(character);
                if (!whitespace)
                {
                    builder?.Append(character);
                    previousWhitespace = false;
                    continue;
                }

                if (previousWhitespace || character != ' ')
                {
                    if (builder == null)
                    {
                        builder = new StringBuilder(value.Length);
                        builder.Append(value, 0, i);
                    }
                    if (!previousWhitespace) builder.Append(' ');
                }
                else builder?.Append(' ');
                previousWhitespace = true;
            }
            return builder?.ToString() ?? value;
        }
    }
}
