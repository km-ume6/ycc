using System.Data;
using System.Diagnostics;

namespace FirstClassLibrary
{
    public class Utilities
    {
        public static void OutputDataTable(DataTable dataTable)
        {
            if (dataTable.Rows.Count > 0)
            {
                // 列名を出力
                string columnNames = string.Join(", ", dataTable.Columns.Cast<DataColumn>().Select(col => col.ColumnName));
                Debug.WriteLine(columnNames);

                // 各行の値を出力
                foreach (DataRow row in dataTable.Rows)
                {
                    string rowValues = string.Join(", ", row.ItemArray.Select(item => item?.ToString() ?? ""));
                    Debug.WriteLine(rowValues);
                }
            }
            else
            {
                Debug.WriteLine("No records found");
            }
        }
    }
}
