using System;
using System.Collections.Generic;
using System.Text;

namespace SW.EditorTools.Data
{
    /// <summary>따옴표로 감싼 탭·줄바꿈·따옴표를 보존하는 표 형식 변환기입니다.</summary>
    public static class SWDelimitedText
    {
        /// <summary>필요한 경우 값을 따옴표로 감싸 내보냅니다.</summary>
        public static string Quote(string value, char separator)
        {
            value ??= string.Empty;
            return value.IndexOfAny(new[] { separator, '"', '\r', '\n' }) >= 0
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }

        /// <summary>행과 열을 읽습니다. 닫히지 않은 따옴표와 잘못된 경계는 거절합니다.</summary>
        public static List<string[]> Read(string text, char separator = '\t')
        {
            List<string[]> rows = new();
            if (string.IsNullOrEmpty(text)) return rows;
            List<string> columns = new();
            StringBuilder field = new();
            bool quoted = false;
            bool quoteClosed = false;
            for (int index = 0; index < text.Length; index++)
            {
                char character = text[index];
                if (quoted)
                {
                    if (character != '"') field.Append(character);
                    else if (index + 1 < text.Length && text[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else
                    {
                        quoted = false;
                        quoteClosed = true;
                    }
                    continue;
                }
                if (character == separator || character == '\r' || character == '\n')
                {
                    columns.Add(field.ToString());
                    field.Clear();
                    quoteClosed = false;
                    if (character != separator)
                    {
                        AddRow(rows, columns);
                        if (character == '\r' && index + 1 < text.Length && text[index + 1] == '\n') index++;
                    }
                }
                else if (quoteClosed)
                    throw new FormatException("닫는 따옴표 뒤에는 열 구분자 또는 줄바꿈이 필요합니다.");
                else if (character == '"' && field.Length == 0)
                    quoted = true;
                else
                    field.Append(character);
            }
            if (quoted) throw new FormatException("닫히지 않은 따옴표가 있습니다.");
            if (field.Length > 0 || columns.Count > 0 || quoteClosed)
            {
                columns.Add(field.ToString());
                AddRow(rows, columns);
            }
            return rows;
        }

        /// <summary>내용이 있는 행을 추가하고 작업 중인 열 목록을 비웁니다.</summary>
        private static void AddRow(List<string[]> rows, List<string> columns)
        {
            if (columns.Count > 1 || columns.Count == 1 && columns[0].Length > 0)
                rows.Add(columns.ToArray());
            columns.Clear();
        }
    }
}
