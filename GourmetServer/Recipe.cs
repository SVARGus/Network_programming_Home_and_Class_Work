using System.Collections.Generic;

namespace GourmetRecipe
{
    public class Recipe
    {
        public string Name { get; set; }
        public List<string> Ingredients { get; set; }
        public string Instructions { get; set; }
        public string Description { get; set; }

        public Recipe()
        {
            Ingredients = new List<string>();
        }
    }
}
