using System.Collections.Generic;
using System.Text;

namespace DiscordTestBot {
    /// <summary>
    /// Simplistic table writer that takes arrays of strings and ensures that they are properly aligned.
    /// </summary>
    public class TableWriter {
        public int NumColumns { get; }
        private List<string[]> _rows;
        public TableWriter(int numColumns) {
            NumColumns = numColumns;
            _rows = new List<string[]>();
        }

        private int GetMaxWidth(int column) {
            int maxLength = 0;
            for (int i = 0; i < _rows.Count; i++) {
                int l = _rows[i][column].Length;
                if (l > maxLength)
                    maxLength = l;
            }
            return maxLength;
        }

        public void AddRow(params string[] row) {
            AddRowArray(row);
        }

        public void AddRowArray(string[] row) {
            if (row.Length != NumColumns)
                throw new System.Exception("Invalid number of columns: Is " + row.Length + ", should be " + NumColumns);
            _rows.Add(row);
        }

        public void Write(StringBuilder builder) {
            int[] columWidth = new int[NumColumns];
            for (int c = 0; c < NumColumns; c++)
                columWidth[c] = GetMaxWidth(c);

            for (int r = 0; r < _rows.Count; r++) {
                for (int c = 0; c < NumColumns; c++)
                    WriteField(builder, _rows[r][c], columWidth[c]);
                builder.AppendLine();
            }
        }

        private void WriteField(StringBuilder builder, string field, int length) {
            builder.Append(field);
            if (field.Length < length)
                builder.Append(' ', length - field.Length);
        }

        public string Write() {
            var sb = new StringBuilder();
            Write(sb);
            return sb.ToString();
        }
    }
}