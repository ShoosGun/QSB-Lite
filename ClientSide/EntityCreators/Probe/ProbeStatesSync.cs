using UnityEngine;

using SNet_Client.Utils;
using SNet_Client.EntityScripts.StateSync;

namespace SNet_Client.EntityCreators.Probe
{
    public class ProbeStatesSync : MonoBehaviour
    {
        private EntityStatesSync statesSync;

        private ProbeLightsAndSound lightsAndSound;

        public void Start()
        {
            statesSync = GetComponent<EntityStatesSync>();
            lightsAndSound = GetComponent<ProbeLightsAndSound>();

            if (gameObject.GetAttachedNetworkedEntity().IsOurs())
            {
                GlobalMessenger.AddListener("ProbeAnchorToSurface", OnProbeAnchorToSurface);
            }

            statesSync.AddStateListener((byte)ProbeStates.ATTACHED, false, ProbeAnchorToSurface);
        }

        public void OnDestroy()
        {
            GlobalMessenger.RemoveListener("ProbeAnchorToSurface", OnProbeAnchorToSurface);
        }

        private void OnProbeAnchorToSurface() => statesSync.ChangeValue((byte)ProbeStates.ATTACHED, true);

        private void ProbeAnchorToSurface(bool isAttached)
        {
            if (lightsAndSound != null)
                lightsAndSound.ProbeAttached(isAttached);
        } 
    }
}
