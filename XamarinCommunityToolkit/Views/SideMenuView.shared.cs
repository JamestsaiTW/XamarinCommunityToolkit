using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using static System.Math;
using static Xamarin.Forms.Device;
using System.ComponentModel;

namespace XamarinCommunityToolkit.Views
{
    [ContentProperty(nameof(Children))]
    public class SideMenuView : TemplatedView
    {
        #region Private Settings

        const string animationName = nameof(SideMenuView);

        const uint animationRate = 16;

        const uint animationLength = 350;

        const int maxTimeDiffItemsCount = 24;

        const int minSwipeTimeDiffItemsCount = 2;

        const double swipeThresholdDistance = 17;

        const double acceptMoveThresholdPercentage = 0.3;

        static readonly Easing animationEasing = Easing.SinOut;

        static readonly TimeSpan swipeThresholdTime = TimeSpan.FromMilliseconds(RuntimePlatform == Android ? 100 : 60);

        #endregion

        #region Public Bindable Properties

        public static readonly BindableProperty DiffProperty
            = BindableProperty.Create(nameof(Diff), typeof(double), typeof(SideMenuView), 0.0, BindingMode.OneWayToSource);

        public static readonly BindableProperty CurrentGestureDiffProperty
            = BindableProperty.Create(nameof(CurrentGestureDiff), typeof(double), typeof(SideMenuView), 0.0, BindingMode.OneWayToSource);

        public static readonly BindableProperty GestureThresholdProperty
            = BindableProperty.Create(nameof(GestureThreshold), typeof(double), typeof(SideMenuView), 7.0);

        public static readonly BindableProperty CancelVerticalGestureThresholdProperty
            = BindableProperty.Create(nameof(CancelVerticalGestureThreshold), typeof(double), typeof(SideMenuView), 1.0);

        public static readonly BindableProperty ShouldThrottleGestureProperty
            = BindableProperty.Create(nameof(ShouldThrottleGesture), typeof(bool), typeof(SideMenuView), false);

        public static readonly BindableProperty StateProperty
            = BindableProperty.Create(nameof(State), typeof(SideMenuState), typeof(SideMenuView), SideMenuState.Default, BindingMode.TwoWay, propertyChanged: OnStatePropertyChanged);

        public static readonly BindableProperty CurrentGestureStateProperty
            = BindableProperty.Create(nameof(CurrentGestureState), typeof(SideMenuState), typeof(SideMenuView), SideMenuState.Default, BindingMode.OneWayToSource);

        #endregion

        #region Public Attached Properties

        public static readonly BindableProperty PositionProperty = BindableProperty.CreateAttached(nameof(GetPosition), typeof(SideMenuPosition), typeof(SideMenuView), SideMenuPosition.None);

        public static readonly BindableProperty MenuWidthPercentageProperty = BindableProperty.CreateAttached(nameof(GetMenuWidthPercentage), typeof(double), typeof(SideMenuView), -1.0);

        public static readonly BindableProperty MenuGestureEnabledProperty = BindableProperty.CreateAttached(nameof(GetMenuGestureEnabled), typeof(bool), typeof(SideMenuView), true);

        #endregion

        #region Private Fields

        readonly PanGestureRecognizer panGesture = new PanGestureRecognizer();

        readonly List<(DateTime Time, double Diff)> timeDiffItems = new List<(DateTime Time, double Diff)>();

        readonly View overlayView;

        View mainView;

        View leftMenu;

        View rightMenu;

        View activeMenu;

        View inactiveMenu;

        double zeroDiff;

        bool isGestureStarted;

        bool isGestureDirectionResolved;

        bool isSwipe;

        double previousDiff;

        #endregion

        #region Public Constructors

        public SideMenuView()
        {
            overlayView = SetupMainViewLayout(new BoxView
            {
                InputTransparent = true,
                GestureRecognizers =
                {
                    new TapGestureRecognizer
                    {
                        Command = new Command(() => State = SideMenuState.Default)
                    }
                }
            });
            Children.Add(overlayView);

            if (RuntimePlatform == Android)
            {
                return;
            }

            panGesture.PanUpdated += OnPanUpdated;
            GestureRecognizers.Add(panGesture);
        }

