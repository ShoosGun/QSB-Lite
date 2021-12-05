using UnityEngine;

namespace SNet_Client.EntityCreators.Player
{
    public class PlayerLight : MonoBehaviour
    {
        Light playerLight;
        public void Start()
        {
            playerLight = CreatePlayerLight(transform);
            playerLight.enabled = false;
        }
        public void TurnLight(bool on = true)
        {
            playerLight.enabled = on;
        }
        private Light CreatePlayerLight(Transform playerT)
        {
            GameObject lightGO = new GameObject("player_light");
            lightGO.transform.parent = playerT.transform;
            lightGO.transform.localPosition = new Vector3(0f, 0.9f, 0.15f);
            lightGO.transform.localRotation = Quaternion.identity;
            lightGO.transform.localScale = Vector3.one;

            Light l = lightGO.AddComponent<Light>();
            l.range = 50f;
            l.spotAngle = 80f;
            l.intensity = 1f;
            l.type = LightType.Spot;
            l.shadows = LightShadows.None;
            return l;
        }
    }
}
