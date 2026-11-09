namespace Faultline.Spatial
{
    using System;
    using Faultline.Domain;
    using Faultline.Domain.Errors;

    /// <summary>
    /// Status colour mapping for spatial nodes, derived from design tokens.
    /// Pure C# — Presentation maps the ColorRgba to UnityEngine.Color at the
    /// boundary.
    /// </summary>
    public enum NodeStatusMaterial
    {
        Healthy,
        Degraded,
        Failed,
        Critical,
        Recovering,
        Unknown
    }

    /// <summary>
    /// Spatial representation of a domain <see cref="Faultline.Domain.ServiceNode"/>.
    /// Carries world position, status material, selection highlight, and
    /// contextual label. Pure data — no UnityEngine types. The Presentation
    /// layer renders this as a world-space node (glyph + label + indicator).
    /// </summary>
    public sealed class SpatialNode
    {
        public string NodeId { get; }
        public string DisplayName { get; }
        public string ServiceType { get; }
        public System.Numerics.Vector3 Position { get; private set; }
        public NodeStatusMaterial StatusMaterial { get; private set; }
        public bool IsSelected { get; private set; }
        public string Label { get; private set; }

        /// <summary>Glyph token name from the design-system glyph registry.</summary>
        public string GlyphName { get; private set; }

        public event EventHandler<SpatialNodeChangedEventArgs> Changed;

        public SpatialNode(
            string nodeId,
            string displayName,
            string serviceType,
            System.Numerics.Vector3 position,
            NodeStatusMaterial statusMaterial,
            string glyphName = null,
            string label = null)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                throw new ArgumentException("Node id is required.", nameof(nodeId));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Display name is required.", nameof(displayName));

            NodeId = nodeId;
            DisplayName = displayName;
            ServiceType = serviceType ?? string.Empty;
            Position = position;
            StatusMaterial = statusMaterial;
            GlyphName = glyphName ?? "FL_NODE";
            Label = label ?? displayName;
            IsSelected = false;
        }

        /// <summary>Move the node to a new world position (clamped to comfortable reach).</summary>
        public void MoveTo(System.Numerics.Vector3 newPosition)
        {
            var (clamped, wasClamped) = CoordinateSystem.ClampToReachEnvelope(newPosition);
            if (Position.Equals(clamped)) return;

            var old = Position;
            Position = clamped;
            Changed?.Invoke(this, new SpatialNodeChangedEventArgs(NodeId, old, clamped, wasClamped));
        }

        /// <summary>Update the node's status material.</summary>
        public void SetStatus(NodeStatusMaterial statusMaterial)
        {
            if (StatusMaterial == statusMaterial) return;
            var old = StatusMaterial;
            StatusMaterial = statusMaterial;
            Changed?.Invoke(this, new SpatialNodeChangedEventArgs(NodeId, Position, Position, false)
                // status change is surfaced via the event args below
            );
        }

        /// <summary>Set selection state.</summary>
        public void SetSelected(bool selected)
        {
            if (IsSelected == selected) return;
            IsSelected = selected;
            Changed?.Invoke(this, new SpatialNodeChangedEventArgs(NodeId, Position, Position, false));
        }

        /// <summary>Update the contextual label.</summary>
        public void SetLabel(string label)
        {
            Label = label ?? DisplayName;
            Changed?.Invoke(this, new SpatialNodeChangedEventArgs(NodeId, Position, Position, false));
        }

        /// <summary>Set the glyph token name.</summary>
        public void SetGlyph(string glyphName)
        {
            GlyphName = glyphName ?? "FL_NODE";
        }

        public override string ToString() =>
            $"SpatialNode({NodeId}, {DisplayName}, pos={Position}, status={StatusMaterial}, selected={IsSelected})";
    }

    /// <summary>Arguments for spatial node changes.</summary>
    public sealed class SpatialNodeChangedEventArgs : EventArgs
    {
        public string NodeId { get; }
        public System.Numerics.Vector3 OldPosition { get; }
        public System.Numerics.Vector3 NewPosition { get; }
        public bool WasClampedToReach { get; }

        public SpatialNodeChangedEventArgs(
            string nodeId,
            System.Numerics.Vector3 oldPosition,
            System.Numerics.Vector3 newPosition,
            bool wasClampedToReach)
        {
            NodeId = nodeId;
            OldPosition = oldPosition;
            NewPosition = newPosition;
            WasClampedToReach = wasClampedToReach;
        }
    }
}
