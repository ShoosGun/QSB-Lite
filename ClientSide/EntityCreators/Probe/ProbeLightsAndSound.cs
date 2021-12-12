using UnityEngine;

namespace SNet_Client.EntityCreators.Probe
{
    //TODO fazer aqui o som do probe voando, se ancorando e sua lanterna
    public class ProbeLightsAndSound : MonoBehaviour
    {
		Light probeLight;
        public void Start()
        {
            probeLight = CreatePlayerLight(transform);
        }
        private Light CreatePlayerLight(Transform probeT)
        {
            GameObject lightGO = new GameObject("probe_light");
            lightGO.transform.parent = probeT.transform;
            lightGO.transform.localPosition = new Vector3(0f, 0f, -1f);
            lightGO.transform.localRotation = Quaternion.identity;
            lightGO.transform.localScale = Vector3.one;

            Light l = lightGO.AddComponent<Light>();
            l.range = 50f;
            l.intensity = 1f;
            l.type = LightType.Point;
            l.shadows = LightShadows.None;
            return l;
        }
    }
}
