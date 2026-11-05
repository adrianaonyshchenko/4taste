using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Results;
using Serilog;

namespace FourTaste.ApplicationCore.Services
{
    public class SubstituteService : ISubstituteService
    {
        private readonly IRecipeRepository _recipeRepository;
        private readonly IAllergyService _allergyService;
        private readonly ISubstituteRepository _substituteRepository;
        private readonly ILogger _logger;

        public SubstituteService(
            IRecipeRepository recipeRepository,
            IAllergyService allergyService,
            ISubstituteRepository substituteRepository,
            ILogger? logger = null)
        {
            _recipeRepository = recipeRepository ?? throw new ArgumentNullException(nameof(recipeRepository));
            _allergyService = allergyService ?? throw new ArgumentNullException(nameof(allergyService));
            _substituteRepository = substituteRepository ?? throw new ArgumentNullException(nameof(substituteRepository));
            _logger = logger ?? Log.Logger;
        }

        public async Task<Result<IReadOnlyList<Ingredient>>> GetConflictingIngredientsAsync(int userId, int recipeId)
        {
            if (userId <= 0)
            {
                _logger.Warning("Спроба отримати конфліктуючі інгредієнти неавторизованим користувачем.");
                return Result<IReadOnlyList<Ingredient>>.Failure("Користувач не авторизований.");
            }

            var recipe = await _recipeRepository.GetByIdAsync(recipeId);
            if (recipe == null)
            {
                _logger.Warning("Рецепт з Id {RecipeId} не знайдено.", recipeId);
                return Result<IReadOnlyList<Ingredient>>.Failure("Рецепт не знайдено.");
            }

            if (!recipe.IsPublic && recipe.UserId != userId)
            {
                _logger.Warning("Користувач {UserId} не має доступу до приватного рецепта {RecipeId}.", userId, recipeId);
                return Result<IReadOnlyList<Ingredient>>.Failure("Цей рецепт приватний.");
            }

            var allergiesResult = await _allergyService.GetAllergiesByUserIdAsync(userId);
            if (allergiesResult.IsFailure || allergiesResult.Value == null || allergiesResult.Value.Count == 0)
            {
                return Result<IReadOnlyList<Ingredient>>.Success(new List<Ingredient>());
            }

            var userAllergenNames = allergiesResult.Value
                .Where(a => !string.IsNullOrWhiteSpace(a.AllergenName))
                .Select(a => a.AllergenName.Trim())
                .ToList();

            var catalog = await _substituteRepository.GetAllergenCatalogAsync();
            var conflicting = recipe.Ingredients
                .Where(ing => AllergyMatcher.IsIngredientAllergenic(ing.Name, userAllergenNames, catalog))
                .ToList();

            return Result<IReadOnlyList<Ingredient>>.Success(conflicting);
        }

