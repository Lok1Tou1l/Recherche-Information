public interface ISearchService
{
    void LoadDocuments(List<string> documents);
    List<(string Title, string Summary, double Score)> Search(string query, Dictionary<string, string> documentTitles);
}