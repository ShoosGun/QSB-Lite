using UnityEngine;

namespace SNet_Client.Utils
{
    public static class AnimationClipHelpers
    {
        public static void SetLinearTranslationCurve(this AnimationClip animationClip, string childTransform, float initialTime, Vector3 initialPosition, float endTime, Vector3 endPosition)
        {
            animationClip.SetCurve(childTransform, typeof(Transform), "localPosition.x", AnimationCurve.Linear(initialTime, initialPosition.x, endTime, endPosition.x));
            animationClip.SetCurve(childTransform, typeof(Transform), "localPosition.y", AnimationCurve.Linear(initialTime, initialPosition.y, endTime, endPosition.y));
            animationClip.SetCurve(childTransform, typeof(Transform), "localPosition.z", AnimationCurve.Linear(initialTime, initialPosition.z, endTime, endPosition.z));
        }
        public static void SetLinearRotationCurve(this AnimationClip animationClip, string childTransform, float initialTime, Quaternion initialRotation, float endTime, Quaternion endRotation)
        {
            animationClip.SetCurve(childTransform, typeof(Transform), "localRotation.w", AnimationCurve.Linear(initialTime, initialRotation.w, endTime, endRotation.w));
            animationClip.SetCurve(childTransform, typeof(Transform), "localRotation.x", AnimationCurve.Linear(initialTime, initialRotation.x, endTime, endRotation.x));
            animationClip.SetCurve(childTransform, typeof(Transform), "localRotation.y", AnimationCurve.Linear(initialTime, initialRotation.y, endTime, endRotation.y));
            animationClip.SetCurve(childTransform, typeof(Transform), "localRotation.z", AnimationCurve.Linear(initialTime, initialRotation.z, endTime, endRotation.z));
        }
    }
}
