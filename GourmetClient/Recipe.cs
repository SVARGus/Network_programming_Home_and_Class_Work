using System;
using System.Collections.Generic;

namespace GourmetRecipe
{
    public class Recipe
    {
        public string Name { get; set; }
        public List<string> Ingredients { get; set; }
        public string Instructions { get; set; }
        public string Description { get; set; }

        public string IngredientsString => string.Join(", ", Ingredients);

        public Recipe()
        {
            Ingredients = new List<string>();
        }
    }

    public class ClientRequest
    {
        public int IdUser { get; set; }
        public string Quary {  get; set; }
    }

    public class ServerResponse
    {
        public int IdUser { get; set; }
        public List<Recipe> Recipes { get; set; }
        public string Error { get; set; }
    }

    public class RequestData
    {
        public DateTime RequestTime { get; set; }
        public string Query { get; set; }
    }
}
