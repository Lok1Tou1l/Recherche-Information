using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RechercheInformation.Pages
{
    public class BooleanModel : PageModel
    {
        private List<string> documents;
        private HashSet<string> stopwords = new HashSet<string> { "is", "and", "the", "for", "in", "on", "to", "a", "of" };
        public string Query { get; set; }
        public List<string> SearchResults { get; set; } = new List<string>();

        public BooleanModel()
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
        }

        private List<string> TokenizeAndProcess(string text)
        {
            text = text.ToLower();
            var tokens = Regex.Split(text, @"\W+").Where(token => token.Length > 0).ToList();
            return tokens.Where(token => !stopwords.Contains(token)).ToList();
        }

        public List<string> BooleanSearch(string query)
        {
            var processedQuery = query.ToLower().Split(' ');
            var matchingDocuments = new List<string>();

            foreach (var doc in documents)
            {
                var docTokens = TokenizeAndProcess(doc);

                if (processedQuery.Contains("and"))
                {
                    var terms = processedQuery.Where(t => t != "and").ToList();
                    if (terms.All(term => docTokens.Contains(term)))
                    {
                        matchingDocuments.Add(doc);
                    }
                }
                else if (processedQuery.Contains("or"))
                {
                    var terms = processedQuery.Where(t => t != "or").ToList();
                    if (terms.Any(term => docTokens.Contains(term)))
                    {
                        matchingDocuments.Add(doc);
                    }
                }
                else
                {
                    if (processedQuery.All(term => docTokens.Contains(term)))
                    {
                        matchingDocuments.Add(doc);
                    }
                }
            }

            return matchingDocuments;
        }

        public void OnGet(string query)
        {
            if (!string.IsNullOrEmpty(query))
            {
                Query = query;
                SearchResults = BooleanSearch(query);
            }
        }
    }
}
