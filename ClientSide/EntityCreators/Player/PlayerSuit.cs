using UnityEngine;

using SNet_Client.Utils;


namespace SNet_Client.EntityCreators.Player
{
    public class PlayerSuit : MonoBehaviour
    {
        GameObject jetpack;
        public void Start()
        {
            jetpack = CreateJetpackMesh(transform);
            jetpack.SetActive(false);
        }

        public void EquipSuit(bool SuitUp = true)
        {
            jetpack.SetActive(SuitUp);
        }

        private GameObject CreateJetpackMesh(Transform playerT)
        {
            GameObject jetpack = new GameObject("jetpack_mesh");
            jetpack.transform.parent = playerT.transform;
            jetpack.transform.localPosition = new Vector3(0f, 0.4f, -0.3f);
            jetpack.transform.localRotation = Quaternion.identity;
            jetpack.transform.localScale = Vector3.one * 0.7731431f;
            if (ResourceLoader.GetJetpackMeshAndMaterial(out MeshMaterialCombo jetpackMeshAndMaterial))
            {
                jetpack.AddComponent<MeshFilter>().mesh = jetpackMeshAndMaterial.mesh;
                jetpack.AddComponent<MeshRenderer>().material = jetpackMeshAndMaterial.material;
            }
            return jetpack;
        }
    }
}