        public async Task<Result<SafeSubstitutesResponse>> GetSafeSubstitutesAsync(int userId, int recipeId, int ingredientId)
        {
            if (userId <= 0)
            {
                _logger.Warning("Спроба отримати безпечні замінники неавторизованим користувачем.");
                return Result<SafeSubstitutesResponse>.Failure("Користувач не авторизований.");
            }

            var recipe = await _recipeRepository.GetByIdAsync(recipeId);
            if (recipe == null)
            {
                _logger.Warning("Рецепт з Id {RecipeId} не знайдено.", recipeId);
                return Result<SafeSubstitutesResponse>.Failure("Рецепт не знайдено.");
            }

            if (!recipe.IsPublic && recipe.UserId != userId)
            {
                _logger.Warning("Користувач {UserId} не має доступу до приватного рецепта {RecipeId}.", userId, recipeId);
                return Result<SafeSubstitutesResponse>.Failure("Цей рецепт приватний.");
            }

            var ingredient = recipe.Ingredients.FirstOrDefault(i => i.Id == ingredientId);
            if (ingredient == null)
            {
                _logger.Warning("Інгредієнт {IngredientId} не знайдено в рецепті {RecipeId}.", ingredientId, recipeId);
                return Result<SafeSubstitutesResponse>.Failure("Інгредієнт не знайдено в рецепті.");
            }

            var allergiesResult = await _allergyService.GetAllergiesByUserIdAsync(userId);
            var userAllergenNames = allergiesResult.IsSuccess && allergiesResult.Value != null
                ? allergiesResult.Value.Where(a => !string.IsNullOrWhiteSpace(a.AllergenName)).Select(a => a.AllergenName.Trim()).ToList()
                : new List<string>();

            var catalog = await _substituteRepository.GetAllergenCatalogAsync();
            var userAllergenCodes = AllergyMatcher.ResolveUserAllergenCodes(userAllergenNames, catalog);

            var matchedAllergens = userAllergenNames
                .Where(al => AllergyMatcher.Normalize(ingredient.Name).Contains(AllergyMatcher.Normalize(al)) ||
                             catalog.Any(c => userAllergenCodes.Contains(c.Code) &&
                                              (AllergyMatcher.Normalize(ingredient.Name).Contains(AllergyMatcher.Normalize(c.NameUk)) ||
                                               AllergyMatcher.ExtractKeywords(c.Keywords).Any(k => AllergyMatcher.Normalize(ingredient.Name).Contains(k)))))
                .Distinct()
                .ToList();

            var allSubstitutes = await _substituteRepository.GetAllSubstitutesAsync();
            var candidateSubstitutes = FilterCandidatesForIngredient(ingredient.Name, allSubstitutes, catalog);

            var safeList = new List<SafeSubstituteItemDto>();
            foreach (var candidate in candidateSubstitutes)
            {
                // Критична перевірка безпеки (Fail-safe та повний профіль алергій)
                if (!AllergyMatcher.IsSubstituteSafe(candidate, userAllergenCodes, userAllergenNames, catalog))
                {
                    continue;
                }

                string calcAmount = AllergyMatcher.CalculateSubstituteAmount(ingredient.Amount, candidate.Ratio);
                safeList.Add(new SafeSubstituteItemDto
                {
                    Id = candidate.Id,
                    Name = candidate.SubstituteName,
                    Ratio = candidate.Ratio,
                    Note = candidate.Note,
                    AllergenCodes = AllergyMatcher.ParseCodes(candidate.AllergenCodes).ToList(),
                    CalculatedAmount = calcAmount,
                });
            }

            var response = new SafeSubstitutesResponse
            {
                RecipeId = recipeId,
                IngredientId = ingredientId,
                OriginalIngredientName = ingredient.Name,
                OriginalAmount = ingredient.Amount,
                MatchedUserAllergens = matchedAllergens,
                Substitutes = safeList,
            };

            return Result<SafeSubstitutesResponse>.Success(response);
        }

        public async Task<Result<AppliedSubstitutionDto>> ApplySubstitutionAsync(int userId, int recipeId, int ingredientId, int substituteId)
        {
            if (userId <= 0)
            {
                return Result<AppliedSubstitutionDto>.Failure("Користувач не авторизований.");
            }

            var recipe = await _recipeRepository.GetByIdAsync(recipeId);
            if (recipe == null)
            {
                return Result<AppliedSubstitutionDto>.Failure("Рецепт не знайдено.");
            }

            if (!recipe.IsPublic && recipe.UserId != userId)
            {
                return Result<AppliedSubstitutionDto>.Failure("Цей рецепт приватний.");
            }

            var ingredient = recipe.Ingredients.FirstOrDefault(i => i.Id == ingredientId);
            if (ingredient == null)
            {
                return Result<AppliedSubstitutionDto>.Failure("Інгредієнт не знайдено в рецепті.");
            }

            var substitute = await _substituteRepository.GetSubstituteByIdAsync(substituteId);
            if (substitute == null)
            {
                return Result<AppliedSubstitutionDto>.Failure("Замінник не знайдено.");
            }

            // Повторна серверна валідація безпечності (fail-safe + повний профіль алергій)
            var allergiesResult = await _allergyService.GetAllergiesByUserIdAsync(userId);
            var userAllergenNames = allergiesResult.IsSuccess && allergiesResult.Value != null
                ? allergiesResult.Value.Where(a => !string.IsNullOrWhiteSpace(a.AllergenName)).Select(a => a.AllergenName.Trim()).ToList()
                : new List<string>();

            var catalog = await _substituteRepository.GetAllergenCatalogAsync();
            var userAllergenCodes = AllergyMatcher.ResolveUserAllergenCodes(userAllergenNames, catalog);

            if (!AllergyMatcher.IsSubstituteSafe(substitute, userAllergenCodes, userAllergenNames, catalog))
            {
                _logger.Warning(
                    "Спроба застосувати небезпечний замінник: користувач {UserId}, замінник {SubstituteId}",
                    userId,
                    substituteId);
                return Result<AppliedSubstitutionDto>.Failure("Обраний замінник є небезпечним для вашого профілю алергій.");
            }

            string calcAmount = AllergyMatcher.CalculateSubstituteAmount(ingredient.Amount, substitute.Ratio);

            var substitution = new UserRecipeSubstitution
            {
                UserId = userId,
                RecipeId = recipeId,
                OriginalIngredientId = ingredientId,
                OriginalIngredientName = ingredient.Name,
                SubstituteIngredientName = substitute.SubstituteName,
                CalculatedAmount = calcAmount,
                Ratio = substitute.Ratio,
                Note = substitute.Note,
            };

            await _substituteRepository.SaveUserRecipeSubstitutionAsync(substitution);

            _logger.Information(
                "Успішно застосовано заміну інгредієнта {IngredientId} на {Substitute} у рецепті {RecipeId} для користувача {UserId}.",
                ingredientId,
                substitute.SubstituteName,
                recipeId,
                userId);

            return Result<AppliedSubstitutionDto>.Success(new AppliedSubstitutionDto
            {
                Id = substitution.Id,
                RecipeId = recipeId,
                OriginalIngredientId = ingredientId,
                OriginalIngredientName = ingredient.Name,
                SubstituteIngredientName = substitute.SubstituteName,
                CalculatedAmount = calcAmount,
                Ratio = substitute.Ratio,
                Note = substitute.Note,
            });
        }

