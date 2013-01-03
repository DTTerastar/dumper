using System;
using System.Text;

namespace DTZ.Utilities
{
    public class IndentedStringBuilder
    {
        private const int SpacesPerIndent = 2;
        private readonly StringBuilder _sb;
        private string _completeIndentationString = "";
        private int _indent;
        private bool _newline;

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
            int i = value.IndexOf("\r\n", StringComparison.Ordinal);
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

        public void AppendLine()
        {
            Append(Environment.NewLine);
        }

        public void AppendLine(string value)
        {
            Append(value);
            AppendLine();
        }

        public void AppendFormat(string format, params object[] objects)
        {
            Append(string.Format(format, objects));
        }

        public DecreaseIndentOnDispose IncreaseIndent()
        {
            _indent++;
            _completeIndentationString = new string(' ', SpacesPerIndent*_indent);
            return new DecreaseIndentOnDispose(this);
        }

        public void DecreaseIndent()
        {
            if (_indent <= 0) return;
            _indent--;
            _completeIndentationString = new string(' ', SpacesPerIndent*_indent);
        }


        public override string ToString()
        {
            return _sb.ToString();
        }
    }

    public class DecreaseIndentOnDispose : IDisposable
    {
        private readonly IndentedStringBuilder _indentedStringBuilder;

        public DecreaseIndentOnDispose(IndentedStringBuilder indentedStringBuilder)
        {
            _indentedStringBuilder = indentedStringBuilder;
        }

        public void Dispose()
        {
            _indentedStringBuilder.DecreaseIndent();
        }
    }
}