        #endregion

        #region Hidden API

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            var diff = e.TotalX;
            var verticalDiff = e.TotalY;
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    OnTouchStarted();
                    return;
                case GestureStatus.Running:
                    OnTouchChanged(diff, verticalDiff);
                    return;
                case GestureStatus.Canceled:
                case GestureStatus.Completed:
                    if (RuntimePlatform == Android)
                        OnTouchChanged(diff, verticalDiff);

                    OnTouchEnded();
                    return;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public async void OnSwiped(SwipeDirection swipeDirection)
        {
            if (RuntimePlatform == Android)
                return;

            await Task.Delay(1);
            if (isGestureStarted)
                return;

            UpdateState(ResolveSwipeState(swipeDirection == SwipeDirection.Right), true);
        }

        #endregion

        #region Public API

        public double Diff
        {
            get => (double)GetValue(DiffProperty);
            set => SetValue(DiffProperty, value);
        }

        public double CurrentGestureDiff
        {
            get => (double)GetValue(CurrentGestureDiffProperty);
            set => SetValue(CurrentGestureDiffProperty, value);
        }

        public double GestureThreshold
        {
            get => (double)GetValue(GestureThresholdProperty);
            set => SetValue(GestureThresholdProperty, value);
        }

        public double CancelVerticalGestureThreshold
        {
            get => (double)GetValue(CancelVerticalGestureThresholdProperty);
            set => SetValue(CancelVerticalGestureThresholdProperty, value);
        }

        public bool ShouldThrottleGesture
        {
            get => (bool)GetValue(ShouldThrottleGestureProperty);
            set => SetValue(ShouldThrottleGestureProperty, value);
        }

        public SideMenuState State
        {
            get => (SideMenuState)GetValue(StateProperty);
            set => SetValue(StateProperty, value);
        }

        public SideMenuState CurrentGestureState
        {
            get => (SideMenuState)GetValue(CurrentGestureStateProperty);
            set => SetValue(CurrentGestureStateProperty, value);
        }

        public static SideMenuPosition GetPosition(BindableObject bindable)
            => (SideMenuPosition)bindable.GetValue(PositionProperty);

        public static void SetPosition(BindableObject bindable, SideMenuPosition value)
            => bindable.SetValue(PositionProperty, value);

        public static double GetMenuWidthPercentage(BindableObject bindable)
            => (double)bindable.GetValue(MenuWidthPercentageProperty);

        public static void SetMenuWidthPercentage(BindableObject bindable, double value)
            => bindable.SetValue(MenuWidthPercentageProperty, value);

        public static bool GetMenuGestureEnabled(BindableObject bindable)
            => (bool)bindable.GetValue(MenuGestureEnabledProperty);

        public static void SetMenuGestureEnabled(BindableObject bindable, bool value)
            => bindable.SetValue(MenuGestureEnabledProperty, value);

        #endregion

        #region Protected Overriden Methods

        protected override void OnAdded(View view)
        {
            base.OnAdded(view);
            HandleViewAdded(view);
        }

        protected override void OnRemoved(View view)
        {
            base.OnRemoved(view);
            HandleViewRemoved(view);
        }

        protected override void LayoutChildren(double x, double y, double width, double height)
        {
            base.LayoutChildren(x, y, width, height);
            if (mainView == null)
                return;

            RaiseChild(mainView);
            RaiseChild(overlayView);
        }

        #endregion

        #region Private Methods

        static void OnStatePropertyChanged(BindableObject bindable, object oldValue, object newValue)
            => ((SideMenuView)bindable).OnStatePropertyChanged();

        void OnStatePropertyChanged()
            => PerformAnimation();

        void OnTouchStarted()
        {
            if (isGestureStarted)
                return;

            isGestureDirectionResolved = false;
            isGestureStarted = true;
            zeroDiff = 0;
            PopulateDiffItems(0);
        }

