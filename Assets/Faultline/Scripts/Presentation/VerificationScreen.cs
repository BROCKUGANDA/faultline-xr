namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Verification screen contract (PRD §6.1, screen 16).
    /// Entry conditions: recovery sequence completed.
    /// Exit conditions: verified (pass) or verification failed.
    /// Interaction model: verification checks listed; hand trigger to
    /// ConfirmVerification / Re-runCheck.
    /// Back navigation: RecoverySequenceScreen.
    /// Accessibility: pass/fail via colour + glyph + text; check descriptions
    /// announced.
    /// </summary>
    public interface VerificationScreen : IScreen
    {
        /// <summary>Plan id being verified.</summary>
        string PlanId { get; }

        /// <summary>Whether all checks passed.</summary>
        bool AllChecksPassed { get; }

        event EventHandler<PlanIdEventArgs> VerificationOpened;
        event EventHandler ConfirmVerificationRequested;
        event EventHandler ReRunCheckRequested;
    }
}
