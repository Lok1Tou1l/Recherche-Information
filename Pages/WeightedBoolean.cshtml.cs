using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RechercheInformation
{
    public class WeightedBoolean : PageModel
    {
        private List<string> _documents = new List<string>();
        private Dictionary<string, double> _scores = new Dictionary<string, double>();
        private HashSet<string> _stopWords = new HashSet<string> { "is", "and", "the", "for", "in", "on", "to", "a", "of" };
        public string Query { get; set; }
        public List<(string Document, double Score)> SearchResults { get; set; } = new List<(string Document, double Score)>();

        public WeightedBoolean()
        {
            // Initialize documents
            _documents = new List<string>
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

            ComputeIDF();
        }

        private List<string> Tokenizer(string text)
        {
            if (string.IsNullOrEmpty(text)) return new List<string>();
            
            text = text.ToLower();
            return Regex.Split(text, @"\W+")
                        .Where(token => !string.IsNullOrWhiteSpace(token) && !_stopWords.Contains(token))
                        .ToList();
        }

        private void ComputeIDF()
        {
            _scores.Clear();
            var docCount = _documents.Count;

            foreach (var doc in _documents)
            {
                foreach (var term in Tokenizer(doc))
                {
                    if (!_scores.ContainsKey(term))
                    {
                        int docFrequency = _documents.Count(d => Tokenizer(d).Contains(term));
                        _scores[term] = Math.Log10((double)docCount / (docFrequency + 1));
                    }
                }
            }
        }

        private Dictionary<string, double> ComputeScores(string document)
        {
            var tokens = Tokenizer(document);
            var tfidf = new Dictionary<string, double>();

            foreach (var term in tokens)
            {
                if (_scores.ContainsKey(term))
                {
                    double tf = tokens.Count(t => t == term);
                    double idf = _scores[term];
                    tfidf[term] = tf * idf;
                }
            }

            return tfidf;
        }

        private double ComputeWeightedBoolean(Dictionary<string, double> queryVector, Dictionary<string, double> documentVector)
        {
            double score = 0;
            foreach (var term in queryVector.Keys)
            {
                if (documentVector.ContainsKey(term))
                {
                    score += queryVector[term] * documentVector[term];
                }
            }
            return score;
        }

        public List<(string Document, double Score)> WeightedBooleanSearch(string query)
        {
            var queryTokens = Tokenizer(query);
            var queryVector = new Dictionary<string, double>();
            foreach (var term in queryTokens)
            {
                queryVector[term] = _scores.ContainsKey(term) ? _scores[term] : 0;
            }

            var results = new List<(string Document, double Score)>();

            foreach (var doc in _documents)
            {
                var documentVector = ComputeScores(doc);
                double score = ComputeWeightedBoolean(queryVector, documentVector);

                if (score > 0)
                {
                    results.Add((doc, score));
                }
            }

            return results.OrderByDescending(r => r.Score).ToList();
        }

        public void OnGet(string query)
        {
            Query = query;
            if (!string.IsNullOrEmpty(query))
            {
                SearchResults = WeightedBooleanSearch(query);
            }
        }
    }
}
