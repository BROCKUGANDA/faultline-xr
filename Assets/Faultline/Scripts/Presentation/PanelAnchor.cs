namespace Faultline.Presentation
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Consistent panel anchoring contract. Panels are anchored to spatial
    /// zones (see <see cref="SpatialZone"/>) with a stable offset; the anchor
    /// prevents overlapping panels and supports contextual panels that follow
    /// a target (e.g. a selected node) within a comfort threshold.
    ///
    /// Pure C# — no UnityEngine. The Presentation layer applies these anchor
    /// computations to world-space transforms at runtime.
    /// </summary>
    public sealed class PanelAnchor
    {
        /// <summary>Unique anchor id.</summary>
        public string AnchorId { get; }

        /// <summary>Zone the panel is anchored to.</summary>
        public SpatialZone Zone { get; }

        /// <summary>Offset from the zone's default position (metres).</summary>
        public System.Numerics.Vector3 Offset { get; private set; }

        /// <summary>Whether the panel is pinned (not dismissed by gesture).</summary>
        public bool IsPinned { get; set; }

        /// <summary>Optional target this panel follows (contextual panel).</summary>
        public string FollowTargetId { get; private set; }

        /// <summary>Whether the anchor is currently active/visible.</summary>
        public bool IsActive { get; private set; }

        public event EventHandler<PanelAnchorMovedEventArgs> Moved;
        public event EventHandler<PanelAnchorEventArgs> Activated;
        public event EventHandler<PanelAnchorEventArgs> Deactivated;

        public PanelAnchor(
            string anchorId,
            SpatialZone zone,
            System.Numerics.Vector3 offset,
            bool isPinned = false)
        {
            if (string.IsNullOrWhiteSpace(anchorId))
                throw new ArgumentException("Anchor id is required.", nameof(anchorId));

            AnchorId = anchorId;
            Zone = zone;
            Offset = offset;
            IsPinned = isPinned;
            IsActive = false;
        }

        /// <summary>
        /// Compute the world-space position of this anchor, given the zone's
        /// default position and an optional follow-target position.
        /// Returns the position clamped to the seated reach envelope.
        /// </summary>
        public System.Numerics.Vector3 ComputeWorldPosition(
            System.Numerics.Vector3 zoneDefaultPosition,
            System.Numerics.Vector3? followTargetPosition = null)
        {
            var position = followTargetPosition.HasValue
                ? followTargetPosition.Value + Offset
                : zoneDefaultPosition + Offset;

            var (clamped, _) = Faultline.Spatial.CoordinateSystem.ClampToReachEnvelope(position);
            return clamped;
        }

        /// <summary>Reposition the anchor's offset.</summary>
        public void SetOffset(System.Numerics.Vector3 offset)
        {
            Offset = offset;
        }

        /// <summary>Set a follow target for a contextual panel.</summary>
        public void SetFollowTarget(string targetId)
        {
            FollowTargetId = targetId;
        }

        /// <summary>Clear the follow target (panel returns to zone position).</summary>
        public void ClearFollowTarget()
        {
            FollowTargetId = null;
        }

        /// <summary>Activate the anchor (panel becomes visible).</summary>
        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;
            Activated?.Invoke(this, new PanelAnchorEventArgs(AnchorId, Zone));
        }

        /// <summary>Deactivate the anchor (panel hidden).</summary>
        public void Deactivate()
        {
            if (!IsActive) return;
            IsActive = false;
            Deactivated?.Invoke(this, new PanelAnchorEventArgs(AnchorId, Zone));
        }

        public override string ToString() =>
            $"PanelAnchor({AnchorId}, zone={Zone}, active={IsActive}, pinned={IsPinned})";
    }

    /// <summary>
    /// Manages a collection of panel anchors, ensuring panels do not overlap
    /// by resolving zone conflicts and enforcing a minimum separation.
    /// </summary>
    public sealed class PanelAnchorManager
    {
        private readonly Dictionary<string, PanelAnchor> _anchors = new(StringComparer.Ordinal);
        private readonly float _minPanelSeparationM = 0.50f;

        /// <summary>All registered anchors.</summary>
        public IReadOnlyDictionary<string, PanelAnchor> Anchors => _anchors;

        /// <summary>Raised when an anchor is added or removed.</summary>
        public event EventHandler<PanelAnchorEventArgs> AnchorAdded;
        public event EventHandler<PanelAnchorEventArgs> AnchorRemoved;

        public void Register(PanelAnchor anchor)
        {
            if (anchor == null) throw new ArgumentNullException(nameof(anchor));
            _anchors[anchor.AnchorId] = anchor;
            AnchorAdded?.Invoke(this, new PanelAnchorEventArgs(anchor.AnchorId, anchor.Zone));
        }

        public bool Unregister(string anchorId)
        {
            if (_anchors.Remove(anchorId))
            {
                AnchorRemoved?.Invoke(this, new PanelAnchorEventArgs(anchorId, SpatialZone.Centre));
                return true;
            }
            return false;
        }

        public PanelAnchor Get(string anchorId) =>
            _anchors.TryGetValue(anchorId, out var anchor) ? anchor : null;

        /// <summary>
        /// Check whether activating this anchor would overlap another active
        /// panel. Returns the conflicting anchor id, or null if clear.
        /// </summary>
        public string CheckOverlap(PanelAnchor anchor)
        {
            foreach (var other in _anchors.Values)
            {
                if (other.AnchorId == anchor.AnchorId || !other.IsActive) continue;
                // Same zone panels must be separated; different zones are
                // considered separated by default (zone spacing >= separation).
                if (other.Zone == anchor.Zone)
                {
                    return other.AnchorId;
                }
            }
            return null;
        }

        /// <summary>Minimum separation between panels in the same zone (metres).</summary>
        public float MinPanelSeparationM => _minPanelSeparationM;
    }

    /// <summary>Arguments for panel anchor events.</summary>
    public sealed class PanelAnchorEventArgs : EventArgs
    {
        public string AnchorId { get; }
        public SpatialZone Zone { get; }

        public PanelAnchorEventArgs(string anchorId, SpatialZone zone)
        {
            AnchorId = anchorId;
            Zone = zone;
        }
    }

    /// <summary>Arguments for panel anchor move events.</summary>
    public sealed class PanelAnchorMovedEventArgs : EventArgs
    {
        public string AnchorId { get; }
        public System.Numerics.Vector3 NewPosition { get; }

        public PanelAnchorMovedEventArgs(string anchorId, System.Numerics.Vector3 newPosition)
        {
            AnchorId = anchorId;
            NewPosition = newPosition;
        }
    }
}
