using UnityEngine;

namespace SNet_Client.EntityScripts.Shades
{
    public class Shade
    {
        public static GameObject GenerateShadeThatFollowsThePlayer(bool colliderDissabled = true)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);            
            Transform playerTransform = Locator.GetPlayerTransform();
            
            gameObject.layer = LayerMask.NameToLayer("Primitive");

            CapsuleCollider c = gameObject.GetComponent<CapsuleCollider>();
            c.radius = 0.5f;
            c.height = 2f;
            c.enabled = !colliderDissabled;

            Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
            rigidbody.mass = 0.001f;
            rigidbody.drag = 0f;
            rigidbody.angularDrag = 0f;

            gameObject.AddComponent<OWRigidbody>();

            gameObject.transform.position = playerTransform.position;
            gameObject.transform.rotation = playerTransform.rotation;
            gameObject.transform.parent = playerTransform;

            return gameObject;
        }
    }
}
