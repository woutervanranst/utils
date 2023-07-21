using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WouterVanRanst.Utils.Extensions;

public static class DataTableExtensions
{
    public static T[,] ToArray<T>(this DataTable dataTable)
    {
        if (dataTable == null)
        {
            throw new ArgumentNullException(nameof(dataTable));
        }

        var rowCount = dataTable.Rows.Count;
        var columnCount = dataTable.Columns.Count;
        T[,] result = new T[rowCount, columnCount];

        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            var row = dataTable.Rows[rowIndex];
            for (var colIndex = 0; colIndex < columnCount; colIndex++)
            {
                result[rowIndex, colIndex] = (T)row[colIndex];
            }
        }

        return result;
    }
}