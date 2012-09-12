using System;
using System.Text;

namespace DTZ.Utilities
{
    public class IndentedStringBuilder : IDisposable
    {
        private const int SpacesPerIndent = 2;

        private readonly StringBuilder _sb;
        private string _completeIndentationString = "";
        private int _indent;
        bool _newline = true;

        public IndentedStringBuilder()
        {
            _sb = new StringBuilder();
        }

        public int Length
        {
            get { return _sb.Length; }
        }

        public void Append(string value)
        {
            var i = value.IndexOf("\r\n", StringComparison.Ordinal);
            if (i < 0) // No newline
                InternalAppend(value, false);
            else if (i == value.Length - 2) // Ends with newline
                InternalAppend(value, true);
            else
            {
                InternalAppend(value.Substring(0, i + 2), true);
                Append(value.Substring(i + 2));
            }
        }

        private void InternalAppend(string value, bool endsInCr)
        {
            if (_newline)
                _sb.Append(_completeIndentationString);
            _sb.Append(value);
            _newline = endsInCr;
        }

        public void AppendLine(string value)
        {
            Append(value + Environment.NewLine);
        }

        public void AppendFormat(string format, params object[] objects)
        {
            Append(string.Format(format, objects));
        }

        public IndentedStringBuilder IncreaseIndent()
        {
            _indent++;
            _completeIndentationString = new string(' ', SpacesPerIndent * _indent);
            return this;
        }

        public void DecreaseIndent()
        {
            if (_indent <= 0) return;
            _indent--;
            _completeIndentationString = new string(' ', SpacesPerIndent*_indent);
        }

        public void Dispose()
        {
            DecreaseIndent();
        }

        public override string ToString()
        {
            return _sb.ToString();
        }
    }
}