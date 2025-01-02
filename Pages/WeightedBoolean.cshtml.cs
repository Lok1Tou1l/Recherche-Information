using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.IO;

namespace RechercheInformation.Pages
{
    public class WeightedBooleanSearchModel : PageModel
    {
        private const string DocumentStoragePath = "./DocumentStorage";
        private readonly HashSet<string> _stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "is", "and", "the", "for", "in", "on", "to", "a", "of"
        };

        private readonly List<string> _documents = new List<string>();
        private readonly Dictionary<string, double> _idfScores = new Dictionary<string, double>();
        private readonly Dictionary<string, string> _documentTitles = new Dictionary<string, string>();

        [BindProperty(Required = true)]
        public IFormFile DocumentUpload { get; set; }

        [BindProperty(Required = true)]
        public string Query { get; set; }

        public List<(string Title, string Summary, double Score)> SearchResults { get; set; } = new List<(string Title, string Summary, double Score)>();
        public List<string> AvailableDocuments { get; set; } = new List<string>();

        public WeightedBooleanSearchModel()
        {
            // Ensure document storage directory exists
            Directory.CreateDirectory(DocumentStoragePath);
            LoadDocuments();
        }

        private void LoadDocuments()
        {
            _documents.Clear();
            AvailableDocuments.Clear();
            _documentTitles.Clear();

            // Load documents from storage
            foreach (var file in Directory.GetFiles(DocumentStoragePath, "*.txt"))
            {
                string documentContent = File.ReadAllText(file);
                _documents.Add(documentContent);
                AvailableDocuments.Add(Path.GetFileName(file));
                _documentTitles[Path.GetFileName(file)] = Path.GetFileName(file);
            }

            foreach (var file in Directory.GetFiles(DocumentStoragePath, "*.pdf"))
            {
                string documentContent = ExtractTextFromPDF(file);
                _documents.Add(documentContent);
                string title = Path.GetFileName(file);
                AvailableDocuments.Add(title);
                _documentTitles[title] = title;
            }

            // Compute IDF if documents exist
            if (_documents.Any())
            {
                ComputeIDF();
            }
        }

        public async Task<IActionResult> OnPostUploadAsync()
        {
            if (DocumentUpload == null || DocumentUpload.Length == 0)
            {
                ModelState.AddModelError("DocumentUpload", "Please select a file to upload.");
                return Page();
            }

            // Validate file type (only .txt and .pdf allowed)
            var extension = Path.GetExtension(DocumentUpload.FileName).ToLower();
            if (extension != ".txt" && extension != ".pdf")
            {
                ModelState.AddModelError("DocumentUpload", "Only .txt and .pdf files are allowed.");
                return Page();
            }

            try
            {
                // Generate a unique filename
                string fileName = $"{Guid.NewGuid()}_{DocumentUpload.FileName}";
                string filePath = Path.Combine(DocumentStoragePath, fileName);

                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await DocumentUpload.CopyToAsync(stream);
                }

                // Read and process the document
                string documentContent = extension == ".pdf" ? ExtractTextFromPDF(filePath) : await File.ReadAllTextAsync(filePath);
                _documents.Add(documentContent);
                _documentTitles[fileName] = Path.GetFileName(filePath);

                // Recompute IDF after adding new document
                ComputeIDF();

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"File upload failed: {ex.Message}");
                return Page();
            }
        }

        public IActionResult OnPostDeleteDocument(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return RedirectToPage();
            }

            try
            {
                string filePath = Path.Combine(DocumentStoragePath, fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // Reload documents
                LoadDocuments();

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error deleting document: {ex.Message}");
                return RedirectToPage();
            }
        }

        public void OnGet(string query)
        {
            Query = query;
            if (!string.IsNullOrEmpty(query))
            {
                SearchResults = WeightedBooleanSearch(query);
            }
        }

        private string ExtractTextFromPDF(string path)
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

        private List<string> Tokenizer(string text)
        {
            if (string.IsNullOrEmpty(text)) return new List<string>();

            return Regex.Split(text.ToLower(), @"\W+")
                .Where(token => !string.IsNullOrWhiteSpace(token) && !_stopWords.Contains(token))
                .ToList();
        }

        private void ComputeIDF()
        {
            _idfScores.Clear();
            int docCount = _documents.Count;

            var allTerms = _documents
                .SelectMany(Tokenizer)
                .Distinct()
                .ToList();

            foreach (var term in allTerms)
            {
                int documentFrequency = _documents.Count(doc => Tokenizer(doc).Contains(term));
                double idf = Math.Log10((double)docCount / (documentFrequency + 1));
                _idfScores[term] = idf;
            }
        }

        private Dictionary<string, double> ComputeTermFrequency(string document)
        {
            var tokens = Tokenizer(document);
            var termFrequency = new Dictionary<string, double>();

            foreach (var term in tokens)
            {
                if (!termFrequency.ContainsKey(term))
                {
                    termFrequency[term] = tokens.Count(t => t == term) / (double)tokens.Count;
                }
            }

            return termFrequency;
        }

        private Dictionary<string, double> ComputeTFIDF(string document)
        {
            var termFrequency = ComputeTermFrequency(document);
            var tfidfScores = new Dictionary<string, double>();

            foreach (var term in termFrequency.Keys)
            {
                if (_idfScores.ContainsKey(term))
                {
                    tfidfScores[term] = termFrequency[term] * _idfScores[term];
                }
            }

            return tfidfScores;
        }

        private double ComputeWeightedBoolean(Dictionary<string, double> queryVector, Dictionary<string, double> documentVector)
        {
            return queryVector.Keys.Sum(term => documentVector.ContainsKey(term) ? queryVector[term] * documentVector[term] : 0);
        }

        public List<(string Title, string Summary, double Score)> WeightedBooleanSearch(string query)
        {
            var queryTokens = Tokenizer(query);
            var queryVector = queryTokens.ToDictionary(term => term, term => _idfScores.ContainsKey(term) ? _idfScores[term] : 0);
            var results = new List<(string Title, string Summary, double Score)>();

            foreach (var doc in _documents)
            {
                var documentVector = ComputeTFIDF(doc);
                double score = ComputeWeightedBoolean(queryVector, documentVector);
                if (score > 0)
                {
                    string title = _documentTitles.FirstOrDefault(x => x.Value == doc).Key;
                    string summary = doc.AsSpan(0, Math.Min(100, doc.Length)).ToString() + "...";
                    results.Add((title, summary, score));
                }
            }

            return results.OrderByDescending(r => r.Score).ToList();
        }
    }
}