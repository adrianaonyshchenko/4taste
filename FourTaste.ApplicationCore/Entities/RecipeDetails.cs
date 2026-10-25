using System;
using System.Collections.Generic;
using System.Text;

namespace FourTaste.ApplicationCore.Entities
{
    public class RecipeDetails
    {
        required public Recipe Recipe { get; init; }

        required public string AuthorName { get; init; }
    }
}
