using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RechercheInformation
{
    public class WeightedBooleanSearchModel : PageModel
    {
        private const string DocumentStoragePath = "./DocumentStorage";
        private List<string> _documents = new List<string>();
        private Dictionary<string, double> _scores = new Dictionary<string, double>();
        private HashSet<string> _stopWords = new HashSet<string> { "is", "and", "the", "for", "in", "on", "to", "a", "of" };
        
        [BindProperty]
        public IFormFile DocumentUpload { get; set; }
        
        public string Query { get; set; }
        public List<(string Document, double Score)> SearchResults { get; set; } = new List<(string Document, double Score)>();
        public List<string> AvailableDocuments { get; set; } = new List<string>();

        public WeightedBooleanSearchModel()
        {
            // Ensure document storage directory exists
            Directory.CreateDirectory(DocumentStoragePath);
            
            // Load existing documents
            LoadDocuments();
        }

        private void LoadDocuments()
        {
            _documents.Clear();
            AvailableDocuments.Clear();

            // Load documents from storage
            foreach (var file in Directory.GetFiles(DocumentStoragePath, "*.txt"))
            {
                string documentContent = System.IO.File.ReadAllText(file);
                _documents.Add(documentContent);
                AvailableDocuments.Add(Path.GetFileName(file));
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

            // Validate file type (optional: enforce .txt)
            if (Path.GetExtension(DocumentUpload.FileName) != ".txt")
            {
                ModelState.AddModelError("DocumentUpload", "Only .txt files are allowed.");
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
                string documentContent = await System.IO.File.ReadAllTextAsync(filePath);
                _documents.Add(documentContent);

                // Recompute IDF after adding new document
                ComputeIDF();

                // Reload available documents
                LoadDocuments();

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
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // Reload documents
                LoadDocuments();

                return RedirectToPage();
            }
            catch (Exception)
            {
                return RedirectToPage();
            }
        }

        // [Previous methods remain the same: Tokenizer, ComputeIDF, ComputeScores, ComputeWeightedBoolean, WeightedBooleanSearch]
        // ... [Copy the previous implementation of these methods from the original code]

        public void OnGet(string query)
        {
            Query = query;
            if (!string.IsNullOrEmpty(query))
            {
                SearchResults = WeightedBooleanSearch(query);
            }
        }


    /// <summary>
/// Tokenizes input text into meaningful tokens
/// </summary>
/// <param name="text">Input text to tokenize</param>
/// <returns>List of cleaned and processed tokens</returns>
private List<string> Tokenizer(string text)
{
    // Handle null or empty input
    if (string.IsNullOrEmpty(text)) 
        return new List<string>();
    
    // Convert to lowercase to ensure case-insensitive matching
    text = text.ToLower();
    
    // Use regex to split text into tokens
    // \W+ matches one or more non-word characters (punctuation, spaces)
    var tokens = Regex.Split(text, @"\W+")
        .Where(token => 
            // Filter out empty or whitespace tokens
            !string.IsNullOrWhiteSpace(token) 
            // Remove stop words
            && !_stopWords.Contains(token))
        .ToList();
    
    return tokens;
}

/// <summary>
/// Computes Term Frequency (TF) for a document
/// </summary>
/// <param name="document">Document text</param>
/// <returns>Dictionary of terms and their term frequencies</returns>
private Dictionary<string, double> ComputeTermFrequency(string document)
{
    // Tokenize the document
    var tokens = Tokenizer(document);
    
    // Create a term frequency dictionary
    var termFrequency = new Dictionary<string, double>();
    
    foreach (var term in tokens)
    {
        // Count occurrences of each term
        if (!termFrequency.ContainsKey(term))
        {
            // Count total occurrences of the term
            termFrequency[term] = tokens.Count(t => t == term);
        }
    }
    
    // Normalize term frequencies (divide by total number of terms)
    int totalTerms = tokens.Count;
    foreach (var term in termFrequency.Keys.ToList())
    {
        termFrequency[term] /= totalTerms;
    }
    
    return termFrequency;
}

/// <summary>
/// Computes Inverse Document Frequency (IDF)
/// </summary>
private void ComputeIDF()
{
    // Clear previous IDF scores
    _scores.Clear();
    
    // Total number of documents
    int docCount = _documents.Count;
    
    // Iterate through all unique terms in all documents
    var allTerms = _documents
        .SelectMany(Tokenizer)
        .Distinct()
        .ToList();
    
    foreach (var term in allTerms)
    {
        // Count in how many documents the term appears
        int documentFrequency = _documents.Count(doc => 
            Tokenizer(doc).Contains(term));
        
        // Compute IDF using logarithmic formula
        // log(Total Documents / (Documents with term + 1))
        // +1 to avoid division by zero
        double idf = Math.Log10((double)docCount / (documentFrequency + 1));
        
        // Store IDF score
        _scores[term] = idf;
    }
}

/// <summary>
/// Computes TF-IDF scores for a document
/// </summary>
/// <param name="document">Document text</param>
/// <returns>Dictionary of terms with their TF-IDF scores</returns>
private Dictionary<string, double> ComputeTFIDF(string document)
{
    // Compute Term Frequency
    var termFrequency = ComputeTermFrequency(document);
    
    // Create TF-IDF dictionary
    var tfidfScores = new Dictionary<string, double>();
    
    foreach (var term in termFrequency.Keys)
    {
        // Multiply Term Frequency by Inverse Document Frequency
        if (_scores.ContainsKey(term))
        {
            double tfIdfScore = termFrequency[term] * _scores[term];
            tfidfScores[term] = tfIdfScore;
        }
    }
    
    return tfidfScores;
}

/// <summary>
/// Compute weighted boolean search score
/// </summary>
/// <param name="queryVector">Query term vector</param>
/// <param name="documentVector">Document term vector</param>
/// <returns>Similarity score between query and document</returns>
private double ComputeWeightedBoolean(
    Dictionary<string, double> queryVector, 
    Dictionary<string, double> documentVector)
{
    double score = 0;
    
    // Compute dot product of query and document vectors
    foreach (var term in queryVector.Keys)
    {
        if (documentVector.ContainsKey(term))
        {
            // Multiply query term weight by document term weight
            score += queryVector[term] * documentVector[term];
        }
    }
    
    return score;
}

/// <summary>
/// Perform weighted boolean search
/// </summary>
/// <param name="query">Search query</param>
/// <returns>Ranked search results</returns>
public List<(string Document, double Score)> WeightedBooleanSearch(string query)
{
    // Tokenize query
    var queryTokens = Tokenizer(query);
    
    // Create query vector with IDF weights
    var queryVector = new Dictionary<string, double>();
    foreach (var term in queryTokens)
    {
        // Use IDF score if available, otherwise 0
        queryVector[term] = _scores.ContainsKey(term) ? _scores[term] : 0;
    }
    
    var results = new List<(string Document, double Score)>();
    
    // Compute score for each document
    foreach (var doc in _documents)
    {
        // Compute document vector with TF-IDF
        var documentVector = ComputeTFIDF(doc);
        
        // Compute weighted boolean score
        double score = ComputeWeightedBoolean(queryVector, documentVector);
        
        // Only add documents with non-zero relevance
        if (score > 0)
        {
            results.Add((doc, score));
        }
    }
    
    // Return results sorted by relevance
    return results.OrderByDescending(r => r.Score).ToList();
}
    }
}