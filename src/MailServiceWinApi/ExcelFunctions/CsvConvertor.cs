using System.Data;
using System.Text;
using ExcelDataReader;

namespace MailServiceWinApi.ExcelFunctions;

public class CsvConvertor
{
    
    public static List<string> ExcelToCsv(
        string filePath,
        string? outputDirectory = null,
        int startRowNumber = 1,
        int startColumnNumber = 1,
        string? brandFilter = null)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var configuration = new ExcelDataSetConfiguration()
        {
            ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
            {
                FilterRow = (rowReader) => rowReader.Depth >= startRowNumber - 1,
                FilterColumn = (columnReader, columnIndex) => columnIndex >= startColumnNumber - 1
            }
        };
        
        DataSet result = reader.AsDataSet(configuration);

        DataTable firstTable = result.Tables[0];

        List<string[]> matrix = [];

        foreach (DataRow row in firstTable.Rows)
        {
            object?[] itemArray = row.ItemArray;
            string?[] rowArray = new string?[itemArray.Length];
            for (int i = 0; i < itemArray.Length; i++)
            {
                object? cell = itemArray[i];
                rowArray[i] = (cell != null) ? cell.ToString() : string.Empty;
            }
            matrix.Add(rowArray!);
        }

        int rows = matrix.Count;
        int cols = matrix.Max(row => row.Length);

        List<string[]> transposedMatrix = new List<string[]>(cols);
        for (int j = 0; j < cols; j++)
        {
            string[] newRow = new string[rows];
            for (int i = 0; i < rows; i++)
            {
                newRow[i] = (j < matrix[i].Length) ? matrix[i][j] : string.Empty;
            }

            transposedMatrix.Add(newRow);
        }

        var stuctedValues = new Dictionary<string, List<string>>();

        for (int j = 0; j < transposedMatrix[0].Length; j++)
        {
            stuctedValues[transposedMatrix[0][j]] = new List<string>();
        }

        for (int i = 1; i < transposedMatrix.Count; i++)
        {
            string[] row = transposedMatrix[i];
            for (int j = 0; j < transposedMatrix[0].Length && j < row.Length; j++)
            {
                string val = row[j];
                if (!string.IsNullOrEmpty(val) && MatchesBrand(val, brandFilter))
                {
                    stuctedValues[transposedMatrix[0][j]].Add(val);
                }
            }
        }

        var outputDir = string.IsNullOrWhiteSpace(outputDirectory)
            ? Path.GetDirectoryName(filePath) ?? Directory.GetCurrentDirectory()
            : outputDirectory;
        Directory.CreateDirectory(outputDir);

        var createdFiles = new List<string>();
        
        DateTime dateTime = DateTime.Now;
        DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow;
        long secondsTimestamp = dateTimeOffset.ToUnixTimeSeconds();
        
        foreach (var keyValuePair in stuctedValues)
        {
            if (keyValuePair.Value.Count == 0)
            {
                continue;
            }

            string fileName = $"order_{keyValuePair.Key}.csv";
            var fileText = $"{keyValuePair.Key + ";" + dateTime.ToString("d") + ";" + secondsTimestamp + ";1;;;;;0;;;;;;"}";
            var fullPath = Path.Combine(outputDir, fileName);
            File.WriteAllText(fullPath, fileText);

            foreach (var value in keyValuePair.Value)
            {
                string trimmedBrand = value.Trim();
                
                string rowBrand = string.IsNullOrWhiteSpace(brandFilter)
                    ? ExtractBrand(trimmedBrand)
                    : brandFilter.Trim();

                fileText = $"\n{trimmedBrand};{rowBrand};1;0;{trimmedBrand}_{rowBrand};B";
                File.AppendAllText(fullPath, fileText);
            }

            createdFiles.Add(fullPath);
        }

        return createdFiles;
    }

    private static string ExtractBrand(string value)
    {
        var trimmedValue = value.Trim();
        if (trimmedValue.Length == 0)
        {
            return string.Empty;
        }

        var lastSpaceIndex = trimmedValue.LastIndexOf(' ');
        return lastSpaceIndex >= 0
            ? trimmedValue[(lastSpaceIndex + 1)..]
            : trimmedValue;
    }
    
    private static bool MatchesBrand(string value, string? brandFilter)
    {
        if (string.IsNullOrWhiteSpace(brandFilter))
        {
            return true;
        }

        var trimmedValue = value.Trim();
        if (trimmedValue.Length == 0)
        {
            return false;
        }

        var lastSpaceIndex = trimmedValue.LastIndexOf(' ');
        var productBrand = lastSpaceIndex >= 0
            ? trimmedValue[(lastSpaceIndex + 1)..]
            : trimmedValue;

        return string.Equals(productBrand, brandFilter.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static async Task SendConvertedFilesAndDeleteAsync(
        IReadOnlyList<string> csvFilePaths,
        Func<string, Task> sendFileAsync,
        CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var path in csvFilePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await sendFileAsync(path);
            }
        }
        finally
        {
            DeleteConvertedFiles(csvFilePaths);
        }
    }

    public static void DeleteConvertedFiles(IEnumerable<string> filePaths)
    {
        foreach (var filePath in filePaths)
        {
            DeleteFile(filePath);
        }
    }

    static void DeleteFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        try
        {
            File.Delete(filePath);
        }
        catch
        {
            // ignore cleanup errors
        }
    }
}
