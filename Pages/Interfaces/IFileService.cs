public interface IFileService 
{
    Task<String> SaveFileAsync(IFormFile file);
    string ReadFileContent(string filePath);
    string ExtractTextFromPDF(string filePath);
    void DeleteFile(string filePath);
    void DeleteAllFiles();
}