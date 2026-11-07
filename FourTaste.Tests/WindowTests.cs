using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Services;
using FourTaste.WPF;
using Moq;
using Xunit;

namespace FourTaste.Tests
{
    public class WindowTests
    {
        [Fact]
        public void CookingHistoryWindow_InitializesWithEmptyItems()
        {
            Exception? exception = null;
            int itemsCount = -1;

            var thread = new Thread(() =>
            {
                try
                {
                    var mockMealPlan = new Mock<MealPlanService>(MockBehavior.Loose, null, null);
                    var mockRecipe = new Mock<IRecipeService>();
                    var window = new CookingHistoryWindow(mockMealPlan.Object, mockRecipe.Object);
                    var listBox = window.FindName("CookingHistoryListBox") as ListBox;
                    itemsCount = listBox?.Items.Count ?? -1;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Assert.Null(exception);
            Assert.Equal(0, itemsCount);
        }

        [Fact]
        public void PublicRecipesWindow_InitializesSuccessfully()
        {
            Exception? exception = null;

            var thread = new Thread(() =>
            {
                try
                {
                    var mockRecipe = new Mock<IRecipeService>();
                    var window = new PublicRecipesWindow(mockRecipe.Object);
                    Assert.NotNull(window);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Assert.Null(exception);
        }

        [Fact]
        public void RecipeDetailsWindow_InitializesSuccessfully()
        {
            Exception? exception = null;
            bool substitutePanelFound = false;
            bool findButtonFound = false;

            var thread = new Thread(() =>
            {
                try
                {
                    var mockRecipe = new Mock<IRecipeService>();
                    var mockAllergy = new Mock<IAllergyService>();
                    var mockSubstitute = new Mock<ISubstituteService>();
                    var window = new RecipeDetailsWindow(mockRecipe.Object, mockAllergy.Object, mockSubstitute.Object, 1);
                    Assert.NotNull(window);

                    var panel = window.FindName("SubstitutePanel") as FrameworkElement;
                    substitutePanelFound = panel != null;

                    var button = window.FindName("FindSubstituteButton") as Button;
                    findButtonFound = button != null;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Assert.Null(exception);
            Assert.True(substitutePanelFound);
            Assert.True(findButtonFound);
        }
    }
}
