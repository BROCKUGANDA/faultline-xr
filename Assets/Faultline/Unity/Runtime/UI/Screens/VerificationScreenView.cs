namespace Faultline.Runtime.UI.Screens
{
    using System;
    using System.Threading.Tasks;

    using UnityEngine;

    using Faultline.App;
    using Faultline.Domain;
    using Faultline.Presentation;
    using Faultline.Runtime.UI;

    /// <summary>
    /// PRD §6.1 screen 16 — verification. Every check compares real simulated
    /// numbers captured before the recovery command with the live engine state
    /// after it (root service health + aggregate health). <see cref="AllChecksPassed"/>
    /// is only true when the domain's <c>VerifyRecovery</c> command succeeds;
    /// re-running genuinely re-invokes verification whenever the domain still
    /// permits it.
    /// </summary>
    public sealed class VerificationScreenView : ScreenViewBase, VerificationScreen
    {
        private bool _allChecksPassed;
        private bool _rootHealthImproved;
        private bool _aggregateHealthImproved;
        private bool _rootBaselineKnown;
        private string _checkDetail = string.Empty;

        private FlButton _confirmButton;
        private FlButton _reRunButton;
        private FlButton _continueButton;

        public override string ScreenId => ScreenIds.Verification;
        public override string Title => "Verification";
        public override string BackScreenId => ScreenIds.RecoverySequence;
        public override string EntryConditions => "The recovery sequence has completed.";
        public override string ExitConditions => "Verification passed or failed with an honest result.";
        public override string InteractionModel =>
            "Pinch Confirm verification, Re-run checks, or Continue to results.";
        public override string AccessibleDescription =>
            "Verification screen. Before and after simulated health numbers with pass, fail, " +
            "or pending stated as text and a bracket glyph, never colour alone.";

        public string PlanId => App?.Controller?.ProposalId;
        public bool AllChecksPassed => _allChecksPassed;

        public event EventHandler<PlanIdEventArgs> VerificationOpened;
        public event EventHandler ConfirmVerificationRequested;
        public event EventHandler ReRunCheckRequested;

        protected override FlPanel BuildPanel() =>
            CreatePanelInZone("Verification", SpatialZone.Centre, 700f, 540f);

        protected override void BuildContent(FlPanel panel)
        {
            _confirmButton = panel.AddButton("verification-confirm", "Confirm verification",
                "Run the domain verification command against the simulated metrics",
                () => RunVerification(raiseReRun: false));
            _reRunButton = panel.AddButton("verification-rerun", "Re-run checks",
                "Re-read the simulated numbers and re-invoke verification while the domain permits it",
                () => RunVerification(raiseReRun: true));
            _continueButton = panel.AddButton("verification-continue", "Continue to results",
                "Open the outcome summary", OnContinue);

            App.Changed += (_, _) => Refresh();
            Refresh();
        }

        protected override bool EvaluateCanEnter()
        {
            var phase = App?.Controller?.Phase;
            return phase == InvestigationPhase.Executed ||
                   phase == InvestigationPhase.Verified ||
                   phase == InvestigationPhase.Resolved;
        }

        protected override void OnEntered()
        {
            ComputeChecks();
            VerificationOpened?.Invoke(this, new PlanIdEventArgs(PlanId));
            Refresh();
        }

        private void RunVerification(bool raiseReRun)
        {
            if (App.IsBusy) return;

            if (raiseReRun) ReRunCheckRequested?.Invoke(this, EventArgs.Empty);
            else ConfirmVerificationRequested?.Invoke(this, EventArgs.Empty);

            // A new run clears any previous error presentation.
            SetLoading();

            // Verification is a synchronous domain command; it still goes
            // through RunAsync so controls stay disabled and completion is
            // reported on the main thread like every other async path.
            App.RunAsync(() => Task.FromResult(App.VerifyRemediation()),
                "Verifying recovery against simulated metrics…", OnVerificationCompleted);
            Refresh();
        }

        private void OnVerificationCompleted(Result<bool> result)
        {
            ComputeChecks();

            if (!result.IsSuccess)
            {
                SetError(result.ErrorCode,
                    "The domain refused this verification run — typically because this plan " +
                    "was already verified or never executed. Live numbers:\n" + _checkDetail,
                    canRetry: App.Controller?.Phase == InvestigationPhase.Executed);
                Refresh();
                return;
            }

            // AllChecksPassed follows the domain verdict, not the visuals.
            _allChecksPassed = result.Value;
            if (result.Value) SetContent();
            else Panel?.SetState(VisualState.Critical);
            Refresh();
            if (result.Value)
                Panel?.SetStatusLine(App.Messages.VerifiedBody(ResolveRootStatus()));
        }

        /// <summary>
        /// Real before/after comparison. The "before" prefers the baseline the
        /// recovery screen captured at command dispatch; otherwise the
        /// aggregate pre-execution health is derived from the domain-recorded
        /// <c>RecoveryResult.HealthDelta</c>. No number is invented.
        /// </summary>
        private void ComputeChecks()
        {
            _rootHealthImproved = false;
            _aggregateHealthImproved = false;
            _rootBaselineKnown = false;

            var controller = App?.Controller;
            var engine = controller?.Engine;
            var recovery = controller?.Recovery;
            if (engine == null) return;

            var aggregateAfter = engine.AggregateHealth;
            var rootId = controller.RootCauseNodeId;
            var hasRoot = engine.Nodes.TryGetValue(rootId, out var rootNode);

            double aggregateBefore = double.NaN;
            double rootBefore = double.NaN;

            if (TryGetRecoveryView(out var recoveryView) && recoveryView.HasExecutionBaseline)
            {
                aggregateBefore = recoveryView.AggregateBeforeExecution;
                rootBefore = recoveryView.RootHealthBeforeExecution;
            }
            else if (recovery != null)
            {
                // healthDelta was computed by RemediationService as
                // post-execution minus pre-execution aggregate health.
                aggregateBefore = aggregateAfter - recovery.HealthDelta;
            }

            _aggregateHealthImproved = !double.IsNaN(aggregateBefore) &&
                                       aggregateAfter > aggregateBefore;
            _rootHealthImproved = hasRoot && !double.IsNaN(rootBefore) &&
                                  rootNode.HealthScore > rootBefore;
            _rootBaselineKnown = !double.IsNaN(rootBefore);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(_rootBaselineKnown
                ? $"Root service '{rootId}': health {rootBefore:P0} before → " +
                  $"{(hasRoot ? rootNode.HealthScore : double.NaN):P0} after · " +
                  $"status {(hasRoot ? rootNode.Status.ToString() : "unknown")} → " +
                  Check(_rootHealthImproved))
                : $"Root service '{rootId}': no pre-execution baseline was recorded → Pending");
            sb.AppendLine(double.IsNaN(aggregateBefore)
                ? "Aggregate health: no pre-execution record → Pending"
                : $"Aggregate health: {aggregateBefore:P0} before → {aggregateAfter:P0} after · " +
                  Check(_aggregateHealthImproved));
            sb.AppendLine(recovery == null
                ? "Domain result record: absent → Pending"
                : $"Domain result: success={(recovery.Success ? "yes" : "no")}, " +
                  $"verification flag={(recovery.VerificationPassed ? "passed" : "not passed")} · " +
                  "Recovery is claimed only from this record, never from animation.");
            _checkDetail = sb.ToString();
        }

        private bool TryGetRecoveryView(out RecoverySequenceScreenView view)
        {
            view = null;
            if (App?.Navigator == null) return false;
            if (!App.Navigator.TryGetScreen(ScreenIds.RecoverySequence, out var screen)) return false;
            view = screen as RecoverySequenceScreenView;
            return view != null;
        }

        private string ResolveRootStatus()
        {
            var controller = App?.Controller;
            if (controller?.Engine == null) return "unknown";
            return controller.Engine.Nodes.TryGetValue(controller.RootCauseNodeId, out var node)
                ? node.Status.ToString()
                : "unknown";
        }

        private static string Check(bool passed) => passed ? "[OK] pass" : "[X] fail";

        private void OnContinue()
        {
            if (App.IsBusy) return;
            App.Navigator.NavigateToScreen(ScreenIds.Results);
        }

        private void Refresh()
        {
            if (Panel == null) return;

            if (!_rootBaselineKnown && App?.Controller?.Recovery != null) ComputeChecks();

            if (!HasError)
            {
                var verdict = _allChecksPassed ? "[OK] VERIFIED" : "[-] NOT YET VERIFIED";
                Panel.SetBody($"{verdict}\n\n{_checkDetail}");
                Panel.SetState(_allChecksPassed ? VisualState.Selected : VisualState.Normal);
                Panel.SetStatusLine(App.IsBusy ? App.BusyStatus :
                    $"Plan {FaultlineMessages.ShortId(PlanId ?? string.Empty)} · " +
                    $"phase {App.Controller?.Phase.ToString() ?? "n/a"}");
            }

            var runnable = !App.IsBusy;
            _confirmButton.Interactable = runnable;
            _reRunButton.Interactable = runnable;
            _continueButton.Interactable = runnable &&
                App.Controller?.Phase is InvestigationPhase.Verified or InvestigationPhase.Resolved;
        }
    }
}
