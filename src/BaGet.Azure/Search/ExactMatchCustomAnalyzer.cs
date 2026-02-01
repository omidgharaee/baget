using System.Collections.Generic;
using Azure.Search.Documents.Indexes.Models;

namespace BaGet.Azure
{
    /// <summary>
    /// A custom analyzer for case insensitive exact match.
    /// </summary>
    public static class ExactMatchCustomAnalyzer
    {
        public const string Name = "baget-exact-match-analyzer";

        public static readonly CustomAnalyzer Instance = new CustomAnalyzer(Name, LexicalTokenizerName.Keyword)
        {
            TokenFilters = { TokenFilterName.Lowercase }
        };
    }
}
