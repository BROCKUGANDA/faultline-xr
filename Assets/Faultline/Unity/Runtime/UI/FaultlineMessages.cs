namespace Faultline.Runtime.UI
{
    /// <summary>
    /// Single source of user-facing copy for the Unity build. Strings live here
    /// rather than inline in views so wording, accessibility phrasing and any
    /// later localisation stay in one place.
    /// </summary>
    public sealed class FaultlineMessages
    {
        public string Loading => "Loading…";
        public string EmptyGuidance => "Nothing to show yet.";
        public string ErrorPrefix => "FAULTLINE";
        public string RetryAvailable => "Retry is available.";
        public string NotRetryable => "This step cannot be retried.";

        public string WelcomeBody =>
            "FAULTLINE XR puts a cascading production incident in front of you as a " +
            "spatial dependency graph. Trace the cause, direct four analyst agents, " +
            "approve one safe remediation and watch the simulated system recover — " +
            "using only your hands.";

        public string TutorialBody =>
            "Pinch your thumb and index finger together to select. Hold the pinch to " +
            "grab a node or a panel. Release to drop. Pinch with both hands and move " +
            "them apart to scale the graph.";

        public string ScenarioBody =>
            "CASCADE: database latency at 08:41 drags the application tier, the queue " +
            "and the worker pool down with it. Seven services, six dependencies, one " +
            "deterministic seed. Nothing here touches real infrastructure.";

        public string DetectedBody(string incidentId, string rootCause, string severity) =>
            $"Incident {ShortId(incidentId)} detected on {rootCause} ({severity}). " +
            "The failing node is marked in the graph.";

        public string InvestigatingBody(string nodeId) =>
            $"Investigating {nodeId}. Dispatch analyst agents from the agent panel; " +
            "each finding cites the evidence it came from.";

        public string FindingsBody(int findingCount, double averageConfidence) =>
            $"{findingCount} grounded findings. Mean confidence {averageConfidence:P0}. " +
            "Uncertainty is shown where the evidence does not settle the question.";

        public string ProposalBody(string actionType, string affected) =>
            $"Proposed remediation: {actionType} on {affected}. Preconditions are " +
            "checked at execution time and the change is reversible from a checkpoint.";

        public string ApprovedBody(string proposalId) =>
            $"Proposal {ShortId(proposalId)} approved. Execution is idempotent: a " +
            "second activation cannot apply the same remediation twice.";

        public string RejectedBody(string reason) =>
            $"Proposal rejected. {reason}";

        public string ExecutedBody(double aggregateHealthBefore, double aggregateHealthAfter) =>
            $"Remediation executed. Aggregate health moved from " +
            $"{aggregateHealthBefore:P0} to {aggregateHealthAfter:P0} in the simulation.";

        public string VerifiedBody(string rootStatus) =>
            $"Verification passed against simulated metrics: root service {rootStatus}. " +
            "Recovery is real domain state, not an animation.";

        public string ResolvedBody(int eventCount, double recoveryScore) =>
            $"Incident resolved. {eventCount} ordered events recorded; recovery score " +
            $"{recoveryScore:P0}. Open the after-action report to review evidence.";

        public static string ShortId(string id)
        {
            if (string.IsNullOrEmpty(id)) return "—";
            return id.Length <= 8 ? id : id.Substring(id.Length - 8);
        }
    }
}