        public async Task<Result<IReadOnlyList<AppliedSubstitutionDto>>> GetUserSubstitutionsAsync(int userId, int recipeId)
        {
            if (userId <= 0)
            {
                return Result<IReadOnlyList<AppliedSubstitutionDto>>.Failure("Користувач не авторизований.");
            }

            var list = await _substituteRepository.GetUserRecipeSubstitutionsAsync(userId, recipeId);
            var dtos = list.Select(s => new AppliedSubstitutionDto
            {
                Id = s.Id,
                RecipeId = s.RecipeId,
                OriginalIngredientId = s.OriginalIngredientId,
                OriginalIngredientName = s.OriginalIngredientName,
                SubstituteIngredientName = s.SubstituteIngredientName,
                CalculatedAmount = s.CalculatedAmount,
                Ratio = s.Ratio,
                Note = s.Note,
            }).ToList();

            return Result<IReadOnlyList<AppliedSubstitutionDto>>.Success(dtos);
        }

        public async Task<Result> RemoveSubstitutionAsync(int userId, int recipeId, int ingredientId)
        {
            if (userId <= 0)
            {
                return Result.Failure("Користувач не авторизований.");
            }

            await _substituteRepository.RemoveUserRecipeSubstitutionAsync(userId, recipeId, ingredientId);
            _logger.Information(
                "Скасовано заміну інгредієнта {IngredientId} у рецепті {RecipeId} для користувача {UserId}.",
                ingredientId,
                recipeId,
                userId);

            return Result.Success();
        }

        private static List<IngredientSubstitute> FilterCandidatesForIngredient(
            string ingredientName,
            List<IngredientSubstitute> allSubstitutes,
            List<AllergenCatalogItem> catalog)
        {
            var normalizedIng = AllergyMatcher.Normalize(ingredientName);

            // 1. Спочатку шукаємо точні або часткові збіги за назвою оригінального інгредієнта
            var directMatches = allSubstitutes.Where(s =>
            {
                var normOrig = AllergyMatcher.Normalize(s.OriginalIngredientName);
                return !string.IsNullOrEmpty(normOrig) &&
                       (normalizedIng.Contains(normOrig) || normOrig.Contains(normalizedIng));
            }).ToList();

            if (directMatches.Count > 0)
            {
                return directMatches;
            }

            // 2. Якщо прямого збігу немає, шукаємо через категорію алергенів
            return allSubstitutes.Where(s =>
            {
                var normOrig = AllergyMatcher.Normalize(s.OriginalIngredientName);
                if (string.IsNullOrEmpty(normOrig))
                {
                    return false;
                }

                var matchedCategory = catalog.FirstOrDefault(c =>
                    AllergyMatcher.Normalize(c.NameUk) == normOrig ||
                    AllergyMatcher.Normalize(c.Code) == normOrig ||
                    AllergyMatcher.ExtractKeywords(c.Keywords).Contains(normOrig));

                if (matchedCategory != null)
                {
                    if (normalizedIng.Contains(AllergyMatcher.Normalize(matchedCategory.NameUk)) ||
                        AllergyMatcher.ExtractKeywords(matchedCategory.Keywords).Any(k => normalizedIng.Contains(k)))
                    {
                        return true;
                    }
                }

                return false;
            }).ToList();
        }
    }
}