        void OnTouchChanged(double diff, double verticalDiff)
        {
            if (!isGestureStarted || Abs(CurrentGestureDiff - diff) <= double.Epsilon)
                return;

            PopulateDiffItems(diff);
            var absDiff = Abs(diff);
            var absVerticalDiff = Abs(verticalDiff);
            if (!isGestureDirectionResolved && Max(absDiff, absVerticalDiff) > CancelVerticalGestureThreshold)
            {
                absVerticalDiff *= 2.5;
                if (absVerticalDiff >= absDiff)
                {
                    isGestureStarted = false;
                    OnTouchEnded();
                    return;
                }
                isGestureDirectionResolved = true;
            }

            mainView.AbortAnimation(animationName);
            var totalDiff = previousDiff + diff;
            if (!TryUpdateDiff(totalDiff - zeroDiff, false))
                zeroDiff = totalDiff - Diff;
        }

        void OnTouchEnded()
        {
            if (!isGestureStarted)
                return;

            isGestureStarted = false;
            CleanDiffItems();

            previousDiff = Diff;
            var state = State;
            var isSwipe = TryResolveFlingGesture(ref state);
            PopulateDiffItems(0);
            timeDiffItems.Clear();
            UpdateState(state, isSwipe);
        }

        void PerformAnimation()
        {
            var state = State;
            var start = Diff;
            var menuWidth = (state == SideMenuState.LeftMenuShown ? leftMenu : rightMenu)?.Width ?? 0;
            var end = Sign((int)state) * menuWidth;

            var animationLength = (uint)(SideMenuView.animationLength * Abs(start - end) / Width);
            if (isSwipe)
            {
                isSwipe = false;
                animationLength /= 2;
            }
            if (animationLength == 0)
            {
                SetOverlayViewInputTransparent(state);
                return;
            }
            var animation = new Animation(v => TryUpdateDiff(v, true), Diff, end);
            mainView.Animate(animationName, animation, animationRate, animationLength, animationEasing, (v, isCanceled) =>
            {
                if (isCanceled)
                    return;

                SetOverlayViewInputTransparent(state);
            });
        }

        void SetOverlayViewInputTransparent(SideMenuState state)
            => overlayView.InputTransparent = state == SideMenuState.Default;

        SideMenuState ResolveSwipeState(bool isRightSwipe)
        {
            var left = SideMenuState.LeftMenuShown;
            var right = SideMenuState.RightMenuShown;
            switch (State)
            {
                case SideMenuState.LeftMenuShown:
                    right = SideMenuState.Default;
                    SetActiveView(true);
                    break;
                case SideMenuState.RightMenuShown:
                    left = SideMenuState.Default;
                    SetActiveView(false);
                    break;
            }
            return isRightSwipe ? left : right;
        }

        bool TryUpdateDiff(double diff, bool shouldUpdatePreviousDiff)
        {
            SetActiveView(diff >= 0);
            if (activeMenu == null || !GetMenuGestureEnabled(activeMenu))
                return false;

            diff = Sign(diff) * Min(Abs(diff), activeMenu.Width);
            if (Abs(Diff - diff) <= double.Epsilon)
                return false;

            Diff = diff;
            SetCurrentGestureState(diff);
            if (shouldUpdatePreviousDiff)
                previousDiff = diff;
            
            mainView.TranslationX = diff;
            overlayView.TranslationX = diff;
            return true;
        }

        void SetCurrentGestureState(double diff)
        {
            var menuWidth = activeMenu?.Width ?? Width;
            var moveThreshold = menuWidth * acceptMoveThresholdPercentage;
            var absDiff = Abs(diff);
            var state = State;
            if (Sign(diff) != (int)state)
                state = SideMenuState.Default;

            if (state == SideMenuState.Default && absDiff <= moveThreshold ||
                state != SideMenuState.Default && absDiff < menuWidth - moveThreshold)
            {
                CurrentGestureState = SideMenuState.Default;
                return;
            }
            if (diff >= 0)
            {
                CurrentGestureState = SideMenuState.LeftMenuShown;
                return;
            }
            CurrentGestureState = SideMenuState.RightMenuShown;
        }

