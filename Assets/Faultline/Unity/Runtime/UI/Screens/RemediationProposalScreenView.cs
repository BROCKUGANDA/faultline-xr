namespace Faultline.Runtime.UI.Screens
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    using UnityEngine;

    using Faultline.Domain;
    using Faultline.Presentation;
    using Faultline.Remediation;
    using Faultline.Runtime.UI;

    /// <summary>
    /// PRD §6.1 screen 13 — remediation proposal. Every field is read from the
    /// proposal the remediation service actually recorded (action, affected services,
    /// reversibility, benefit, side effects, expiry); submission is blocked, with the
    /// reason stated, when there is no proposal or its approval window has closed.
    /// </summary>
    public sealed class RemediationProposalScreenView : ScreenViewBase, RemediationProposalScreen
    {
        private string _openedProposalId;

        public override string ScreenId => ScreenIds.RemediationProposal;
        public override string Title => "Remediation proposal";
        public override string BackScreenId => ScreenIds.InvestigationWorkspace;
        public override string EntryConditions =>
            "A remediation proposal must be recorded for the incident.";
        public override string ExitConditions =>
            "The proposal is submitted for approval, or it is cancelled and the workspace is reopened.";
        public override string InteractionModel =>
            "Pinch Submit for approval to open the approval gate, Inspect evidence to re-read the " +
            "cited events, or Cancel to return to the workspace.";
        public override string AccessibleDescription =>
            "Remediation proposal. Announces the action type, affected services, reversibility, " +
            "expected benefit, side effects, status and whether the approval window is still open.";

        public string ProposalId => App?.Controller?.ProposalId;

        public bool CanSubmit
        {
            get
            {
                var proposal = CurrentProposal();
                return proposal != null && !proposal.IsExpired &&
                       proposal.Status == RemediationPlanStatus.Proposed;
            }
        }

        public event EventHandler<ProposalIdEventArgs> ProposalOpened;
        public event EventHandler SubmitForApprovalRequested;
        public event EventHandler CancelProposalRequested;

        protected override bool EvaluateCanEnter() => CurrentProposal() != null;

        protected override SpatialZone ZoneForScreen() => SpatialZone.Right;

        protected override FlPanel BuildPanel() =>
            CreatePanelInZone("REMEDIATION PROPOSAL", SpatialZone.Right, 680f, 600f);

        protected override void BuildContent(FlPanel panel)
        {
            _ = panel.AddButton("proposal-submit", "Submit for approval",
                "Open the approval confirmation gate for this proposal",
                OnSubmit);

            _ = panel.AddButton("proposal-evidence", "Inspect evidence",
                "Open the evidence the findings cite",
                () => App.Navigator.NavigateToScreen(ScreenIds.EvidenceExplorer));

            _ = panel.AddButton("proposal-cancel", "Cancel proposal",
                "Return to the investigation workspace; the proposal record is left unchanged",
                OnCancel);

            App.Changed += (_, _) => Refresh();
        }

        protected override void OnEntered() => Refresh();

        private void OnSubmit()
        {
            if (!CanSubmit)
            {
                Refresh();
                return;
            }

            SubmitForApprovalRequested?.Invoke(this, EventArgs.Empty);
            App.Navigator.NavigateToScreen(ScreenIds.ApprovalConfirmation);
        }

        private void OnCancel()
        {
            CancelProposalRequested?.Invoke(this, EventArgs.Empty);
            App.Navigator.NavigateToScreen(ScreenIds.InvestigationWorkspace);
        }

        private void Refresh()
        {
            if (!isActiveAndEnabled || Panel == null) return;

            var proposal = CurrentProposal();
            if (proposal == null)
            {
                SetEmpty("No remediation proposal is recorded for this incident. Review the findings " +
                         "and propose a remediation from the investigation workspace.");
                Panel.SetStatusLine(App.StatusLine);
                SetButtons(false);
                return;
            }

            if (!string.Equals(_openedProposalId, proposal.ProposalId, StringComparison.Ordinal))
            {
                _openedProposalId = proposal.ProposalId;
                ProposalOpened?.Invoke(this, new ProposalIdEventArgs(proposal.ProposalId));
            }

            FocusTargetId = proposal.ProposalId;

            var action = proposal.Action;
            var lines = new List<string>
            {
                $"Proposal {FaultlineMessages.ShortId(proposal.ProposalId)} · incident " +
                $"{FaultlineMessages.ShortId(proposal.IncidentId)}",
                $"Action: {action?.Type.ToString() ?? "not recorded"}",
                $"Description: {action?.Description ?? "not recorded"}",
                $"Affected services: {Affected(action)}",
                $"Reversibility: {action?.Reversibility.ToString() ?? "not recorded"}",
                $"Expected benefit: {action?.ExpectedBenefit ?? "not recorded"}",
                $"Side effects: {SideEffects(action)}",
                $"Status: {proposal.Status} · evidence ids cited: " +
                $"{action?.EvidenceIds.Count ?? 0}",
                ExpiryLine(proposal)
            };

            lines.Add(string.Empty);
            lines.Add(CanSubmit
                ? "Submitting opens the approval gate. Nothing is executed until an approval is " +
                  "explicitly confirmed there."
                : BlockReason(proposal));

            Panel.SetBody(string.Join(Environment.NewLine, lines));
            Panel.SetStatusLine(SubmitStatus(proposal));
            SetContent();
            SetButtons(CanSubmit);
        }

        private void SetButtons(bool canSubmit)
        {
            if (Panel.TryGetButton("proposal-submit", out var submit))
                submit.Interactable = canSubmit;
            if (Panel.TryGetButton("proposal-evidence", out var evidence))
                evidence.Interactable = App.Navigator.TryGetScreen(ScreenIds.EvidenceExplorer,
                    out var screen) && screen.CanEnter;
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

        private static string Affected(Faultline.Domain.RemediationAction action) =>
            action == null || action.AffectedServiceIds.Count == 0
                ? "none recorded"
                : string.Join(", ", action.AffectedServiceIds);

        private static string SideEffects(Faultline.Domain.RemediationAction action) =>
            action == null || action.SideEffects.Count == 0
                ? "none recorded"
                : string.Join(" · ", action.SideEffects);

        private static string ExpiryLine(RemediationProposal proposal)
        {
            if (!proposal.ExpiresAt.HasValue)
                return "Approval window: no expiry recorded.";

            var issued = proposal.ExpiresAt.Value.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            if (proposal.IsExpired)
                return $"Approval window: expired at {issued} UTC.";

            var remaining = proposal.ExpiresAt.Value - DateTimeOffset.UtcNow;
            return $"Approval window: expires at {issued} UTC " +
                   $"({(int)Math.Max(0, Math.Floor(remaining.TotalMinutes))} min left).";
        }

        private static string BlockReason(RemediationProposal proposal)
        {
            if (proposal.IsExpired)
                return "Submit blocked: the approval window recorded by the remediation service has " +
                       "closed, so approval would be rejected (FL-RM-0082). Replay for a fresh proposal.";

            return $"Submit blocked: the recorded proposal is in status {proposal.Status}, " +
                   "not awaiting approval.";
        }

        private string SubmitStatus(RemediationProposal proposal) =>
            CanSubmit ? App.StatusLine : BlockReason(proposal);
    }
}
