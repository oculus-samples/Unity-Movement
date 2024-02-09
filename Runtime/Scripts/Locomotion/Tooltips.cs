// Copyright (c) Meta Platforms, Inc. and affiliates.

namespace Oculus.Movement.Locomotion
{
    public static class StateTransitionTooltips
    {
        public const string Duration =
            "Seconds of inactivity till the temporary state exits";

        public const string EnterTime =
            "Seconds that the " + nameof(StateTransition.OnTransitionEvents.Entering) + " callback will be called.";

        public const string ExitTime =
            "Seconds that the " + nameof(StateTransition.OnTransitionEvents.Exiting) + " callback will be called.";

        public const string OnTransition =
            "Callbacks to trigger at specific points during the transition between states.";
    }

    public static class TriggerRetargetingConstraintMaskTooltip
    {
        public const string Mask =
            "Section of body where body tracking continues animating while activity triggers";

        public const string Animator =
            "The body being animated";
    }
}
