namespace Faultline.Runtime.UI.Screens
{
    using System;

    using UnityEngine;

    using Faultline.App;
    using Faultline.Domain;
    using Faultline.Presentation;
    using Faultline.Runtime.UI;

    /// <summary>
    /// PRD §6.1 screen 15 — recovery sequence. The four steps mirror the phases
    /// <c>RemediationService.ExecuteAsync</c> actually runs (preconditions →
    /// checkpoint → apply → result). A step only advances when the domain
    /// record proves it completed; the panel never treats an animation as
    /// evidence of recovery.
    /// </summary>
    public sealed class RecoverySequenceScreenView : ScreenViewBase, RecoverySequenceScreen
    {
        private static readonly string[] PhaseLabels =
        {
            "Preconditions verified",
            "Simulation checkpoint created",
            "Recovery applied to affected services",
            "Recovery result recorded",
        };

        private int _currentStep;
        private double _aggregateBefore = double.NaN;
        private double _rootHealthBefore = double.NaN;

        private FlButton _preconditionsButton;
        private FlButton _executeButton;
        private FlButton _confirmResultButton;
        private FlButton _abortButton;

        public override string ScreenId => ScreenIds.RecoverySequence;
        public override string Title => "Recovery Sequence";
        public override string BackScreenId => ScreenIds.ApprovalConfirmation;
        public override string EntryConditions =>
            "A remediation proposal has been approved by the operator.";
        public override string ExitConditions =>
            "Recovery sequence complete, failed, or aborted back to approval.";
        public override string InteractionModel =>
            "Pinch each recovery step in order; Abort returns to the approval screen.";
        public override string AccessibleDescription =>
            "Recovery sequence screen. Four real remediation phases, each confirmed " +
            "only when the domain command succeeded. Progress is announced as text, " +
            "not colour alone.";

        public string PlanId => App?.Controller?.ProposalId;
        public int CurrentStep => _currentStep;
        public int TotalSteps => PhaseLabels.Length;
        public bool CanAdvance => !App.IsBusy && _currentStep < TotalSteps;

        public event EventHandler<PlanIdEventArgs> RecoveryStarted;
        public event EventHandler<RecoveryStepEventArgs> StepAdvanced;
        public event EventHandler RecoveryAborted;
        public event EventHandler RecoveryCompleted;

        /// <summary>
        /// Engine aggregate health captured at the instant the ExecuteAsync
        /// command was dispatched — the Verification screen compares against it.
        /// </summary>
        internal bool HasExecutionBaseline => !double.IsNaN(_aggregateBefore);
        internal double AggregateBeforeExecution => _aggregateBefore;
        internal double RootHealthBeforeExecution => _rootHealthBefore;

        protected override FlPanel BuildPanel() =>
            CreatePanelInZone("Recovery sequence", SpatialZone.Centre, 700f, 540f);

        protected override void BuildContent(FlPanel panel)
        {
            _preconditionsButton = panel.AddButton("recovery-preconditions",
                "1 · Check preconditions",
                "Verify the approved proposal is still executable before running the remediation",
                OnCheckPreconditions);
            _executeButton = panel.AddButton("recovery-execute",
                "2-3 · Execute remediation",
                "Run the approved remediation against the simulation; this creates a " +
                "checkpoint and applies the recovery as one domain command",
                OnExecute);
            _confirmResultButton = panel.AddButton("recovery-confirm-result",
                "4 · Confirm result",
                "Read back the recovery result recorded by the domain and continue to verification",
                OnConfirmResult);
            _abortButton = panel.AddButton("recovery-abort", "Abort",
                "Abort the recovery and go back to the approval screen", OnAbort);

            App.Changed += (_, _) => Refresh();
            Refresh();
        }

        protected override bool EvaluateCanEnter()
        {
            var phase = App?.Controller?.Phase;
            return phase == InvestigationPhase.Approved ||
                   phase == InvestigationPhase.Executed ||
                   phase == InvestigationPhase.Verified;
        }

        protected override void OnEntered() => Refresh();

        /// <summary>
        /// Real precondition probe using the same public domain state the
        /// remediation service checks at execution time (approved, unexpired,
        /// not yet executed).
        /// </summary>
        private void OnCheckPreconditions()
        {
            if (_currentStep != 0 || App.IsBusy) return;

            var controller = App.Controller;
            var proposalId = controller?.ProposalId;
            var ok = proposalId != null &&
                     controller.Phase == InvestigationPhase.Approved &&
                     controller.Remediation != null &&
                     controller.Remediation.Proposals.TryGetValue(proposalId, out var probe) &&
                     probe.CanExecute;

            if (!ok)
            {
                SetError("FL-RM-0080",
                    "The remediation is not approved, has expired, or was already executed. " +
                    "Return to approval and submit a fresh proposal.",
                    canRetry: false);
                return;
            }

            _currentStep = 1;
            RecoveryStarted?.Invoke(this, new PlanIdEventArgs(proposalId));
            StepAdvanced?.Invoke(this, new RecoveryStepEventArgs(0));
            SetContent();
            Refresh();
        }

