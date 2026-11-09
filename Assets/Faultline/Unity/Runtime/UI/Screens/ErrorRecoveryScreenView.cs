namespace Faultline.Runtime.UI.Screens
{
    using System;

    using UnityEngine;

    using Faultline.Presentation;
    using Faultline.Runtime.UI;

    /// <summary>
    /// PRD §6.1 screen 23 — error recovery. The code shown is the code the domain
    /// or the boot path actually produced; nothing is re-invented here. Retry is
    /// offered only where replaying the boot path can genuinely change the
    /// outcome, and stays visible but disabled with the reason where it cannot.
    /// </summary>
    public sealed class ErrorRecoveryScreenView : ScreenViewBase, ErrorRecoveryScreen
    {
        private bool _bootErrorReplayed;
        private string _errorCode;
        private string _guidance;
        private bool _canRetry;
        private string _lastDisplayedCode;
        private string _actionNote;

        private FlButton _retryButton;
        private FlButton _backButton;

        public override string ScreenId => ScreenIds.ErrorRecovery;
        public override string Title => "Error recovery";

        // Back target is context-dependent: the run being resumed, or welcome
        // when there is nothing running, is decided by the state machine.
        public override string BackScreenId => null;
        public override string EntryConditions => "An FL- error requires a user decision.";
        public override string ExitConditions => "The error was retried, stepped back from, or the app aborted.";
        public override string InteractionModel =>
            "Pinch Retry when the line says it can change the outcome, Back to return, or " +
            "Abort to quit the session.";
        public override string AccessibleDescription =>
            "Error recovery. Reads the FL- code and the guidance text aloud, and states whether " +
            "retry is available before offering Back or Abort.";

        public string ErrorCode => _errorCode;
        public string UserGuidance => _guidance;
        public bool CanRetry => _canRetry;

        public event EventHandler<ErrorCodeEventArgs> ErrorDisplayed;
        public event EventHandler RetryRequested;
        public event EventHandler BackRequested;
        public event EventHandler AbortRequested;

        protected override FlPanel BuildPanel() =>
            CreatePanelInZone("NEEDS ATTENTION", SpatialZone.Centre, 700f, 470f);

        protected override void BuildContent(FlPanel panel)
        {
            _retryButton = panel.AddButton("recovery-retry", "Retry",
                "Re-run the boot path that produced this error", OnRetry);
            _backButton = panel.AddButton("recovery-back", "Back",
                "Leave the error screen and return to the run or to the welcome screen", OnBack);
            panel.AddButton("recovery-abort", "Abort session",
                "Close the XR session and quit the application", OnAbort);

            App.RegisterCriticalAction("recovery-abort", "ErrorRecovery.AbortSession");

            App.Changed += (_, _) => Refresh();
            Refresh();
        }

        protected override void OnEntered()
        {
            _actionNote = null;
            Refresh();
        }

        private void OnRetry()
        {
            if (!CanRetry) return;
            RetryRequested?.Invoke(this, EventArgs.Empty);

            var booting = App.Navigator.TransitionTo(ApplicationState.Booting);
            if (!booting.IsSuccess)
            {
                Report(booting.ErrorCode, booting.ErrorMessage, "retrying the boot path");
                return;
            }

            var ready = App.Navigator.TransitionTo(ApplicationState.Ready);
            if (!ready.IsSuccess)
            {
                App.Navigator.NavigateToScreen(ScreenIds.ErrorRecovery);
                Report(ready.ErrorCode, ready.ErrorMessage, "returning to the ready state");
                return;
            }

            // BootError is never rewritten by the application root, so the screen
            // records on its own that the boot path has since completed.
            _bootErrorReplayed = true;
            _errorCode = null;
            _guidance = null;
            _canRetry = false;
            _lastDisplayedCode = null;
        }

        private void OnBack()
        {
            BackRequested?.Invoke(this, EventArgs.Empty);

            var target = App.Controller != null && App.Controller.IsScenarioRunning
                ? ApplicationState.ScenarioActive
                : ApplicationState.Ready;

            var transition = App.Navigator.TransitionTo(target);
            if (transition.IsSuccess) return;

            _actionNote = $"{transition.ErrorCode} · {target} was refused, so this screen stayed open.";
            Refresh();
        }

        private void OnAbort()
        {
            AbortRequested?.Invoke(this, EventArgs.Empty);
            if (Panel != null)
                Panel.SetStatusLine("Quitting the session…");
            Application.Quit();
        }

        private void Report(string code, string message, string operation)
        {
            _actionNote = $"{code} · the failure is still present after {operation}: {message}";
            Refresh();
        }

        private void Refresh()
        {
            if (Panel == null) return;

            var source = ResolveError();
            if (source == null)
            {
                SetEmpty("No error is recorded for this session. Open Welcome to continue, " +
                        "or Diagnostics to check the XR provider.");
                Panel.SetStatusLine(_actionNote ?? "Nothing to retry — no error is recorded.");
                _retryButton.Interactable = false;
                _backButton.Interactable = !App.IsBusy;
                return;
            }

            _errorCode = ReadCode(source);
            _canRetry = RetryAppliesTo(_errorCode);
            _guidance = Guidance(source, _errorCode);

            SetError(_errorCode, _guidance, _canRetry);
            Panel.SetStatusLine(_actionNote ?? StatusLine(_errorCode));

            if (!string.Equals(_lastDisplayedCode, _errorCode, StringComparison.Ordinal))
            {
                _lastDisplayedCode = _errorCode;
                ErrorDisplayed?.Invoke(this, new ErrorCodeEventArgs(_errorCode, _guidance));
            }

            _retryButton.Interactable = _canRetry && !App.IsBusy;
            _backButton.Interactable = !App.IsBusy;
        }

        private string StatusLine(string code) =>
            _canRetry
                ? App.Messages.RetryAvailable + " Replay the boot path from this screen."
                : App.Messages.NotRetryable + " Back leaves the error; Abort ends the session. (" +
                  code + ")";

        private string ResolveError()
        {
            if (!_bootErrorReplayed && !string.IsNullOrWhiteSpace(App.BootError))
                return App.BootError;

            var domain = App.Controller?.LastError;
            return string.IsNullOrWhiteSpace(domain) ? null : domain;
        }

        /// <summary>
        /// Only boot and navigation failures are worth replaying here: the retry
        /// runs the same Booting → Ready transition. Domain rule rejections are
        /// deterministic, so replaying them cannot produce a different result.
        /// </summary>
        private static bool RetryAppliesTo(string code) =>
            code != null && (code.StartsWith("FL-XR", StringComparison.Ordinal) ||
                             code.StartsWith("FL-UI", StringComparison.Ordinal) ||
                             code.StartsWith("FL-RT", StringComparison.Ordinal));

        private static string ReadCode(string message)
        {
            var separator = message.IndexOf(':');
            return separator > 0 ? message.Substring(0, separator).Trim() : message.Trim();
        }

        private string Guidance(string message, string code)
        {
            var separator = message.IndexOf(':');
            var detail = separator > 0 && separator < message.Length - 1
                ? message.Substring(separator + 1).Trim()
                : string.Empty;
            var next = code.StartsWith("FL-XR", StringComparison.Ordinal)
                ? "Confirm the headset is connected, then pinch Retry to replay the boot path."
                : code.StartsWith("FL-UI", StringComparison.Ordinal)
                    ? "Retry replays the boot path. Back returns to the last working screen."
                    : code.StartsWith("FL-RT", StringComparison.Ordinal)
                        ? "A command faulted part-way; Retry replays the boot path, and Back " +
                          "keeps whatever state the run already reached."
                        : "This state was refused by a domain rule. Pinch Back and complete the " +
                          "step the command asked for first.";
            return string.IsNullOrEmpty(detail) ? next : detail + " " + next;
        }
    }
}
