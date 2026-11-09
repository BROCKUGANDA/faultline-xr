namespace Faultline.Runtime.UI.Screens
{
    using System;
    using System.Collections.Generic;

    using UnityEngine;

    using Faultline.App;
    using Faultline.Domain;
    using Faultline.Presentation;
    using Faultline.Remediation;
    using Faultline.Runtime.UI;

    /// <summary>
    /// PRD §6.1 screen 14 — the human approval gate. The safety rule implemented
    /// here is that a single ambiguous pinch can never execute an approval: the first
    /// pinch on Approve only arms an explicit "Confirm approval" control, and only the
    /// second, separately targeted pinch calls the controller's approve command.
    /// </summary>
    public sealed class ApprovalConfirmationScreenView : ScreenViewBase, ApprovalConfirmationScreen
    {
        /// <summary>Reject reasons the operator may record; each is a real decision, not a stub.</summary>
        private static readonly (string Label, string Reason)[] RejectReasons =
        {
            ("Reject: evidence insufficient",
                "The cited evidence does not yet justify restarting the affected service."),
            ("Reject: blast radius too wide",
                "The restart covers more services than the containment requires."),
            ("Reject: reversibility unacceptable",
                "Partial reversibility is not acceptable for this service in this window.")
        };

        private FlButton _approveButton;
        private FlButton _confirmButton;
        private bool _confirmArmed;
        private bool _showingError;
        private string _openedPlanId;
        private string _lastRejectReason;

        public override string ScreenId => ScreenIds.ApprovalConfirmation;
        public override string Title => "Approval confirmation";
        public override string BackScreenId => ScreenIds.RemediationProposal;
        public override string EntryConditions =>
            "A recorded remediation proposal must be awaiting the human decision.";
        public override string ExitConditions =>
            "The proposal is approved, rejected, or dismissed back to the proposal.";
        public override string InteractionModel =>
            "Pinch Approve to arm the confirmation step, then pinch Confirm approval — two separate " +
            "targets, so one ambiguous pinch cannot approve. Reject records one of the listed reasons.";
        public override string AccessibleDescription =>
            "Approval gate. Reads the proposal's action, affected services and reversibility, and " +
            "requires an explicit second pinch before the approval is executed.";

        public string PlanId => App?.Controller?.ProposalId;

        public bool AlreadyApproved
        {
            get
            {
                var status = CurrentProposal()?.Status;
                return status == RemediationPlanStatus.Approved ||
                       status == RemediationPlanStatus.Executed ||
                       status == RemediationPlanStatus.Verified;
            }
        }

        public event EventHandler<PlanIdEventArgs> ApprovalOpened;
        public event EventHandler ApproveRequested;
        public event EventHandler RejectRequested;

        protected override SpatialZone ZoneForScreen() => SpatialZone.Centre;

        protected override FlPanel BuildPanel() =>
            CreatePanelInZone("APPROVAL GATE", SpatialZone.Centre, 720f, 640f);

        protected override void BuildContent(FlPanel panel)
        {
            _approveButton = panel.AddButton("approval-approve", "Approve (step 1 of 2)",
                "Arm the explicit confirmation step; no approval is recorded by this pinch",
                OnApprovePressed);

            _confirmButton = panel.AddButton("approval-confirm", "Confirm approval",
                "Confirm and record the approval for the proposal",
                OnConfirmPressed, interactable: false);

            foreach (var (label, reason) in RejectReasons)
                _ = panel.AddButton("approval-reject-" + ReasonKey(reason), label,
                    "Reject the proposal, recording this reason: " + reason,
                    () => OnReject(reason));

            _ = panel.AddButton("approval-back", "Back to proposal",
                "Return to the remediation proposal without deciding",
                () => App.Navigator.NavigateToScreen(ScreenIds.RemediationProposal));

            // The confirmation step is itself a critical action, so it must stay a
            // visible hand target and can never collapse into a hidden gesture.
            App.RegisterCriticalAction("confirm-approval", "ApprovalConfirmation.ConfirmApproval");

            App.Changed += (_, _) => Refresh();
        }

        protected override void OnEntered()
        {
            _confirmArmed = false;
            _showingError = false;
            Refresh();
        }

        protected override void OnExited() => _confirmArmed = false;

        private static string ReasonKey(string reason)
        {
            var key = reason.ToLowerInvariant();
            return key.Substring(0, Math.Min(12, key.Length)).Replace(" ", "-");
        }

        private void OnApprovePressed()
        {
            if (!CanApprove()) return;

            ApproveRequested?.Invoke(this, EventArgs.Empty);
            _confirmArmed = true;
            _showingError = false;
            Refresh();
        }

        private void OnConfirmPressed()
        {
            // Disarm first: the approval must be an explicit, single recorded act and
            // a repeated pinch cannot re-run it (FL-RM-0081 guard in the domain).
            _confirmArmed = false;

            if (!CanApprove() || AlreadyApproved || App.IsBusy)
            {
                Refresh();
                return;
            }

            var result = App.ApproveRemediation();
            if (!result.IsSuccess)
            {
                _showingError = true;
                SetError(result.ErrorCode, ApprovalFailureGuidance(result.ErrorCode, result.ErrorMessage),
                    canRetry: IsRetryable(result.ErrorCode));
                return;
            }

            Refresh();
            App.Navigator.NavigateToScreen(ScreenIds.RecoverySequence);
        }

        private void OnReject(string reason)
        {
            if (App.IsBusy) return;
            if (CurrentProposal() == null || AlreadyApproved) return;

            RejectRequested?.Invoke(this, EventArgs.Empty);

            var result = App.RejectRemediation(reason);
            if (!result.IsSuccess)
            {
                _showingError = true;
                SetError(result.ErrorCode,
                    "The rejection was not accepted by the domain: " + result.ErrorMessage,
                    canRetry: IsRetryable(result.ErrorCode));
                return;
            }

            _showingError = false;
            _lastRejectReason = reason;
            _confirmArmed = false;
            Refresh();
        }

        private bool CanApprove()
        {
            var proposal = CurrentProposal();
            if (proposal == null || proposal.IsExpired) return false;
            if (proposal.Status != RemediationPlanStatus.Proposed) return false;
            return App?.Controller?.Phase == InvestigationPhase.Proposed;
        }

        private void Refresh()
        {
            if (!isActiveAndEnabled || Panel == null) return;

            var proposal = CurrentProposal();

            if (_showingError && proposal != null)
            {
                // Keep the error presentation until the operator acts again; the domain
                // still raises Changed on every simulation tick.
                Panel.SetStatusLine(App.StatusLine);
                SetControls(false, false);
                return;
            }

            _showingError = false;

            if (proposal == null)
            {
                _confirmArmed = false;
                SetEmpty("There is no proposal awaiting a decision. Propose a remediation from the " +
                         "investigation workspace first.");
                SetControls(false, false);
                return;
            }

            if (!string.Equals(_openedPlanId, proposal.ProposalId, StringComparison.Ordinal))
            {
                _openedPlanId = proposal.ProposalId;
                FocusTargetId = proposal.ProposalId;
                ApprovalOpened?.Invoke(this, new PlanIdEventArgs(proposal.ProposalId));
            }

            var action = proposal.Action;
            var rejected = App.Controller.Phase == InvestigationPhase.Rejected ||
                           proposal.Status == RemediationPlanStatus.Rejected;

            var lines = new List<string>
            {
                $"Proposal {FaultlineMessages.ShortId(proposal.ProposalId)} · incident " +
                $"{FaultlineMessages.ShortId(proposal.IncidentId)}",
                $"Action: {action?.Type.ToString() ?? "not recorded"} on {Affected(action)}",
                $"Reversibility: {action?.Reversibility.ToString() ?? "not recorded"}",
                $"Side effects: {SideEffects(action)}",
                ExpiryLine(proposal)
            };

            if (AlreadyApproved)
            {
                lines.Add(string.Empty);
                lines.Add($"Approval already recorded by {proposal.ApprovedBy ?? "an operator"} — status " +
                          $"{proposal.Status}. A second approval is refused (FL-RM-0081).");
                lines.Add("Continue in the recovery sequence to execute it.");
            }
            else if (rejected)
            {
                lines.Add(string.Empty);
                lines.Add(_lastRejectReason == null
                    ? App.Messages.RejectedBody("The rejection reason was recorded on the approval screen.")
                    : App.Messages.RejectedBody(_lastRejectReason));
                lines.Add("No change was applied to the simulation; the proposal is terminal.");
            }
            else if (proposal.IsExpired)
            {
                lines.Add(string.Empty);
                lines.Add("The approval window has closed, so approval would be refused " +
                          "(FL-RM-0082). Replay the scenario for a fresh proposal.");
            }
            else if (_confirmArmed)
            {
                lines.Add(string.Empty);
                lines.Add("STEP 2 REQUIRED — nothing has been approved yet. Pinch Confirm approval " +
                          "to record the decision.");
            }
            else
            {
                lines.Add(string.Empty);
                lines.Add("Approval is a two-pinch action: arm it, then confirm it on the second control.");
            }

            Panel.SetBody(string.Join(Environment.NewLine, lines));
            Panel.SetStatusLine(StatusLine(proposal, rejected));
            SetContent();

            var canAct = CanApprove() && !App.IsBusy;
            _confirmButton.Label = _confirmArmed ? ConfirmLabel(action) : "Confirm approval";

            SetControls(canAct, _confirmArmed && canAct);
        }

        private void SetControls(bool approveEnabled, bool confirmEnabled)
        {
            if (_approveButton != null) _approveButton.Interactable = approveEnabled;
            if (_confirmButton != null) _confirmButton.Interactable = confirmEnabled;

            var proposal = CurrentProposal();
            var canReject = proposal != null && !AlreadyApproved && !App.IsBusy &&
                            App.Controller.Phase != InvestigationPhase.Rejected &&
                            proposal.Status != RemediationPlanStatus.Rejected;

            foreach (var (_, reason) in RejectReasons)
                if (Panel.TryGetButton("approval-reject-" + ReasonKey(reason), out var rejectButton))
                    rejectButton.Interactable = canReject;
        }

        private static string ConfirmLabel(Faultline.Domain.RemediationAction action) =>
            action == null
                ? "Confirm approval"
                : $"Confirm approval — {action.Type}";

        private static string Affected(Faultline.Domain.RemediationAction action) =>
            action == null || action.AffectedServiceIds.Count == 0
                ? "none recorded"
                : string.Join(", ", action.AffectedServiceIds);

        private static string SideEffects(Faultline.Domain.RemediationAction action) =>
            action == null || action.SideEffects.Count == 0
                ? "none recorded"
                : string.Join(" · ", action.SideEffects);

        private string StatusLine(RemediationProposal proposal, bool rejected)
        {
            if (rejected) return "Decision recorded: rejected. No remediation was executed.";
            if (AlreadyApproved) return $"Decision recorded: {proposal.Status}.";
            if (proposal.IsExpired) return "Approval blocked: the recorded approval window has expired.";
            return _confirmArmed
                ? "Approval armed but NOT recorded — pinch Confirm approval."
                : App.StatusLine;
        }

        private RemediationProposal CurrentProposal()
        {
            var controller = App?.Controller;
            var proposalId = controller?.ProposalId;
            if (proposalId == null || controller.Remediation?.Proposals == null) return null;
            return controller.Remediation.Proposals.TryGetValue(proposalId, out var proposal)
                ? proposal
                : null;
        }

        private static string ExpiryLine(RemediationProposal proposal) =>
            !proposal.ExpiresAt.HasValue
                ? "Approval window: no expiry recorded."
                : proposal.IsExpired
                    ? $"Approval window: expired at {proposal.ExpiresAt.Value:u}."
                    : $"Approval window: open until {proposal.ExpiresAt.Value:u}.";

        private static bool IsRetryable(string errorCode) =>
            !string.IsNullOrEmpty(errorCode) &&
            errorCode != "FL-RM-0081" &&
            errorCode != "FL-RM-0082" &&
            errorCode != "FL-RT-001";

        private static string ApprovalFailureGuidance(string errorCode, string message) => errorCode switch
        {
            "FL-RM-0081" =>
                "This proposal has already been approved — a second approval is refused. " +
                "Open the recovery sequence to execute it.",
            "FL-RM-0082" =>
                "The approval window expired. Return to the workspace and replay for a fresh proposal.",
            "FL-RM-0080" =>
                "No proposal is awaiting approval, so nothing was recorded: " + message,
            _ =>
                "The approval was not recorded: " + message
        };
    }
}
