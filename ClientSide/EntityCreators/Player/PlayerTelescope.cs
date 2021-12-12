using UnityEngine;

using SNet_Client.Utils;


namespace SNet_Client.EntityCreators.Player
{
    public class PlayerTelescope : MonoBehaviour
    {
        GameObject telescope;
        public void Start()
        {
            telescope = CreateJetpackMesh(transform);
            telescope.SetActive(false);
        }

        public void EquipTele(bool equip = true)
        {
            telescope.SetActive(equip);
        }

        private GameObject CreateJetpackMesh(Transform playerT)
        {
            GameObject tele = new GameObject("telescope_mesh");
            tele.transform.parent = playerT.transform;
            tele.transform.localPosition = new Vector3(0.07f, 0.99f, 0.35f);
            tele.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            tele.transform.localScale = Vector3.one;
            if (ResourceLoader.GetTelescopeMeshAndMaterial(out MeshMaterialCombo telescopeMeshAndMaterial))
            {
                tele.AddComponent<MeshFilter>().mesh = telescopeMeshAndMaterial.mesh;
                tele.AddComponent<MeshRenderer>().material = telescopeMeshAndMaterial.material;
            }
            return tele;
        }
    }
}
