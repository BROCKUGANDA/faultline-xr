namespace Faultline.Runtime.UI.Screens
{
    using System;

    using UnityEngine;

    using Faultline.Presentation;
    using Faultline.Runtime.UI;
    using Faultline.XR;

    /// <summary>
    /// PRD §6.1 screen 4 — hand tutorial. Target, pinch, release and inspect are
    /// practised as separate steps, each coached to one hand. Per-hand status is
    /// read from the live XR session every poll tick, and a step only completes on
    /// a gesture the provider actually reported for that hand. When a provider
    /// cannot report the coached hand, "Practised" continues without detection and
    /// says so; it never pretends a gesture happened.
    /// </summary>
    public sealed class HandTutorialScreenView : ScreenViewBase, HandTutorialScreen
    {
        private const int TargetStep = 0;
        private const int PinchStep = 1;
        private const int ReleaseStep = 2;
        private const int InspectStep = 3;

        private const float PollSeconds = 0.25f;

        private static readonly (string Name, HandSide Hand, string Copy)[] Gestures =
        {
            ("Target", HandSide.Right,
             "Raise your right hand into view. The runtime labels it Tracking once it has a " +
             "pose, and your hand is then the pointer — no controller, no reticle to hold."),
            ("Pinch", HandSide.Right,
             "Pinch thumb and index together on the right hand. This single gesture selects a " +
             "button, a service node or an evidence card."),
            ("Release", HandSide.Right,
             "Open the right hand again to release. Release is a real edge in the tracking " +
             "stream: if the hand drops out of view mid-gesture, nothing is triggered."),
            ("Inspect", HandSide.Left,
             "Hold a pinch with the left hand on a panel or node. A held pinch grabs it, so you " +
             "can drag it into reach and drop it where you want it."),
        };

        private int _gesture;
        private bool _detected;
        private bool _pinchSeen;
        private bool _pinchHeld;
        private bool _practiceTracked;
        private string _leftStatus = "Not read yet";
        private string _rightStatus = "Not read yet";
        private string _note;
        private float _pollTimer;

        private FlButton _nextButton;
        private FlButton _backButton;
        private FlButton _practisedButton;

        public override string ScreenId => ScreenIds.HandTutorial;
        public override string Title => "Hand tutorial";
        public override string BackScreenId => ScreenIds.Onboarding;
        public override string EntryConditions => "Onboarding finished or was skipped.";
        public override string ExitConditions =>
            "All four gestures practised with the coached hand, or the tutorial skipped.";
        public override string InteractionModel =>
            "Practise each gesture and watch the per-hand status lines; Next advances once the " +
            "hand you are coaching has really performed it.";
        public override string AccessibleDescription =>
            $"Hand tutorial. Gesture {_gesture + 1} of {Gestures.Length}: {CurrentGesture}, " +
            $"coached to the {Gestures[_gesture].Hand} hand. Live status is shown as text for " +
            "both hands, including any tracking failure.";

        public string CurrentGesture => Gestures[_gesture].Name;
        public bool GestureCompleted => _detected;
        public string LeftHandStatus => _leftStatus;
        public string RightHandStatus => _rightStatus;

        public event EventHandler<HandTutorialGestureEventArgs> GestureDemonstrated;
        public event EventHandler TutorialCompleted;
        public event EventHandler TutorialSkipped;

        private HandSide PracticeHand => Gestures[_gesture].Hand;

        /// <summary>
        /// True only while the device provider reports a tracked pose for the hand
        /// being coached; otherwise automated detection cannot be trusted.
        /// </summary>
        private bool DetectionLive =>
            App != null && App.ProviderKind == XrProviderKind.Device && _practiceTracked;

        protected override FlPanel BuildPanel() =>
            CreatePanelInZone("HANDS, NOT CONTROLLERS", SpatialZone.Centre, 760f, 570f);

        protected override void BuildContent(FlPanel panel)
        {
            _nextButton = panel.AddButton("tutorial-next", "Try the next gesture",
                "Advance to the next gesture once this one is performed", OnNext);
            _backButton = panel.AddButton("tutorial-back", "Previous gesture",
                "Return to the previous gesture, or to onboarding at the first one", OnBack);
            _practisedButton = panel.AddButton("tutorial-practised", "Practised — continue",
                "Continue without gesture detection when this provider cannot report the coached hand",
                OnPractised);
            panel.AddButton("tutorial-skip", "Skip tutorial",
                "End the tutorial and open scenario selection", OnSkip);

            App.Input.PinchStarted += (_, e) => OnPinchStarted(e.Hand);
            App.Input.PinchEnded += (_, e) => OnPinchEnded(e.Hand);
            App.Input.GrabStarted += (_, e) => OnGrabStarted(e.Hand);
            App.Changed += (_, _) => Refresh();

            PollHands();
            Refresh();
        }

        protected override void OnEntered()
        {
            _pollTimer = PollSeconds;
            Refresh();
        }

        private void Update()
        {
            if (App == null || Panel == null) return;
            _pollTimer += Time.unscaledDeltaTime;
            if (_pollTimer < PollSeconds) return;
            _pollTimer = 0f;
            PollHands();
            Refresh();
        }

        private void OnNext()
        {
            if (!_detected)
            {
                Panel.SetStatusLine(_practisedButton.Interactable
                    ? "Practise the gesture, or pinch Practised to continue without detection."
                    : "Practise this gesture with the " + PracticeWord(PracticeHand) +
                      " hand — Next unlocks when it is detected.");
                return;
            }

            if (_gesture < InspectStep)
            {
                StepTo(_gesture + 1);
                return;
            }

            TutorialCompleted?.Invoke(this, EventArgs.Empty);
            FinishAndRoute(ScreenIds.ScenarioSelection);
        }

        private void OnBack()
        {
            if (_gesture > TargetStep)
            {
                StepTo(_gesture - 1);
                return;
            }

            var onboarding = App.Navigator.NavigateToScreen(ScreenIds.Onboarding);
            if (!onboarding.IsSuccess)
                Panel.SetStatusLine($"{onboarding.ErrorCode} · onboarding would not reopen.");
        }

        private void OnPractised()
        {
            if (DetectionLive) return;
            _detected = true;
            _note = "Advanced without detection: this provider does not report the " +
                    PracticeWord(PracticeHand) + " hand.";
            Refresh();
        }

        private void OnSkip()
        {
            TutorialSkipped?.Invoke(this, EventArgs.Empty);
            FinishAndRoute(ScreenIds.ScenarioSelection);
        }

        private void FinishAndRoute(string screenId)
        {
            MarkWelcomeOnboarded();
            var next = App.Navigator.NavigateToScreen(screenId);
            if (!next.IsSuccess)
                SetError(next.ErrorCode,
                    "Scenario selection would not open. The tutorial result is kept — try again " +
                    "from the welcome screen.", canRetry: true);
        }

        /// <summary>
        /// WelcomeScreenView only clears its onboarding flag when the tutorial
        /// finishes or is skipped, so the flag is set through the navigator rather
        /// than assumed.
        /// </summary>
        private void MarkWelcomeOnboarded()
        {
            if (App.Navigator.TryGetScreen(ScreenIds.Welcome, out var screen) &&
                screen is WelcomeScreenView welcome)
                welcome.MarkOnboardingCompleted();
        }

        private void StepTo(int next)
        {
            _gesture = Mathf.Clamp(next, TargetStep, Gestures.Length - 1);
            _detected = false;
            _pinchSeen = false;
            _pinchHeld = false;
            _note = null;
            Refresh();
        }

        private void OnPinchStarted(HandSide hand)
        {
            if (hand != PracticeHand) return;
            _pinchSeen = true;
            _pinchHeld = true;
            if (_detected) return;
            if (_gesture == TargetStep || _gesture == PinchStep) Detect();
        }

        private void OnPinchEnded(HandSide hand)
        {
            if (hand != PracticeHand) return;
            _pinchHeld = false;
            if (_detected) return;
            if (_gesture == ReleaseStep && _pinchSeen) Detect();
        }

        private void OnGrabStarted(HandSide hand)
        {
            if (hand != PracticeHand) return;
            _pinchSeen = true;
            _pinchHeld = true;
            if (_detected) return;
            if (_gesture == TargetStep || _gesture == PinchStep || _gesture == InspectStep) Detect();
        }

        private void Detect()
        {
            _detected = true;
            _note = null;
            GestureDemonstrated?.Invoke(this, new HandTutorialGestureEventArgs(CurrentGesture, true));
            Refresh();
        }

        private void PollHands()
        {
            if (App.Session == null)
            {
                _leftStatus = _rightStatus = "No XR session on this provider";
                _practiceTracked = false;
                return;
            }

            var reading = App.Session.GetHandTrackingState();

            if (!reading.IsSuccess)
            {
                _leftStatus = _rightStatus = $"Not readable · {reading.ErrorCode}";
                _practiceTracked = false;
                return;
            }

            var state = reading.Value;
            _leftStatus = Describe(state.Left);
            _rightStatus = Describe(state.Right);

            var practice = PracticeHand == HandSide.Left ? state.Left : state.Right;
            _practiceTracked = practice.Status == HandTrackingStatus.Tracking;

            if (!_detected && _gesture == TargetStep && _practiceTracked)
            {
                Detect();
                return;
            }

            if (_pinchHeld && !_practiceTracked)
            {
                _pinchHeld = false;
                _pinchSeen = false;
                _note = $"Tracking lost mid-gesture on the {PracticeWord(PracticeHand)} hand — " +
                        "nothing was triggered.";
                GestureDemonstrated?.Invoke(this,
                    new HandTutorialGestureEventArgs(CurrentGesture, false));
            }
        }

        private void Refresh()
        {
            if (Panel == null || HasError) return;

            var (name, hand, copy) = Gestures[_gesture];
            Panel.SetBody(
                $"Gesture {_gesture + 1} of {Gestures.Length} · {name} · {PracticeWord(hand)} hand\n\n" +
                $"{copy}\n\n" +
                $"Left hand:   {_leftStatus}\n" +
                $"Right hand:  {_rightStatus}\n\n" +
                (_detected ? "Detected — pinch Next." : App.Messages.TutorialBody));

            Panel.SetStatusLine(_note ?? DetectionLine());

            _nextButton.Label = _gesture == InspectStep ? "Finish tutorial" : "Try the next gesture";
            _backButton.Label = _gesture == TargetStep ? "Back to onboarding" : "Previous gesture";
            _practisedButton.Interactable = !DetectionLive && !_detected;
        }

        private string DetectionLine() =>
            DetectionLive
                ? $"Provider {App.ProviderKind} · detecting the {PracticeWord(PracticeHand)} hand"
                : $"Provider {App.ProviderKind} · no live detection for the " +
                  $"{PracticeWord(PracticeHand)} hand — Practised continues without it";

        private static string PracticeWord(HandSide hand) =>
            hand == HandSide.Left ? "left" : "right";

        private static string Describe(HandData hand) => hand.Status switch
        {
            HandTrackingStatus.Tracking => hand.PinchActive
                ? "Tracking · pinch held"
                : "Tracking · ready",
            HandTrackingStatus.Available => "Available · no pose data yet",
            HandTrackingStatus.Lost => "Lost · bring the hand into view",
            _ => "Not supported on this device",
        };
    }
}
