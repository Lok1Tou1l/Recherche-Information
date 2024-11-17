using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RechercheInformation.Pages
{
    public class VectorModel : PageModel
    {
        private List<string> documents;
        private HashSet<string> vocabulary;
        private Dictionary<string, double> idfScores;
        private HashSet<string> stopwords = new HashSet<string> { "is", "and", "the", "for", "in", "on", "to", "a", "of" };
        public string Query { get; set; }

        public VectorModel()
        {
            // Initialize documents
            documents = new List<string>
            {
               "AI models are used in game development.",
             "Game development involves AI techniques.",
    "Python is widely used in AI and machine learning.",
    "Game engines like Unity and Unreal offer tools for game development.",
    "Machine learning algorithms help improve game AI.",
    "Unity is a popular game development platform.",
    "Artificial Intelligence is transforming various industries.",
    "Data science and AI are closely related fields.",
    "Deep learning is a subset of machine learning.",
    "Unreal Engine is known for high-end graphics in games.",
    "AI chatbots are used in customer service.",
    "Video games can utilize procedural generation techniques.",
    "Python is often used for data analysis and visualization.",
    "Natural Language Processing (NLP) enables AI to understand human language.",
    "Machine learning models can predict user behavior.",
    "3D modeling and texturing are essential for game design.",
    "Data-driven decisions are increasingly common in software development.",
    "AI in healthcare is improving diagnostics and patient care.",
    "Neural networks learn by training on large datasets.",
    "Video game graphics rely on rendering techniques and shaders."
            };

            BuildVocabulary();
            ComputeIDFScores();
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

        public List<string> Search(string query)
        {
            var queryVector = ComputeTFIDFVector(query);
            double maxSimilarity = 0;
            string bestMatch = null;
            List<string> results = new List<string>();

            foreach (var doc in documents)
            {
                var docVector = ComputeTFIDFVector(doc);
                var similarity = ComputeCosineSimilarity(queryVector, docVector);
                if (similarity > maxSimilarity)
                {
                    maxSimilarity = similarity;
                    bestMatch = doc;
                }
                results.Add(doc);
            }

            return results;
        }

public List<string> SearchResults { get; private set; } = new List<string>();

public void OnGet(string query)
{
    Query = query;
    if (!string.IsNullOrWhiteSpace(query))
    {
        List<string> results = Search(query);
        List<string> sortedResults = SortResults(results, query);

       
        
            sortedResults.RemoveAll(doc => ComputeCosineSimilarity(ComputeTFIDFVector(query), ComputeTFIDFVector(doc)) == 0);
            if (sortedResults.Count == 0)
            {
                SearchResults.Add("No results found.");
            }
            else
            {
            foreach (var item in sortedResults)
            {
                SearchResults.Add(" ");
                SearchResults.Add(item);
                SearchResults.Add("Similarity: " + ComputeCosineSimilarity(ComputeTFIDFVector(query), ComputeTFIDFVector(item)));
                SearchResults.Add("");

            }
        
    }
}
}

private List<string> SortResults(List<string> results, string query)
{
    var queryVector = ComputeTFIDFVector(query);
    var sortedResults = results.OrderByDescending(doc => ComputeCosineSimilarity(queryVector, ComputeTFIDFVector(doc))).ToList();
    return sortedResults;
}
}}

