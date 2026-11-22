using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Results;
using Serilog;

namespace FourTaste.ApplicationCore.Services
{
    public class RecipeCategoryService : IRecipeCategoryService
    {
        public const int MaxCategoryNameLength = 60;

        private readonly IRecipeCategoryRepository _categoryRepository;
        private readonly ILogger _logger;

        public RecipeCategoryService(IRecipeCategoryRepository categoryRepository, ILogger logger)
        {
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<List<RecipeCategory>>> GetAllAsync()
        {
            _logger.Information("Отримання списку категорій рецептів.");
            var categories = await _categoryRepository.GetAllAsync();
            return Result<List<RecipeCategory>>.Success(categories);
        }

        public async Task<Result<int>> CreateAsync(string name)
        {
            string trimmed = (name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                _logger.Warning("Помилка валідації при створенні категорії: назва порожня.");
                return Result<int>.Failure("Назва категорії не може бути порожньою.");
            }

            if (trimmed.Length > MaxCategoryNameLength)
            {
                _logger.Warning("Помилка валідації при створенні категорії: задовга назва ({Length}).", trimmed.Length);
                return Result<int>.Failure($"Назва категорії не може перевищувати {MaxCategoryNameLength} символів.");
            }

            bool exists = await _categoryRepository.ExistsByNameAsync(trimmed);
            if (exists)
            {
                _logger.Warning("Спроба створити дублікат категорії '{CategoryName}'.", trimmed);
                return Result<int>.Failure($"Категорія «{trimmed}» уже існує.");
            }

            var category = new RecipeCategory
            {
                Name = trimmed,
                CreatedAt = DateTime.UtcNow
            };

            await _categoryRepository.AddAsync(category);
            _logger.Information("Створено нову категорію рецептів '{CategoryName}' (Id: {CategoryId}).", category.Name, category.Id);
            return Result<int>.Success(category.Id);
        }

        public async Task<Result> RenameAsync(int id, string newName)
        {
            if (id <= 0)
            {
                _logger.Warning("Помилка валідації при перейменуванні категорії: некоректний Id ({CategoryId}).", id);
                return Result.Failure("Некоректний ідентифікатор категорії.");
            }

            string trimmed = (newName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                _logger.Warning("Помилка валідації при перейменуванні категорії {CategoryId}: нова назва порожня.", id);
                return Result.Failure("Нова назва категорії не може бути порожньою.");
            }

            if (trimmed.Length > MaxCategoryNameLength)
            {
                _logger.Warning("Помилка валідації при перейменуванні категорії {CategoryId}: задовга назва ({Length}).", id, trimmed.Length);
                return Result.Failure($"Назва категорії не може перевищувати {MaxCategoryNameLength} символів.");
            }

            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                _logger.Warning("Перейменування категорії: запис з Id {CategoryId} не знайдено.", id);
                return Result.Failure("Категорію не знайдено.");
            }

            bool duplicate = await _categoryRepository.ExistsByNameAsync(trimmed, excludeId: id);
            if (duplicate)
            {
                _logger.Warning("Перейменування категорії {CategoryId}: назва '{NewName}' уже використовується.", id, trimmed);
                return Result.Failure($"Категорія «{trimmed}» уже існує.");
            }

            string oldName = category.Name;
            if (string.Equals(oldName, trimmed, StringComparison.Ordinal))
            {
                return Result.Success();
            }

            category.Name = trimmed;
            await _categoryRepository.UpdateAsync(category);
            await _categoryRepository.RenameCategoryInRecipesAsync(oldName, trimmed);

            _logger.Information("Категорію {CategoryId} перейменовано з '{OldName}' на '{NewName}'.", id, oldName, trimmed);
            return Result.Success();
        }

        public async Task<Result> DeleteAsync(int id)
        {
            if (id <= 0)
            {
                _logger.Warning("Помилка валідації при видаленні категорії: некоректний Id ({CategoryId}).", id);
                return Result.Failure("Некоректний ідентифікатор категорії.");
            }

            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                _logger.Warning("Видалення категорії: запис з Id {CategoryId} не знайдено.", id);
                return Result.Failure("Категорію не знайдено.");
            }

            int recipesCount = await _categoryRepository.CountRecipesByCategoryNameAsync(category.Name);
            if (recipesCount > 0)
            {
                _logger.Warning("Неможливо видалити категорію '{CategoryName}' (Id: {CategoryId}): до неї прив'язано {RecipesCount} рецептів.", category.Name, id, recipesCount);
                return Result.Failure($"Неможливо видалити категорію «{category.Name}», оскільки до неї прив'язано рецептів: {recipesCount}.");
            }

            await _categoryRepository.DeleteAsync(id);
            _logger.Information("Категорію '{CategoryName}' (Id: {CategoryId}) успішно видалено.", category.Name, id);
            return Result.Success();
        }
    }
}