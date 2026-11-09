namespace Faultline.Runtime.UI
{
    using System;
    using System.Collections.Generic;

    using UnityEngine;

    using Faultline.Domain;
    using Faultline.Presentation;

    /// <summary>
    /// Unity implementation of <see cref="IScreenNavigator"/>. Owns the
    /// explicit application state machine and the back stack, and activates the
    /// panel GameObjects that render each screen. Transitions are validated
    /// before anything is shown, so an unreachable state can never half-open a
    /// screen.
    /// </summary>
    public sealed class UnityScreenNavigator : IScreenNavigator
    {
        /// <summary>Screen shown when an application state is entered.</summary>
        private static readonly Dictionary<ApplicationState, string> DefaultScreens =
            new()
            {
                { ApplicationState.Booting, "Boot" },
                { ApplicationState.Ready, "Welcome" },
                { ApplicationState.ScenarioActive, "InvestigationWorkspace" },
                { ApplicationState.Paused, "PauseMenu" },
                { ApplicationState.ErrorRecovery, "ErrorRecovery" },
            };

        private static readonly Dictionary<ApplicationState, HashSet<ApplicationState>> Allowed = new()
        {
            { ApplicationState.Booting, new() { ApplicationState.Ready, ApplicationState.ErrorRecovery, ApplicationState.ShuttingDown } },
            { ApplicationState.Ready, new() { ApplicationState.ScenarioActive, ApplicationState.ErrorRecovery, ApplicationState.ShuttingDown } },
            { ApplicationState.ScenarioActive, new() { ApplicationState.Paused, ApplicationState.Ready, ApplicationState.ErrorRecovery, ApplicationState.ShuttingDown } },
            { ApplicationState.Paused, new() { ApplicationState.ScenarioActive, ApplicationState.Ready, ApplicationState.ErrorRecovery, ApplicationState.ShuttingDown } },
            { ApplicationState.ErrorRecovery, new() { ApplicationState.Booting, ApplicationState.Ready, ApplicationState.ScenarioActive, ApplicationState.ShuttingDown } },
            { ApplicationState.ShuttingDown, new() },
        };

        private readonly Dictionary<string, IScreen> _screens = new(StringComparer.Ordinal);
        private readonly List<IScreen> _backStack = new();
        private ApplicationState _currentState = ApplicationState.Booting;

        public ApplicationState CurrentState => _currentState;
        public IScreen CurrentScreen { get; private set; }
        public IReadOnlyList<IScreen> BackStack => _backStack;

        public event EventHandler<ApplicationStateChangedEventArgs> StateChanged;
        public event EventHandler<ScreenChangedEventArgs> ScreenChanged;

        public void RegisterScreen(string screenId, IScreen screen)
        {
            if (string.IsNullOrWhiteSpace(screenId))
                throw new ArgumentException("Screen id is required.", nameof(screenId));
            if (screen == null)
                throw new ArgumentNullException(nameof(screen));
            if (!_screens.TryGetValue(screenId, out var existing) || !ReferenceEquals(existing, screen))
            {
                if (existing != null)
                    Debug.LogWarning($"UnityScreenNavigator: replacing screen registration for '{screenId}'.");
                _screens[screenId] = screen;
            }
        }

        public bool CanTransitionTo(ApplicationState targetState)
        {
            if (targetState == _currentState) return CurrentScreen != null;
            return Allowed.TryGetValue(_currentState, out var targets) && targets.Contains(targetState);
        }

        public Result<bool> TransitionTo(ApplicationState targetState)
        {
            if (!CanTransitionTo(targetState))
            {
                return Result<bool>.Failure("FL-UI-0021",
                    $"Transition {_currentState} → {targetState} is not allowed.");
            }

            var previous = _currentState;

            if (targetState == ApplicationState.ShuttingDown)
            {
                if (CurrentScreen != null) Deactivate(CurrentScreen);
                _currentState = targetState;
                StateChanged?.Invoke(this, new ApplicationStateChangedEventArgs(
                    previous, targetState, DateTime.UtcNow.Ticks));
                return Result<bool>.Success(true);
            }

            if (!DefaultScreens.TryGetValue(targetState, out var screenId))
            {
                return Result<bool>.Failure("FL-UI-0022",
                    $"No default screen is registered for state {targetState}.");
            }

            _currentState = targetState;
            StateChanged?.Invoke(this, new ApplicationStateChangedEventArgs(
                previous, targetState, DateTime.UtcNow.Ticks));

            return NavigateToScreen(screenId);
        }

        public Result<bool> NavigateToScreen(string screenId)
        {
            if (!_screens.TryGetValue(screenId, out var target))
            {
                return Result<bool>.Failure("FL-UI-0023",
                    $"Screen '{screenId}' is not registered. Known: {string.Join(", ", _screens.Keys)}.");
            }

            var previousScreen = CurrentScreen;
            if (previousScreen != null && ReferenceEquals(previousScreen, target))
                return Result<bool>.Success(true);

            if (previousScreen != null)
            {
                var exit = previousScreen.Exit();
                if (!exit.IsSuccess) return exit;
                _backStack.Add(previousScreen);
                if (_backStack.Count > 12) _backStack.RemoveAt(0);
            }

            var enter = target.Enter();
            if (!enter.IsSuccess)
            {
                Debug.LogWarning($"UnityScreenNavigator: entering '{screenId}' failed: {enter.ErrorMessage}");
                return enter;
            }

            CurrentScreen = target;
            ScreenChanged?.Invoke(this, new ScreenChangedEventArgs(
                previousScreen?.ScreenId, screenId, DateTime.UtcNow.Ticks));
            return Result<bool>.Success(true);
        }

        public Result<bool> GoBack()
        {
            for (var i = _backStack.Count - 1; i >= 0; i--)
            {
                var candidate = _backStack[i];
                _backStack.RemoveAt(i);
                if (!candidate.CanEnter) continue;
                return NavigateToScreen(candidate.ScreenId);
            }

            return Result<bool>.Failure("FL-UI-0024", "No screen to return to.");
        }

        /// <summary>Number of registered screens; used by the boot diagnostics line.</summary>
        public int RegisteredScreenCount => _screens.Count;

        public bool TryGetScreen(string screenId, out IScreen screen) =>
            _screens.TryGetValue(screenId, out screen);

        private static void Deactivate(IScreen screen)
        {
            var result = screen.Exit();
            if (!result.IsSuccess)
                Debug.LogWarning($"UnityScreenNavigator: exiting '{screen.ScreenId}' failed: {result.ErrorMessage}");
        }
    }
}