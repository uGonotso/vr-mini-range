using UnityEngine.XR.Interaction.Toolkit;

namespace VRMiniRange.Feedback
{
    public static class HapticFeedback
    {
        /// <summary>
        /// Send haptic pulse to the controller holding an interactable
        /// </summary>
        public static void Pulse(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor interactor, float amplitude, float duration)
        {
            if (interactor is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor controllerInteractor)
            {
                if (controllerInteractor.xrController != null)
                {
                    controllerInteractor.xrController.SendHapticImpulse(amplitude, duration);
                }
            }
        }

        /// <summary>
        /// Send haptic pulse directly to a controller
        /// </summary>
        public static void Pulse(XRBaseController controller, float amplitude, float duration)
        {
            controller?.SendHapticImpulse(amplitude, duration);
        }

        // Preset pulses for consistency
        public static void LightPulse(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor interactor) => Pulse(interactor, 0.2f, 0.05f);
        public static void MediumPulse(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor interactor) => Pulse(interactor, 0.4f, 0.1f);
        public static void StrongPulse(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor interactor) => Pulse(interactor, 0.6f, 0.15f);
        public static void FirePulse(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor interactor) => Pulse(interactor, 0.3f, 0.08f);
        public static void HitConfirmPulse(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor interactor) => Pulse(interactor, 0.5f, 0.12f);
    }
}