        void UpdateState(SideMenuState state, bool isSwipe)
        {
            this.isSwipe = isSwipe;
            if (State == state)
            {
                PerformAnimation();
                return;
            }
            State = state;
        }

        void SetActiveView(bool isLeft)
        {
            activeMenu = leftMenu;
            inactiveMenu = rightMenu;
            if (!isLeft)
            {
                activeMenu = rightMenu;
                inactiveMenu = leftMenu;
            }

            if (inactiveMenu == null ||
                activeMenu == null ||
                leftMenu.X + leftMenu.Width <= rightMenu.X ||
                Children.IndexOf(inactiveMenu) < Children.IndexOf(activeMenu))
                return;

            LowerChild(inactiveMenu);
        }

        bool TryResolveFlingGesture(ref SideMenuState state)
        {
            if (state != CurrentGestureState)
            {
                state = CurrentGestureState;
                return false;
            }

            if (timeDiffItems.Count < minSwipeTimeDiffItemsCount)
                return false;
            
            var lastItem = timeDiffItems.LastOrDefault();
            var firstItem = timeDiffItems.FirstOrDefault();
            var distDiff = lastItem.Diff - firstItem.Diff;

            if (Sign(distDiff) != Sign(lastItem.Diff))
                return false;

            var absDistDiff = Abs(distDiff);
            var timeDiff = lastItem.Time - firstItem.Time;

            var acceptValue = swipeThresholdDistance * timeDiff.TotalMilliseconds / swipeThresholdTime.TotalMilliseconds;

            if (absDistDiff < acceptValue)
                return false;

            state = ResolveSwipeState(distDiff > 0);
            return true;
        }

        void PopulateDiffItems(double diff)
        {
            CurrentGestureDiff = diff;

            if (timeDiffItems.Count > maxTimeDiffItemsCount)
                CleanDiffItems();

            timeDiffItems.Add((DateTime.UtcNow, diff));
        }

        void CleanDiffItems()
        {
            var time = timeDiffItems.LastOrDefault().Time;
            for (var i = timeDiffItems.Count - 1; i >= 0; --i)
                if (time - timeDiffItems[i].Time > swipeThresholdTime)
                    timeDiffItems.RemoveAt(i);
        }

        void HandleViewAdded(View view)
        {
            switch (GetPosition(view))
            {
                case SideMenuPosition.None:
                    mainView = SetupMainViewLayout(view);
                    break;
                case SideMenuPosition.LeftMenu:
                    leftMenu = SetupMenuLayout(view, true);
                    break;
                case SideMenuPosition.RightMenu:
                    rightMenu = SetupMenuLayout(view, false);
                    break;
            }
        }

        void HandleViewRemoved(View view)
        {
            switch (GetPosition(view))
            {
                case SideMenuPosition.None:
                    mainView = null;
                    break;
                case SideMenuPosition.LeftMenu:
                    leftMenu = null;
                    break;
                case SideMenuPosition.RightMenu:
                    rightMenu = null;
                    break;
            }

            if (activeMenu == view)
                activeMenu = null;
            else if (inactiveMenu == view)
                inactiveMenu = null;
        }

        View SetupMainViewLayout(View view)
        {
            SetLayoutFlags(view, AbsoluteLayoutFlags.All);
            SetLayoutBounds(view, new Rectangle(0, 0, 1, 1));
            return view;
        }

        View SetupMenuLayout(View view, bool isLeft)
        {
            var width = GetMenuWidthPercentage(view);
            var flags = width > 0
                ? AbsoluteLayoutFlags.All
                : AbsoluteLayoutFlags.PositionProportional | AbsoluteLayoutFlags.HeightProportional;
            SetLayoutFlags(view, flags);
            SetLayoutBounds(view, new Rectangle(isLeft ? 0 : 1, 0, width, 1));
            return view;
        }

        #endregion
    }
}
