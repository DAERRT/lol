using System.Collections.Generic;

namespace lol.Helpers
{
    public static class StackColorHelper
    {
        private static readonly Dictionary<string, string> StackColors = new()
        {
            { "C#", "#b2dfdb" }, { "Java", "#ffe082" }, { "Python", "#c5e1a5" }, { "JavaScript", "#fff9c4" }, { "TypeScript", "#b3e5fc" },
            { "PHP", "#e1bee7" }, { "Go", "#b2ebf2" }, { "Kotlin", "#f8bbd0" }, { "Swift", "#ffccbc" }, { "Ruby", "#ffcdd2" }, { "C++", "#d1c4e9" },
            { ".NET", "#d7ccc8" }, { "ASP.NET", "#cfd8dc" }, { "Django", "#aed581" }, { "Flask", "#ffe0b2" }, { "Spring", "#b9f6ca" },
            { "React", "#b3e5fc" }, { "Angular", "#ffccbc" }, { "Vue.js", "#c8e6c9" }, { "Node.js", "#dcedc8" }, { "Express", "#f0f4c3" }, { "Laravel", "#f8bbd0" }
        };

        public static string GetColor(string tech)
        {
            return StackColors.TryGetValue(tech, out var color) ? color : "#e0e0e0";
        }
    }
} 