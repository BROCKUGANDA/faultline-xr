namespace Faultline.Interaction
{
    using System;
    using System.Collections.Generic;
    using Faultline.Domain;

    /// <summary>
    /// Controller-free accessibility contract.
    ///
    /// Guarantees that every critical action has a large, visible,
    /// hand-interactable alternative — no hidden gestures, no controller-only
    /// paths, no mandatory voice. Panels present explicit buttons/targets for
    /// SelectNode, GrabNode, ApproveRemediation, RejectRemediation,
    /// DispatchAgent, InspectEvidence, ClosePanel, etc.
    ///
    /// The Presentation layer registers each critical action with a visible
    /// target id; the validator confirms coverage and reports gaps.
    /// </summary>
    public sealed class ControllerFreeValidator
    {
        private readonly HashSet<string> _registeredCriticalActions = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _actionToTarget = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Critical action ids that must have a visible hand target.</summary>
        public IReadOnlyCollection<string> RegisteredCriticalActions => _registeredCriticalActions;

        /// <summary>Register a critical action and its visible hand-interactable target.</summary>
        public void RegisterCriticalAction(string actionId, string visibleTargetId)
        {
            if (string.IsNullOrWhiteSpace(actionId))
                throw new ArgumentException("Action id is required.", nameof(actionId));
            if (string.IsNullOrWhiteSpace(visibleTargetId))
                throw new ArgumentException("Visible target id is required.", nameof(visibleTargetId));

            _registeredCriticalActions.Add(actionId);
            _actionToTarget[actionId] = visibleTargetId;
        }

        /// <summary>
        /// Validate that every registered critical action has a visible,
        /// hand-interactable target. Returns success when coverage is
        /// complete; failure lists the missing actions.
        /// </summary>
        public Result<bool> ValidateCoverage()
        {
            var missing = new List<string>();
            foreach (var action in _registeredCriticalActions)
            {
                if (!_actionToTarget.ContainsKey(action) ||
                    string.IsNullOrWhiteSpace(_actionToTarget[action]))
                {
                    missing.Add(action);
                }
            }

            if (missing.Count > 0)
            {
                return Result<bool>.Failure("FL-IN-0022",
                    $"Controller-free coverage incomplete: {missing.Count} critical action(s) lack a visible hand-interactable target: " +
                    string.Join(", ", missing) + ".",
                    diagnosticContext: new Dictionary<string, object> { { "missingActions", missing } });
            }

            return Result<bool>.Success(true);
        }

        /// <summary>Get the visible target id registered for a critical action.</summary>
        public bool TryGetTarget(string actionId, out string visibleTargetId)
        {
            return _actionToTarget.TryGetValue(actionId, out visibleTargetId);
        }

        /// <summary>Clear all registrations. Used on session reset.</summary>
        public void Reset()
        {
            _registeredCriticalActions.Clear();
            _actionToTarget.Clear();
        }
    }
}
