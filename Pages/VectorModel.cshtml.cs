using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RechercheInformation.Pages
{
    public class VectorModel : PageModel
    {
        private List<string> documents;
        private HashSet<string> vocabulary;
        private Dictionary<string, double> idfScores;
        private HashSet<string> stopwords = new HashSet<string> { "is", "and", "the", "for", "in", "on", "to", "a", "of" };
        
        [BindProperty]
        public IFormFile DocumentUpload { get; set; }
        
        public string Query { get; set; }
        public List<(string Document, double Similarity)> SearchResults { get; private set; } = new List<(string Document, double Similarity)>();
        public List<string> UploadedDocuments { get; private set; } = new List<string>();

        public VectorModel()
        {
            BuildVocabulary();
            ComputeIDFScores();
        }

        public void OnGet()
        {
            // Initialize the page without search
        }

        public async Task<IActionResult> OnPostUploadAsync()
        {
            if (DocumentUpload != null && DocumentUpload.Length > 0)
            {
                // Limit file size to 5MB
                if (DocumentUpload.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("DocumentUpload", "File size cannot exceed 5MB.");
                    return Page();
                }

                // Read the uploaded file
                using (var reader = new StreamReader(DocumentUpload.OpenReadStream()))
                {
                    string content = await reader.ReadToEndAsync();
                    
                    // Add the document to the list
                    documents.Add(content);
                    UploadedDocuments.Add(DocumentUpload.FileName);
                }

                // Rebuild vocabulary and IDF scores
                BuildVocabulary();
                ComputeIDFScores();
            }

            return RedirectToPage();
        }

        public IActionResult OnPostSearch(string query)
        {
            Query = query;
            if (!string.IsNullOrWhiteSpace(query))
            {
                var queryVector = ComputeTFIDFVector(query);

                SearchResults = documents
                    .Select(doc => (Document: doc, Similarity: ComputeCosineSimilarity(queryVector, ComputeTFIDFVector(doc))))
                    .Where(result => result.Similarity > 0)
                    .OrderByDescending(result => result.Similarity)
                    .ToList();
            }

            return Page();
        }

        private void BuildVocabulary()
        {
            vocabulary = new HashSet<string>();
            foreach (var doc in documents)
            {
                var tokens = TokenizeAndProcess(doc);
                foreach (var token in tokens)
                {
                    vocabulary.Add(token);
                }
            }
        }

        private List<string> TokenizeAndProcess(string text)
        {
            text = text.ToLower();
            var tokens = Regex.Split(text, @"\W+").Where(token => token.Length > 0).ToList();
            return tokens.Where(token => !stopwords.Contains(token)).Select(Stemming).ToList();
        }

        private string Stemming(string word)
        {
            if (word.EndsWith("ing")) return word.Substring(0, word.Length - 3);
            if (word.EndsWith("ed")) return word.Substring(0, word.Length - 2);
            return word;
        }

        private void ComputeIDFScores()
        {
            idfScores = new Dictionary<string, double>();
            var docCount = documents.Count;

            foreach (var word in vocabulary)
            {
                int count = documents.Count(doc => TokenizeAndProcess(doc).Contains(word));
                idfScores[word] = Math.Log((double)docCount / (count + 1));
            }
        }

        private double[] ComputeTFIDFVector(string document)
        {
            var tokens = TokenizeAndProcess(document);
            var tfVector = new double[vocabulary.Count];
            var vocabList = vocabulary.ToList();

            foreach (var token in tokens)
            {
                int index = vocabList.IndexOf(token);
                if (index >= 0)
                {
                    tfVector[index]++;
                }
            }

            for (int i = 0; i < tfVector.Length; i++)
            {
                string word = vocabList[i];
                tfVector[i] = tfVector[i] * (idfScores.ContainsKey(word) ? idfScores[word] : 0);
            }

            return tfVector;
        }

        private double ComputeCosineSimilarity(double[] vector1, double[] vector2)
        {
            double dotProduct = 0;
            double magnitude1 = 0;
            double magnitude2 = 0;

            for (int i = 0; i < vector1.Length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                magnitude1 += vector1[i] * vector1[i];
                magnitude2 += vector2[i] * vector2[i];
            }

            magnitude1 = Math.Sqrt(magnitude1);
            magnitude2 = Math.Sqrt(magnitude2);

            if (magnitude1 == 0 || magnitude2 == 0) return 0;

            return dotProduct / (magnitude1 * magnitude2);
        }
    }
}