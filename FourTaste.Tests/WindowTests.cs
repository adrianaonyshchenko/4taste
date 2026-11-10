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

                    var itemsControl = window.FindName("UserRoomsItemsControl") as ItemsControl;
                    Assert.NotNull(itemsControl);
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

        [Fact]
        public void CreateRoomWindow_InitializesSuccessfully()
        {
            Exception? exception = null;
            bool createButtonFound = false;
            bool joinButtonFound = false;
            bool cancelButtonFound = false;

            var thread = new Thread(() =>
            {
                try
                {
                    var mockRoom = new Mock<IRoomService>();
                    var window = new CreateRoomWindow(mockRoom.Object);
                    Assert.NotNull(window);

                    var createBtn = window.FindName("CreateButton") as Button;
                    createButtonFound = createBtn != null;

                    var joinBtn = window.FindName("JoinButton") as Button;
                    joinButtonFound = joinBtn != null && !joinBtn.IsEnabled;

                    var cancelBtn = window.FindName("CancelButton") as Button;
                    cancelButtonFound = cancelBtn != null;
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
            Assert.True(createButtonFound);
            Assert.True(joinButtonFound);
            Assert.True(cancelButtonFound);
        }

        [Fact]
        public void RoomCreatedDialog_InitializesSuccessfully()
        {
            Exception? exception = null;
            bool copyButtonFound = false;
            bool okButtonFound = false;

            var thread = new Thread(() =>
            {
                try
                {
                    var dialog = new RoomCreatedDialog("Тестова", "XYZ123");
                    Assert.NotNull(dialog);

                    var copyBtn = dialog.FindName("CopyButton") as Button;
                    copyButtonFound = copyBtn != null;

                    var okBtn = dialog.FindName("OkButton") as Button;
                    okButtonFound = okBtn != null;
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
            Assert.True(copyButtonFound);
            Assert.True(okButtonFound);
        }

        [Fact]
        public void RoomDetailsWindow_InitializesSuccessfully()
        {
            Exception? exception = null;
            bool roomCodeButtonFound = false;
            bool exitButtonFound = false;

            var thread = new Thread(() =>
            {
                try
                {
                    var mockRoom = new Mock<IRoomService>();
                    var roomDto = new FourTaste.ApplicationCore.Models.RoomDto
                    {
                        Id = 1,
                        Name = "Моя кімната",
                        OwnerId = 10,
                        InviteCode = "K7W92X"
                    };
                    var window = new RoomDetailsWindow(roomDto, mockRoom.Object);
                    Assert.NotNull(window);

                    var codeBtn = window.FindName("RoomCodeButton") as Button;
                    roomCodeButtonFound = codeBtn != null;

                    var exitBtn = window.FindName("ExitButton") as Button;
                    exitButtonFound = exitBtn != null;
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
            Assert.True(roomCodeButtonFound);
            Assert.True(exitButtonFound);
        }

        [Fact]
        public void UserProfileWindow_InitializesSuccessfully()
        {
            Exception? exception = null;
            bool homeButtonFound = false;

            var thread = new Thread(() =>
            {
                try
                {
                    var mockUserRepo = new Mock<IUserRepository>();
                    var mockLogger = new Mock<Serilog.ILogger>();
                    var mockMealRepo = new Mock<IMealPlanRepository>();
                    var mockRecipeRepo = new Mock<IRecipeRepository>();

                    var mockMealPlan = new Mock<MealPlanService>(MockBehavior.Loose, mockMealRepo.Object, mockLogger.Object);
                    var mockAuth = new Mock<AuthService>(MockBehavior.Loose, mockUserRepo.Object, mockLogger.Object);
                    var mockAllergy = new Mock<IAllergyService>();
                    var mockUser = new Mock<UserService>(MockBehavior.Loose, mockUserRepo.Object, mockLogger.Object);
                    var mockRecipe = new Mock<IRecipeService>();
                    var mockFridge = new Mock<IFridgeService>();

                    var window = new UserProfileWindow(
                        mockMealPlan.Object,
                        mockAuth.Object,
                        mockAllergy.Object,
                        mockUser.Object,
                        mockRecipe.Object,
                        mockFridge.Object);
                    Assert.NotNull(window);

                    var homeBtn = window.FindName("HomeButton") as Button;
                    homeButtonFound = homeBtn != null;
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
            Assert.True(homeButtonFound);
        }
    }
}
