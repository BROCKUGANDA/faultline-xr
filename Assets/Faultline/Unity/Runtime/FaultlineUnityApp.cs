namespace Faultline.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using UnityEngine;

    using Faultline.App;
    using Faultline.Audio = Faultline.Runtime.Audio;
    using Faultline.Domain;
    using Faultline.Infrastructure;
    using Faultline.Interaction;
    using Faultline.Persistence;
    using Faultline.Presentation;
    using Faultline.Runtime.UI;
    using Faultline.Runtime.View;
    using Faultline.Runtime.Xr;
    using Faultline.Spatial;
    using Faultline.XR;

    /// <summary>Which XR provider stack is live. Reported by diagnostics and never faked.</summary>
    public enum XrProviderKind
    {
        None,
        Device,
        Simulated
    }

    /// <summary>
    /// Application composition root. Builds the service graph, selects the XR
    /// provider (device first, labelled simulation fallback when no
    /// hand-tracking runtime is present), installs the spatial topology and the
    /// PRD §6.1 screens, and translates interpreted gestures into validated
    /// domain commands.
    ///
    /// Views never mutate domain state: gestures become commands, commands go
    /// through <see cref="InvestigationController"/>, and views re-read the
    /// resulting domain state on the Changed event.
    /// </summary>
    public sealed class FaultlineUnityApp : MonoBehaviour
    {
        /// <summary>Simulation advances at this rate; bounded so a pause cannot spike.</summary>
        private const float SimulationTickSeconds = 0.5f;

        [SerializeField] private bool preferDeviceXr = true;

        private readonly Dictionary<string, ScreenViewBase> _screenViews = new(StringComparer.Ordinal);
        private readonly CancellationTokenSource _shutdownToken = new();

        private UnityScreenNavigator _navigator;
        private InvestigationController _controller;
        private SpatialTopology _topology;
        private GestureInterpreter _interpreter;
        private IXrInputProvider _input;
        private MetaXrInputProvider _deviceInput;
        private MetaXrSession _deviceSession;
        private IXrSession _session;
        private HandRayInteractor _interactor;
        private AudioCueService _audio;
        private PersistenceService _persistence;
        private ControllerFreeValidator _validator;
        private TrackingLossHandler _trackingLoss;
        private AccessibilitySettings _accessibility;
        private FaultlineMessages _messages;
        private SpatialLayoutService _layout;
        private TopologyView _topologyView;
        private Transform _uiRoot;
        private Transform _cameraAnchor;
        private float _tickTimer;
        private bool _bootCompleted;
        private string _bootError;
        private double _healthBeforeRemediation;

        public event EventHandler Changed;

        public InvestigationController Controller => _controller;
        public TopologyView Topology => _topologyView;
        public FaultlineMessages Messages => _messages;
        public SpatialLayoutService Layout => _layout;
        public AccessibilitySettings Accessibility => _accessibility;
        public IXrSession Session => _session;
        public IXrInputProvider Input => _input;
        public XrProviderKind ProviderKind { get; private set; }
        public ControllerFreeValidator Validator => _validator;
        public string BootError => _bootError;
        public bool IsBooted => _bootCompleted;
        public int RegisteredScreenCount => _screenViews.Count;

        public UnityScreenNavigator Navigator
        {
            get
            {
                if (_navigator == null)
                    Debug.LogError("FaultlineUnityApp: navigator accessed before boot completed.");
                return _navigator;
            }
        }

        private void Awake()
        {
            Application.targetFrameRate = 72;
            _messages = new FaultlineMessages();
            _layout = new SpatialLayoutService();
            _accessibility = new AccessibilitySettings();
            _persistence = new PersistenceService(System.IO.Path.Combine(Application.persistentDataPath, "faultline-session.json"));
            _controller = new InvestigationController(saveFilePath: System.IO.Path.Combine(
                Application.persistentDataPath, "faultline-run.json"));
            _topology = new SpatialTopology();
            _validator = new ControllerFreeValidator();
            _trackingLoss = new TrackingLossHandler();
            _interpreter = new GestureInterpreter();
        }

        private IEnumerator Start()
        {
            _cameraAnchor = Camera.main != null ? Camera.main.transform : transform;
            _uiRoot = new GameObject("FaultlineUiRoot").transform;
            _uiRoot.SetParent(_cameraAnchor.parent != null ? _cameraAnchor.parent : transform, false);

            InitialiseXr();

            _topologyView = _uiRoot.gameObject.AddComponent<TopologyView>();
            _topologyView.Initialise(_controller, _cameraAnchor);
            _topologyView.NodeSelectionRequested += OnNodeSelectionRequested;
            _topologyView.HoverNodeChanged += nodeId => _interpreter.SetHoveredNode(nodeId);

            _interactor = _uiRoot.gameObject.AddComponent<HandRayInteractor>();
            if (_deviceInput != null) _interactor.Bind(_deviceInput, _topologyView);

            ScreenRegistry.Install(this, _uiRoot, _screenViews);
            _navigator = new UnityScreenNavigator();
            foreach (var pair in _screenViews)
                _navigator.RegisterScreen(pair.Key, pair.Value);

            _controller.Changed += (_, _) => RaiseChanged();
            _navigator.StateChanged += (_, e) => OnApplicationStateChanged(e.NewState);
            _accessibility.SeatedOperationChanged += (_, e) => { if (_accessibility.SeatedOperation) ResetLayout(); };
            _accessibility.ReducedMotionChanged += (_, _) => ApplyAccessibility();
            _accessibility.TextSizeChanged += (_, _) => ApplyAccessibility();
            _accessibility.HighContrastChanged += (_, _) => ApplyAccessibility();

            LoadStoredSettings();
            RegisterCriticalActions();

            // Let the XR loader settle one frame before the boot screen advances.
            yield return null;
            yield return null;

            var boot = Boot();
            if (!boot.IsSuccess)
            {
                _bootError = $"{boot.ErrorCode}: {boot.ErrorMessage}";
                Debug.LogError($"FaultlineUnityApp boot failed: {_bootError}");
                _navigator.TransitionTo(ApplicationState.ErrorRecovery);
            }
        }

        private void Update()
        {
            if (!_bootCompleted) return;

            _session?.UpdateIfSupported();
            _input?.Poll();

            var state = _input?.HandTrackingState;
            if (state != null)
            {
                var command = _interpreter.Interpret(state);
                if (command != null) HandleCommand(command);
            }

            if (_interactor != null && _deviceInput != null && _interactor.IsDraggingPanel)
            {
                if (_deviceInput.TryGetPointerRay(HandSide.Right, out var origin, out _) ||
                    _deviceInput.TryGetPointerRay(HandSide.Left, out origin, out _))
                    _interactor.ApplyDrag(origin);
            }

            TickSimulation();
        }

        private void OnDestroy()
        {
            _shutdownToken.Cancel();
            _shutdownToken.Dispose();
            _audio?.Dispose();
            if (_deviceSession != null) _deviceSession.EndSession();
            else _session?.EndSession();
        }

        // ------------------------------------------------------------------
        // Boot
        // ------------------------------------------------------------------

        private void InitialiseXr()
        {
            if (preferDeviceXr)
            {
                var deviceSession = new MetaXrSession();
                var init = deviceSession.Initialize();
                if (init.IsSuccess)
                {
                    var start = deviceSession.StartSession();
                    if (start.IsSuccess && deviceSession.HandSubsystem != null)
                    {
                        _deviceSession = deviceSession;
                        _deviceInput = new MetaXrInputProvider(deviceSession);
                        _session = deviceSession;
                        _input = _deviceInput;
                        ProviderKind = XrProviderKind.Device;
                        WireTrackingEvents(deviceSession);
                        return;
                    }

                    deviceSession.EndSession();
                }
            }

            var simulated = new Faultline.XR.XrSession();
            simulated.Initialize();
            simulated.StartSession();
            _session = simulated;
            _input = new SimulatedInputProvider(simulated);
            ProviderKind = XrProviderKind.Simulated;
            Debug.LogWarning("FaultlineUnityApp: no hand-tracking device detected — using the labelled simulation fallback. Device validation still required.");
            WireTrackingEvents(simulated);
        }

        private void WireTrackingEvents(IXrSession session)
        {
            session.TrackingLost += (_, e) =>
                _trackingLoss.OnTrackingLost(new[] { e.LostHand }, _ => SetStatusFromDomain());
            session.TrackingRecovered += (_, e) =>
                _trackingLoss.OnTrackingRecovered(new[] { e.RecoveredHand }, _ => SetStatusFromDomain());
        }

        private Result<bool> Boot()
        {
            var transition = _navigator.TransitionTo(ApplicationState.Ready);
            if (!transition.IsSuccess) return transition;

            _bootCompleted = true;
            RaiseChanged();
            return Result<bool>.Success(true);
        }

        // ------------------------------------------------------------------
        // Commands invoked by screens and gestures
        // ------------------------------------------------------------------

        public Result<bool> BeginScenarioAndOpenIncident()
        {
            var begin = _controller.BeginScenario();
            if (!begin.IsSuccess) return begin;

            var graph = _controller.Engine.Graph;
            _topology.BuildFromGraph(graph, graph);
            _topologyView.Rebuild(_topology);
            _healthBeforeRemediation = _controller.Engine.AggregateHealth;

            var open = _controller.OpenIncident();
            if (!open.IsSuccess) return open;

            return _navigator.TransitionTo(ApplicationState.ScenarioActive);
        }

        public Task<Result<bool>> InvestigateAsync()
        {
            _audio?.Dispatch();
            return _controller.Investigate(_shutdownToken.Token);
        }

        public Result<bool> ProposeRemediation() => _controller.ProposeRemediation();

        public Result<bool> ApproveRemediation()
        {
            var approve = _controller.ApproveRemediation();
            if (approve.IsSuccess) _audio?.Approval();
            return approve;
        }

        public Result<bool> RejectRemediation(string reason) => _controller.RejectRemediation(reason);

        public async Task<Result<bool>> ExecuteRemediationAsync()
        {
            _audio?.Dispatch();
            return await _controller.ExecuteRemediation(_shutdownToken.Token);
        }

        public Result<bool> VerifyRemediation()
        {
            var verify = _controller.VerifyRecovery();
            if (verify.IsSuccess) _audio?.Verified();
            return verify;
        }

        public Result<bool> ResolveIncident() => _controller.ResolveIncident();

        public Result<bool> ReplayScenario() => _controller.Replay();

        public Result<bool> SelectNode(string nodeId)
        {
            _topologyView.SelectNode(nodeId);
            _interpreter.SetHoveredNode(nodeId);
            RaiseChanged();
            return Result<bool>.Success(true);
        }

        public Result<bool> PauseScenario() => _navigator.TransitionTo(ApplicationState.Paused);

        public Result<bool> ResumeScenario() => _navigator.TransitionTo(ApplicationState.ScenarioActive);

        public void ResetLayout()
        {
            _layout.CaptureBasePose(_cameraAnchor.position, _cameraAnchor.forward);
            _topologyView?.ResetView();
            foreach (var view in _screenViews.Values)
                view.Relayout();
            SetStatusFromDomain();
        }

        public string DiagnosticsSummary()
        {
            var state = _controller == null ? "no controller" : _controller.Phase.ToString();
            return $"provider={ProviderKind}; phase={state}; screens={_screenViews.Count}; " +
                   $"nodes={_topologyView?.NodeCount ?? 0}; edges={_topologyView?.EdgeCount ?? 0}; " +
                   $"fps={1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime):F0}; " +
                   $"seated={_accessibility.SeatedOperation}; reducedMotion={_accessibility.ReducedMotion}; " +
                   $"contrast={_accessibility.HighContrast}; textScale={_accessibility.TextSizeScale:F2}; " +
                   $"audio={_audio?.Enabled == true}; handedness={(_layout.LeftHanded ? "left" : "right")}";
        }

        public void RegisterCriticalAction(string actionId, string visibleTargetId) =>
            _validator.RegisterCriticalAction(actionId, visibleTargetId);

        // ------------------------------------------------------------------
        // Async command bridge
        // ------------------------------------------------------------------

        private bool _busy;
        private string _busyStatus = string.Empty;

        public bool IsBusy => _busy;
        public string BusyStatus => _busyStatus;

        /// <summary>
        /// Run a controller command that returns a Task without blocking the XR
        /// frame loop. Progress is polled per frame — never with sleeps — and
        /// completion is reported on the main thread so views stay Unity-safe.
        /// </summary>
        public void RunAsync(Func<Task<Result<bool>>> work, string pendingLabel,
            Action<Result<bool>> onCompleted = null)
        {
            if (_busy)
            {
                Debug.LogWarning("FaultlineUnityApp: ignoring a command while another is running.");
                return;
            }

            StartCoroutine(PendingCommand(work, pendingLabel, onCompleted));
        }

        private System.Collections.IEnumerator PendingCommand(Func<Task<Result<bool>>> work,
            string pendingLabel, Action<Result<bool>> onCompleted)
        {
            _busy = true;
            _busyStatus = pendingLabel;
            SetStatusFromDomain();
            RaiseChanged();

            var task = work();
            while (task != null && !task.IsCompleted)
                yield return null;

            _busy = false;
            _busyStatus = string.Empty;

            if (task == null)
            {
                RaiseChanged();
                yield break;
            }

            if (task.IsFaulted)
            {
                Debug.LogError("FaultlineUnityApp: command faulted: " + task.Exception?.GetBaseException().Message);
                onCompleted?.Invoke(Result<bool>.Failure("FL-RT-001",
                    task.Exception?.GetBaseException().Message ?? "Unknown failure."));
            }
            else
            {
                var result = task.Result;
                if (result.IsSuccess) _audio?.Focus();
                onCompleted?.Invoke(result);
            }

            SetStatusFromDomain();
            RaiseChanged();
        }

        // ------------------------------------------------------------------
        // Accessibility + persistence
        // ------------------------------------------------------------------

        public void SetTextSizeScale(float scale) => _accessibility.TextSizeScale = scale;
        public void SetHighContrast(bool enabled) => _accessibility.HighContrast = enabled;
        public void SetReducedMotion(bool enabled) => _accessibility.ReducedMotion = enabled;
        public void SetSeatedOperation(bool enabled) => _accessibility.SeatedOperation = enabled;

        public void SetAudioEnabled(bool enabled)
        {
            if (_audio == null && enabled) _audio = new AudioCueService(transform);
            if (_audio != null) _audio.Enabled = enabled;
            ApplyAccessibility();
        }

        public void SetLeftHanded(bool leftHanded)
        {
            _layout.SetLeftHanded(leftHanded);
            ResetLayout();
        }

        public bool AudioEnabled => _audio != null && _audio.Enabled;

        private void ApplyAccessibility()
        {
            _topologyView?.SetReducedMotion(_accessibility.ReducedMotion);
            foreach (var view in _screenViews.Values)
                view.ApplyAccessibility(_accessibility);
            SetStatusFromDomain();
        }

        private void LoadStoredSettings()
        {
            var load = _persistence.Load();
            if (!load.IsSuccess) return;

            var settings = load.Value.Settings;
            if (settings == null) return;

            _accessibility.TextSizeScale = settings.TextSizeScale;
            _accessibility.HighContrast = settings.HighContrast;
            _accessibility.ReducedMotion = settings.ReducedMotion;
            _accessibility.SeatedOperation = settings.SeatedOperation;
            _layout.SetLeftHanded(settings.LeftHanded);
            SetAudioEnabled(settings.AudioEnabled);
        }

        public void SaveSettings()
        {
            var state = SessionState.Create(DateTimeOffset.UtcNow);
            state.Settings.TextSizeScale = _accessibility.TextSizeScale;
            state.Settings.HighContrast = _accessibility.HighContrast;
            state.Settings.ReducedMotion = _accessibility.ReducedMotion;
            state.Settings.SeatedOperation = _accessibility.SeatedOperation;
            state.Settings.LeftHanded = _layout.LeftHanded;
            state.Settings.AudioEnabled = AudioEnabled;
            var save = _persistence.Save(state);
            if (!save.IsSuccess)
                Debug.LogWarning($"FaultlineUnityApp: settings were not persisted ({save.ErrorCode}).");
        }

        // ------------------------------------------------------------------
        // Gesture → command translation
        // ------------------------------------------------------------------

        private void HandleCommand(InteractionCommand command)
        {
            switch (command.Type)
            {
                case InteractionCommandType.SelectNode:
                    if (!string.IsNullOrEmpty(command.TargetId))
                    {
                        _audio?.Select();
                        SelectNode(command.TargetId);
                    }
                    break;
                case InteractionCommandType.GrabNode:
                    _audio?.Focus();
                    break;
                case InteractionCommandType.ReleaseNode:
                    break;
                default:
                    break;
            }
        }

        private void OnNodeSelectionRequested(string nodeId)
        {
            SelectNode(nodeId);
            _navigator.NavigateToScreen(ScreenIds.ServiceInspection);
        }

        private void TickSimulation()
        {
            if (!_controller.IsScenarioRunning) return;
            if (_navigator.CurrentState == ApplicationState.Paused) return;

            _tickTimer += Time.unscaledDeltaTime;
            if (_tickTimer < SimulationTickSeconds) return;
            _tickTimer = 0f;

            _controller.Engine.Tick();
            SetStatusFromDomain();
        }

        /// <summary>
        /// Leaving the scenario for the welcome flow must stop the simulation;
        /// otherwise the tick loop keeps advancing a run nobody is looking at.
        /// </summary>
        private void OnApplicationStateChanged(ApplicationState next)
        {
            if (next == ApplicationState.Ready && _controller != null && _controller.IsScenarioRunning)
            {
                _controller.App.StopScenario();
                _topologyView?.ClearField();
                _healthBeforeRemediation = 0d;
                RaiseChanged();
            }
        }

        private void SetStatusFromDomain()
        {
            if (_controller?.Engine != null)
                _statusLine = $"Aggregate health {_controller.Engine.AggregateHealth:P0} · " +
                              $"root {_controller.RootCauseNodeId} · provider {ProviderKind}";
            RaiseChanged();
        }

        private string _statusLine = string.Empty;

        public string StatusLine => _statusLine;

        private double HealthBeforeRemediation => _healthBeforeRemediation;

        public double CurrentAggregateHealth => _controller?.Engine?.AggregateHealth ?? 0d;

        private void RegisterCriticalActions()
        {
            _validator.RegisterCriticalAction("begin-cascade", "ScenarioSelection.Start");
            _validator.RegisterCriticalAction("open-incident", "IncidentDetail.Investigate");
            _validator.RegisterCriticalAction("dispatch-agents", "AgentControlPanel.Dispatch");
            _validator.RegisterCriticalAction("propose", "InvestigationWorkspace.Propose");
            _validator.RegisterCriticalAction("approve", "ApprovalConfirmation.Approve");
            _validator.RegisterCriticalAction("reject", "ApprovalConfirmation.Reject");
            _validator.RegisterCriticalAction("execute", "RecoverySequence.Execute");
            _validator.RegisterCriticalAction("verify", "Verification.Confirm");
            _validator.RegisterCriticalAction("resolve", "Results.Continue");
            _validator.RegisterCriticalAction("replay", "Results.Replay");
        }

        private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Extension that lets the app drive either provider kind uniformly.</summary>
    internal static class XrSessionUpdateExtensions
    {
        public static void UpdateIfSupported(this IXrSession session)
        {
            if (session is MetaXrSession device) device.UpdateTrackingEvents();
        }
    }
}