        private void OnExecute()
        {
            if (_currentStep != 1 || App.IsBusy) return;

            if (App.Controller?.Recovery != null)
            {
                // Execution idempotency (FL-RM-0083): the domain already holds a
                // result record; there is nothing to execute a second time.
                _currentStep = Math.Max(_currentStep, 3);
                Refresh();
                return;
            }

            // Capture the live engine numbers at the moment the domain command
            // is dispatched; this is the honest "before" for verification.
            var engine = App.Controller.Engine;
            _aggregateBefore = engine != null ? engine.AggregateHealth : double.NaN;
            _rootHealthBefore = double.NaN;
            if (engine != null &&
                engine.Nodes.TryGetValue(App.Controller.RootCauseNodeId, out var root))
                _rootHealthBefore = root.HealthScore;

            App.RunAsync(() => App.ExecuteRemediationAsync(),
                "Executing remediation — checkpoint and apply…", OnExecuteCompleted);
            Refresh();
        }

        private void OnExecuteCompleted(Result<bool> result)
        {
            if (!result.IsSuccess)
            {
                // RemediationService restores the checkpoint on any failure and
                // refuses double execution, so no retry for an idempotency block.
                SetError(result.ErrorCode,
                    "The remediation did not complete. Any applied change was rolled " +
                    "back to the checkpoint; nothing was applied twice.",
                    canRetry: result.ErrorCode != "FL-RM-0083");
                Refresh();
                return;
            }

            // The single ExecuteAsync command runs the checkpoint and apply
            // phases atomically; the returned success is the domain record that
            // both completed, so each step advances on this evidence.
            SetContent();
            _currentStep = 3;
            StepAdvanced?.Invoke(this, new RecoveryStepEventArgs(1));
            StepAdvanced?.Invoke(this, new RecoveryStepEventArgs(2));
            Refresh();
        }

        private void OnConfirmResult()
        {
            if (_currentStep != 3 || App.IsBusy) return;

            var recovery = App.Controller?.Recovery;
            if (recovery == null)
            {
                SetError("FL-RM-0084",
                    "No recovery result was recorded by the domain, so there is " +
                    "nothing to confirm. Aborting returns to approval.",
                    canRetry: false);
                return;
            }

            _currentStep = 4;
            StepAdvanced?.Invoke(this, new RecoveryStepEventArgs(3));
            RecoveryCompleted?.Invoke(this, EventArgs.Empty);
            Refresh();
            App.Navigator.NavigateToScreen(ScreenIds.Verification);
        }

        private void OnAbort()
        {
            RecoveryAborted?.Invoke(this, EventArgs.Empty);
            App.Navigator.GoBack();
        }

        private void Refresh()
        {
            if (Panel == null) return;

            var recovery = App.Controller?.Recovery;
            if (recovery != null && _currentStep < 3) _currentStep = 3;

            if (!HasError)
            {
                var sb = new System.Text.StringBuilder();
                for (var i = 0; i < PhaseLabels.Length; i++)
                {
                    var marker = i < _currentStep ? "[x]" : (i == _currentStep ? "[>]" : "[ ]");
                    sb.AppendLine($"{marker} {i + 1}. {PhaseLabels[i]}");
                }

                if (recovery != null)
                {
                    sb.AppendLine();
                    sb.AppendLine($"Health delta recorded: {recovery.HealthDelta:P0} " +
                                  $"(aggregate now {App.CurrentAggregateHealth:P0})");
                    sb.AppendLine($"Execution succeeded: {(recovery.Success ? "yes" : "no")} · " +
                                  $"Domain verification flag: {(recovery.VerificationPassed ? "passed" : "not yet passed")}");
                }
                else
                {
                    sb.AppendLine();
                    sb.AppendLine("No recovery has been executed yet. Recovery is only " +
                                  "claimed when the domain records it — not from visuals.");
                }

                Panel.SetBody(sb.ToString());
                Panel.SetStatusLine(App.IsBusy ? App.BusyStatus :
                    $"Plan {FaultlineMessages.ShortId(App?.Controller?.ProposalId ?? string.Empty)} · " +
                    $"phase {App?.Controller?.Phase.ToString() ?? "n/a"}");
            }

            _preconditionsButton.Interactable = !App.IsBusy && _currentStep == 0;
            _executeButton.Interactable = !App.IsBusy && _currentStep == 1 && recovery == null;
            _confirmResultButton.Interactable = !App.IsBusy && _currentStep == 3 && recovery != null;
            _abortButton.Interactable = !App.IsBusy && _currentStep < TotalSteps;
        }
    }
}
