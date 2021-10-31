//using UnityEngine;

//namespace ClientSide.EntityScripts.Shades
//{
//    public class Shade : MonoBehaviour
//    {
//        public static GameObject GenerateShade()//GameObject.CreatePrimitive(PrimitiveType.Cylinder).AddComponent<Shade>();
//        {
//            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

//            Transform playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
//            //center
//            gameObject.layer = LayerMask.NameToLayer("Primitive");

//            gameObject.GetComponent<CapsuleCollider>().radius = 0.5f;
//            gameObject.GetComponent<CapsuleCollider>().height = 2f;

//            Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
//            rigidbody.mass = 0.001f;
//            rigidbody.drag = 0f;
//            rigidbody.angularDrag = 0f;
//            rigidbody.isKinematic = true;

//            if (playerTransform.collider.enabled && gameObject.collider.enabled)
//            {
//                Physics.IgnoreCollision(gameObject.collider, playerTransform.collider);
//            }

//            gameObject.AddComponent<OWRigidbody>();

//            //Após testes reabilitar esses componentes mais deixalos com enable = false, e só liga-los se uma extrapolação for ocorrer
//            GameObject shadeGODetector = new GameObject
//            {
//                layer = LayerMask.NameToLayer("BasicEffectVolume")
//            };

//            shadeGODetector.GetComponent<Transform>().parent = gameObject.transform;
//            shadeGODetector.GetComponent<Transform>().name = "Detector";
//            shadeGODetector.AddComponent<SphereCollider>().isTrigger = true;

//            shadeGODetector.AddComponent<AlignmentFieldDetector>();
//            gameObject.AddComponent<AlignWithField>();

//            gameObject.AddComponent<ShadeDetachHandler>();

//            //Colliders = new Transform[] { shadeGODetector.transform, collider.transform };

//            gameObject.transform.position = playerTransform.position;
//            gameObject.transform.rotation = playerTransform.rotation;

//            gameObject.transform.parent = GameObject.Find("TimberHearth_Body").transform;

//            //         //Serve para fazer o player seguir o shade, "não importa o que ocorra"
//            //         playerTransform.GetComponent<PlayerCharacterController>().LockMovement(false);
//            //         playerTransform.gameObject.AddComponent<OWRigidbodyFollowsAnother>().SetConstrain(OWUtilities.GetAttachedOWRigidbody(gameObject, false));

//            return gameObject;
//        }
//    }
//}
