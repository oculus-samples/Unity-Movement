// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

namespace Meta.XR.Movement.Editor
{
    /// <summary>
    /// Has useful calls for recording telemetry information related to editor interactions.
    /// </summary>
    public static class TelemetryManager
    {
        public static string _PRODUCT_TYPE = "movement_sdk";
        public static string _ALIGN_TARGET_TO_SOURCE_EVENT_NAME = "align_target_to_source";
        public static string _CREATE_OR_UPDATE_HANDLE_EVENT_NAME = "create_or_update_handle";
        public static string _CREATE_OR_UPDATE_UTILITY_CONFIG_EVENT_NAME = "create_or_update_utility_config";

        private static bool _loggedOVRBody = false;
        private static bool _loggedOVRFace = false;
        private static bool _loggedOVREye = false;

        /// <summary>
        /// Send a config event.
        /// </summary>
        /// <param name="eventName">Event name.</param>
        /// <param name="errorMessage">Error message. None means no error was encountered.</param>
        public static void SendConfigEvent(string eventName, string errorMessage = null)
        {
            SendEvent(isEssentialEvent: errorMessage != null ? true : false, eventNameToLog: eventName,
                errorMessage: errorMessage);
        }

        /// <summary>
        /// Use this to log error messages.
        /// </summary>
        /// <param name="eventName">Name of the telemetry event.</param>
        /// <param name="errorMessage">Error message.</param>
        public static void SendErrorEvent(string eventName, string errorMessage)
        {
            SendEvent(isEssentialEvent: true, eventNameToLog: eventName, errorMessage: errorMessage);
        }

        /// <summary>
        /// Found <see cref="OVRBody"/> component.
        /// </summary>
        public static void SendFoundOVRBodyEvent()
        {
            if (_loggedOVRBody)
            {
                return;
            }
            SendEvent(true, "found_ovr_body_component");
            _loggedOVRBody = true;
        }

        /// <summary>
        /// Found <see cref="OVRFaceExpressions"/> component.
        /// </summary>
        public static void SendFoundOVRFaceExpressionsEvent()
        {
            if (_loggedOVRFace)
            {
                return;
            }
            SendEvent(true, "found_ovr_face_expressions_component");
            _loggedOVRFace = true;
        }

        /// <summary>
        /// Found <see cref="OVREyeGaze"/> component.
        /// </summary>
        public static void SendFoundOVREyeGazeEvent()
        {
            if (_loggedOVREye)
            {
                return;
            }
            SendEvent(true, "found_ovr_eye_gaze_component");
            _loggedOVREye = true;
        }

        private static void SendEvent(
            bool isEssentialEvent,
            string eventNameToLog,
            string eventJsonPayload = null,
            string errorMessage = null)
        {
#if META_XR_CORE_V78_MIN
            var telemetryResult = OVRPlugin.SendUnifiedEvent(
                isEssential: isEssentialEvent ? OVRPlugin.Bool.True : OVRPlugin.Bool.False,
                productType: _PRODUCT_TYPE,
                eventName: eventNameToLog,
                event_metadata_json: eventJsonPayload,
                error_msg: errorMessage);
            if (telemetryResult != OVRPlugin.Result.Success)
            {
                UnityEngine.Debug.LogWarning($"Telemetry call failure, return value: {telemetryResult}.");
            }
#endif
        }
    }
}
