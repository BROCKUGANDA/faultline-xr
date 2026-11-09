namespace Faultline.Runtime.UI
{
    /// <summary>
    /// Canonical screen identifiers for the PRD §6.1 screens, in journey order.
    /// The navigator, the controller-free validator and the tests all address
    /// screens through these constants rather than free-form strings.
    /// </summary>
    public static class ScreenIds
    {
        public const string Boot = "Boot";
        public const string Welcome = "Welcome";
        public const string Onboarding = "Onboarding";
        public const string HandTutorial = "HandTutorial";
        public const string ScenarioSelection = "ScenarioSelection";
        public const string SpatialEnvironment = "SpatialEnvironment";
        public const string InfrastructureOverview = "InfrastructureOverview";
        public const string ServiceInspection = "ServiceInspection";
        public const string IncidentDetail = "IncidentDetail";
        public const string AgentControlPanel = "AgentControlPanel";
        public const string InvestigationWorkspace = "InvestigationWorkspace";
        public const string EvidenceExplorer = "EvidenceExplorer";
        public const string RemediationProposal = "RemediationProposal";
        public const string ApprovalConfirmation = "ApprovalConfirmation";
        public const string RecoverySequence = "RecoverySequence";
        public const string Verification = "Verification";
        public const string Results = "Results";
        public const string IncidentTimeline = "IncidentTimeline";
        public const string Settings = "Settings";
        public const string AccessibilitySettings = "AccessibilitySettings";
        public const string Diagnostics = "Diagnostics";
        public const string PauseMenu = "PauseMenu";
        public const string ErrorRecovery = "ErrorRecovery";

        /// <summary>All 23 screen ids in journey order.</summary>
        public static readonly string[] All =
        {
            Boot, Welcome, Onboarding, HandTutorial, ScenarioSelection, SpatialEnvironment,
            InfrastructureOverview, ServiceInspection, IncidentDetail, AgentControlPanel,
            InvestigationWorkspace, EvidenceExplorer, RemediationProposal, ApprovalConfirmation,
            RecoverySequence, Verification, Results, IncidentTimeline, Settings,
            AccessibilitySettings, Diagnostics, PauseMenu, ErrorRecovery
        };
    }
}