namespace Faultline.Interaction
{
    using System;

    /// <summary>
    /// Domain commands emitted by gestures. These are pure data — the
    /// Interaction module never mutates domain state directly; it submits
    /// commands to the owning domain service (Incidents, Agents, Remediation,
    /// etc.) which validates and applies them.
    ///
    /// No UnityEngine types are referenced here.
    /// </summary>
    public enum InteractionCommandType
    {
        /// <summary>Select a node or UI target (brief pinch / tap).</summary>
        SelectNode,

        /// <summary>Release a selection without further action.</summary>
        ReleaseNode,

        /// <summary>Grab a node or panel (sustained pinch / grab-hold).</summary>
        GrabNode,

        /// <summary>Dispatch an agent task to the selected target.</summary>
        DispatchAgent,

        /// <summary>Approve a remediation proposal.</summary>
        ApproveRemediation,

        /// <summary>Reject a remediation proposal.</summary>
        RejectRemediation,

        /// <summary>Inspect evidence associated with a node/incident.</summary>
        InspectEvidence,

        /// <summary>Close a panel / dismiss a modal.</summary>
        ClosePanel,
    }

    /// <summary>
    /// A single interaction command produced by a gesture interpreter.
    /// Carries the command type, target (node id or panel id), and an
    /// optional payload for commands that need extra data (e.g. two-hand
    /// scale factor for resizing).
    /// </summary>
    public sealed class InteractionCommand
    {
        public InteractionCommandType Type { get; }
        public string TargetId { get; } // node id, panel id, or anchor id
        public string SourceHand { get; } // "Left" / "Right" / "Both"
        public System.Numerics.Vector3? Position { get; } // world position at gesture time
        public float? ScaleFactor { get; } // for two-hand scale / resize
        public string Payload { get; } // free-form JSON or identifier payload
        public long TimestampTicks { get; }

        private InteractionCommand(
            InteractionCommandType type,
            string targetId,
            string sourceHand,
            System.Numerics.Vector3? position,
            float? scaleFactor,
            string payload,
            long timestampTicks)
        {
            Type = type;
            TargetId = targetId;
            SourceHand = sourceHand;
            Position = position;
            ScaleFactor = scaleFactor;
            Payload = payload;
            TimestampTicks = timestampTicks;
        }

        /// <summary>Create a node-selection command.</summary>
        public static InteractionCommand SelectNode(
            string nodeId, string sourceHand, System.Numerics.Vector3? position = null) =>
            new(InteractionCommandType.SelectNode, nodeId, sourceHand, position, null, null,
                DateTimeOffset.UtcNow.Ticks);

        /// <summary>Create a node-release command.</summary>
        public static InteractionCommand ReleaseNode(
            string nodeId, string sourceHand = null) =>
            new(InteractionCommandType.ReleaseNode, nodeId, sourceHand, null, null, null,
                DateTimeOffset.UtcNow.Ticks);

        /// <summary>Create a node-grab command.</summary>
        public static InteractionCommand GrabNode(
            string nodeId, string sourceHand, System.Numerics.Vector3? position = null) =>
            new(InteractionCommandType.GrabNode, nodeId, sourceHand, position, null, null,
                DateTimeOffset.UtcNow.Ticks);

        /// <summary>Create an agent-dispatch command.</summary>
        public static InteractionCommand DispatchAgent(
            string targetId, string agentRole, string sourceHand = null) =>
            new(InteractionCommandType.DispatchAgent, targetId, sourceHand, null, null, agentRole,
                DateTimeOffset.UtcNow.Ticks);

        /// <summary>Create a remediation-approve command.</summary>
        public static InteractionCommand ApproveRemediation(
            string planId, string sourceHand = null) =>
            new(InteractionCommandType.ApproveRemediation, planId, sourceHand, null, null, null,
                DateTimeOffset.UtcNow.Ticks);

        /// <summary>Create a remediation-reject command.</summary>
        public static InteractionCommand RejectRemediation(
            string planId, string sourceHand = null) =>
            new(InteractionCommandType.RejectRemediation, planId, sourceHand, null, null, null,
                DateTimeOffset.UtcNow.Ticks);

        /// <summary>Create an evidence-inspect command.</summary>
        public static InteractionCommand InspectEvidence(
            string evidenceId, string sourceHand = null) =>
            new(InteractionCommandType.InspectEvidence, evidenceId, sourceHand, null, null, null,
                DateTimeOffset.UtcNow.Ticks);

        /// <summary>Create a close-panel command.</summary>
        public static InteractionCommand ClosePanel(
            string panelId, string sourceHand = null) =>
            new(InteractionCommandType.ClosePanel, panelId, sourceHand, null, null, null,
                DateTimeOffset.UtcNow.Ticks);

        /// <summary>Create a two-hand scale command (panel/graph resize).</summary>
        public static InteractionCommand Scale(
            string targetId, float scaleFactor, System.Numerics.Vector3? position = null) =>
            new(InteractionCommandType.SelectNode, targetId, "Both", position, scaleFactor, null,
                DateTimeOffset.UtcNow.Ticks);

        public override string ToString() =>
            $"InteractionCommand({Type}, target={TargetId}, hand={SourceHand}, " +
            $"scale={ScaleFactor}, payload={Payload})";
    }
}
