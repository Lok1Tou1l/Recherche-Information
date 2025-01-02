using System.Text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
public class FileService : IFileService
{
    private const string DocumentStoragePath = "./DocumentStorage";

    public FileService(){
        Directory.CreateDirectory(DocumentStoragePath);
    }

    public async Task<string> SaveFileAsync(IFormFile file)
    {
        string fileName = $"{file.FileName}";
        string filePath = System.IO.Path.Combine(DocumentStoragePath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
        return fileName;
    }

    public void DeleteFile(string fileName)
    {
        string filePath  = System.IO.Path.Combine(DocumentStoragePath, fileName);
        File.Delete(filePath);
    }   

    public void DeleteAllFiles()
    {
        foreach (var file in Directory.GetFiles(DocumentStoragePath))
        {
            File.Delete(file);
        }
    }

    public string ReadFileContent(string filePath)
    {
        return File.ReadAllText(filePath);
    }

     public List<string> GetAvailableFiles()
    {
        return Directory.GetFiles(DocumentStoragePath).Select(System.IO.Path.GetFileName).ToList();
    }

    public string ExtractTextFromPDF(string path)
    {
        using (PdfReader reader = new PdfReader(path))
        {
            StringBuilder text = new StringBuilder();
            for (int i = 1; i <= reader.NumberOfPages; i++)
            {
                text.Append(PdfTextExtractor.GetTextFromPage(reader, i));
            }
            return text.ToString();
        }
    }

}