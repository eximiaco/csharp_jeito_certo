using System.Text;

namespace OrderReport;

public static class StringHelper
{
    public static ReadOnlySpan<char> GetColumnValueAsSpan(string line, int column)
    {
        var current = 0;
        var start = 0;
        var currentColumn = 0;

        foreach (var cursor in line)
        {
            if (cursor == ',')
            {
                if (column == currentColumn)
                {
                    break;
                }
                else
                {
                    currentColumn++;
                    start = current + 1;
                }
            }

            current++;
        }

        return line.AsSpan(start, current - start);
    }

    public static ReadOnlySpan<char> GetColumnValueAsSpan(ReadOnlySpan<char> line, int column)
    {
        var current = 0;
        var start = 0;
        var currentColumn = 0;

        foreach (var cursor in line)
        {
            if (cursor == ',')
            {
                if (column == currentColumn)
                {
                    break;
                }
                else
                {
                    currentColumn++;
                    start = current + 1;
                }
            }

            current++;
        }

        return line[start..current];
    }
}
