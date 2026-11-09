namespace Faultline.Runtime.UI
{
    using System;
    using System.Collections.Generic;

    using UnityEngine;

    using Faultline.Runtime.UI.Screens;

    /// <summary>
    /// Creates the PRD §6.1 screen views and binds them to the application
    /// root. Every screen in <see cref="ScreenIds.All"/> must be registered, so
    /// a missing view is a build-time failure rather than a runtime gap.
    /// </summary>
    public static class ScreenRegistry
    {
        private static readonly (string Id, Type ViewType)[] Definitions =
        {
            (ScreenIds.Boot, typeof(BootScreenView)),
            (ScreenIds.Welcome, typeof(WelcomeScreenView)),
            (ScreenIds.Onboarding, typeof(OnboardingScreenView)),
            (ScreenIds.HandTutorial, typeof(HandTutorialScreenView)),
            (ScreenIds.ScenarioSelection, typeof(ScenarioSelectionScreenView)),
            (ScreenIds.SpatialEnvironment, typeof(SpatialEnvironmentScreenView)),
            (ScreenIds.InfrastructureOverview, typeof(InfrastructureOverviewScreenView)),
            (ScreenIds.ServiceInspection, typeof(ServiceInspectionScreenView)),
            (ScreenIds.IncidentDetail, typeof(IncidentDetailScreenView)),
            (ScreenIds.AgentControlPanel, typeof(AgentControlPanelScreenView)),
            (ScreenIds.InvestigationWorkspace, typeof(InvestigationWorkspaceScreenView)),
            (ScreenIds.EvidenceExplorer, typeof(EvidenceExplorerScreenView)),
            (ScreenIds.RemediationProposal, typeof(RemediationProposalScreenView)),
            (ScreenIds.ApprovalConfirmation, typeof(ApprovalConfirmationScreenView)),
            (ScreenIds.RecoverySequence, typeof(RecoverySequenceScreenView)),
            (ScreenIds.Verification, typeof(VerificationScreenView)),
            (ScreenIds.Results, typeof(ResultsScreenView)),
            (ScreenIds.IncidentTimeline, typeof(IncidentTimelineScreenView)),
            (ScreenIds.Settings, typeof(SettingsScreenView)),
            (ScreenIds.AccessibilitySettings, typeof(AccessibilitySettingsScreenView)),
            (ScreenIds.Diagnostics, typeof(DiagnosticsScreenView)),
            (ScreenIds.PauseMenu, typeof(PauseMenuScreenView)),
            (ScreenIds.ErrorRecovery, typeof(ErrorRecoveryScreenView)),
        };

        public static int Count => Definitions.Length;

        public static void Install(FaultlineUnityApp app, Transform uiRoot,
            Dictionary<string, ScreenViewBase> destination)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (uiRoot == null) throw new ArgumentNullException(nameof(uiRoot));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            destination.Clear();

            foreach (var (id, viewType) in Definitions)
            {
                var gameObject = new GameObject("Screen:" + id);
                gameObject.transform.SetParent(uiRoot, false);
                gameObject.SetActive(false);

                var view = gameObject.AddComponent(viewType) as ScreenViewBase;
                if (view == null)
                    throw new InvalidOperationException($"Screen view for '{id}' is not a ScreenViewBase.");

                view.Bind(app);

                if (view.ScreenId != id)
                    throw new InvalidOperationException(
                        $"Screen '{id}' reports ScreenId '{view.ScreenId}'. Registry and view disagree.");

                destination[id] = view;
            }
        }
    }
}