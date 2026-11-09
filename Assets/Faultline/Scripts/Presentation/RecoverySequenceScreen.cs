namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Recovery sequence screen contract (PRD §6.1, screen 15).
    /// Entry conditions: remediation approved; recovery steps defined.
    /// Exit conditions: recovery sequence complete or failed.
    /// Interaction model: step-by-step recovery with hand-triggered Next /
    /// Abort per step; tracking-loss recovery shown via status indicator.
    /// Back navigation: ApprovalConfirmationScreen.
    /// Accessibility: each recovery step announced; simple tracking-loss
    /// recovery (no hidden gesture).
    /// </summary>
    public interface RecoverySequenceScreen : IScreen
    {
        /// <summary>Plan id being recovered.</summary>
        string PlanId { get; }

        /// <summary>Current recovery step index (0-based).</summary>
        int CurrentStep { get; }

        /// <summary>Total recovery steps.</summary>
        int TotalSteps { get; }

        /// <summary>Whether the current step can be advanced.</summary>
        bool CanAdvance { get; }

        event EventHandler<PlanIdEventArgs> RecoveryStarted;
        event EventHandler<RecoveryStepEventArgs> StepAdvanced;
        event EventHandler RecoveryAborted;
        event EventHandler RecoveryCompleted;
    }

    public sealed class RecoveryStepEventArgs : EventArgs
    {
        public int StepIndex { get; }
        public RecoveryStepEventArgs(int stepIndex)
        {
            StepIndex = stepIndex;
        }
    }
}
