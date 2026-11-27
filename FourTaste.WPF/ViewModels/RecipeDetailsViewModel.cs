using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Services;

namespace FourTaste.WPF.ViewModels
{
    public sealed class RecipeDetailsViewModel : INotifyPropertyChanged
    {
        public const int MinServings = 1;
        public const int MaxServings = 50;

        private readonly IRecipeScalingService _scalingService;

        private int _baseServings = 1;
        private int _targetServings = 1;
        private List<Ingredient> _rawIngredients = new ();
        private HashSet<int> _conflictingIds = new ();
        private Dictionary<int, AppliedSubstitutionDto> _activeSubstitutions = new ();

        public RecipeDetailsViewModel(IRecipeScalingService scalingService)
        {
            _scalingService = scalingService ?? throw new ArgumentNullException(nameof(scalingService));

            IncreaseServingsCommand = new RelayCommand(
                execute: () => TargetServings++,
                canExecute: () => TargetServings < MaxServings);

            DecreaseServingsCommand = new RelayCommand(
                execute: () => TargetServings--,
                canExecute: () => TargetServings > MinServings);

            ResetServingsCommand = new RelayCommand(
                execute: () => TargetServings = _baseServings,
                canExecute: () => TargetServings != _baseServings);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<RecipeIngredientDisplayModel> Ingredients { get; } = new ();

        public RelayCommand IncreaseServingsCommand { get; }

        public RelayCommand DecreaseServingsCommand { get; }

        public RelayCommand ResetServingsCommand { get; }

        public int BaseServings
        {
            get => _baseServings;
            private set
            {
                if (_baseServings == value)
                {
                    return;
                }

                _baseServings = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ResetButtonText));
                OnPropertyChanged(nameof(CanResetServings));
                ResetServingsCommand.RaiseCanExecuteChanged();
            }
        }

        public int TargetServings
        {
            get => _targetServings;
            set
            {
                int clamped = Math.Clamp(value, MinServings, MaxServings);
                if (_targetServings == clamped)
                {
                    return;
                }

                _targetServings = clamped;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ServingsWord));
                OnPropertyChanged(nameof(CanResetServings));

                IncreaseServingsCommand.RaiseCanExecuteChanged();
                DecreaseServingsCommand.RaiseCanExecuteChanged();
                ResetServingsCommand.RaiseCanExecuteChanged();

                RebuildScaledIngredients();
            }
        }

        public string ServingsWord => FormatServingsWord(_targetServings);

        public string ResetButtonText => $"Скинути до {_baseServings}";

        public bool CanResetServings => _targetServings != _baseServings;

        public void Initialize(
            Recipe recipe,
            IEnumerable<Ingredient> conflictingIngredients,
            IReadOnlyDictionary<int, AppliedSubstitutionDto> activeSubstitutions)
        {
            _rawIngredients = recipe.Ingredients.ToList();
            _conflictingIds = new HashSet<int>(conflictingIngredients.Select(i => i.Id));
            _activeSubstitutions = new Dictionary<int, AppliedSubstitutionDto>(activeSubstitutions);

            int initialServings = recipe.Servings > 0 ? Math.Clamp(recipe.Servings, MinServings, MaxServings) : 1;
            BaseServings = initialServings;
            _targetServings = initialServings;

            OnPropertyChanged(nameof(TargetServings));
            OnPropertyChanged(nameof(ServingsWord));
            OnPropertyChanged(nameof(ResetButtonText));
            OnPropertyChanged(nameof(CanResetServings));

            IncreaseServingsCommand.RaiseCanExecuteChanged();
            DecreaseServingsCommand.RaiseCanExecuteChanged();
            ResetServingsCommand.RaiseCanExecuteChanged();

            RebuildScaledIngredients();
        }

        public void UpdateSubstitutions(IReadOnlyDictionary<int, AppliedSubstitutionDto> activeSubstitutions)
        {
            _activeSubstitutions = new Dictionary<int, AppliedSubstitutionDto>(activeSubstitutions);
            RebuildScaledIngredients();
        }

        private static string FormatServingsWord(int count)
        {
            int mod10 = count % 10;
            int mod100 = count % 100;
            if (mod10 == 1 && mod100 != 11)
            {
                return "порція";
            }

            if (mod10 >= 2 && mod10 <= 4 && (mod100 < 10 || mod100 >= 20))
            {
                return "порції";
            }

            return "порцій";
        }

        private void RebuildScaledIngredients()
        {
            Ingredients.Clear();

            foreach (var ing in _rawIngredients)
            {
                bool isSubstituted = _activeSubstitutions.TryGetValue(ing.Id, out var sub);
                bool isAllergen = !isSubstituted && _conflictingIds.Contains(ing.Id);

                var (cleanName, effectiveAmount) = AllergyMatcher.ExtractCleanNameAndAmount(ing.Name, ing.Amount);

                string baseDisplayAmount = isSubstituted ? sub!.CalculatedAmount : effectiveAmount;

                var scaledDisplayResult = _scalingService.ScaleAmount(baseDisplayAmount, _baseServings, _targetServings);
                var scaledOriginalResult = _scalingService.ScaleAmount(effectiveAmount, _baseServings, _targetServings);

                Ingredients.Add(new RecipeIngredientDisplayModel
                {
                    Id = ing.Id,
                    Name = isSubstituted ? sub!.SubstituteIngredientName : cleanName,
                    Amount = scaledDisplayResult.IsSuccess ? scaledDisplayResult.Value! : baseDisplayAmount,
                    IsAllergen = isAllergen,
                    IsSubstituted = isSubstituted,
                    OriginalName = cleanName,
                    OriginalAmount = scaledOriginalResult.IsSuccess ? scaledOriginalResult.Value! : effectiveAmount,
                    SubstituteNote = isSubstituted ? sub!.Note : null,
                });
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}