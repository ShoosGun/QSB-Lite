using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SNet_Client.Utils
{
    public class ResourceLoader
    {
        public static bool GetGameObjectMeshAndMaterial(string path, out MeshMaterialCombo materialAndMesh, bool isSkinnedMesh = false)
        {
            materialAndMesh = new MeshMaterialCombo();
            var go = GameObject.Find(path);
            if (go == null)
                return false;

            if (isSkinnedMesh)
                materialAndMesh.mesh = go.GetComponent<SkinnedMeshRenderer>().sharedMesh;
            else
                materialAndMesh.mesh = go.GetComponent<MeshFilter>().mesh;

            materialAndMesh.material = go.GetComponent<Renderer>().material;

            return true;
        }
        private static MeshMaterialCombo villagerMeshAndMaterial;
        public static bool GetVillagerMeshAndMaterial(out MeshMaterialCombo materialAndMesh)
        {
            //Villagers/Craftsman/Villager_Base/villager_rig:Villager_Dude

            if (villagerMeshAndMaterial.mesh != null)
            {
                materialAndMesh = villagerMeshAndMaterial;
                return true;
            }
            bool result = GetGameObjectMeshAndMaterial("Villagers/Craftsman/Villager_Base/villager_rig:Villager_Dude", out villagerMeshAndMaterial, true);
            materialAndMesh = villagerMeshAndMaterial;
            return result;
        }

        private static MeshMaterialCombo jetpackMeshAndMaterial;
        public static bool GetJetpackMeshAndMaterial(out MeshMaterialCombo materialAndMesh)
        {
            //CaveEntrance/SpaceSuit/SuitLayout_ZeroGChamber/JetPackBody

            if (jetpackMeshAndMaterial.mesh != null)
            {
                materialAndMesh = jetpackMeshAndMaterial;
                return true;
            }
            bool result = GetGameObjectMeshAndMaterial("CaveEntrance/SpaceSuit/SuitLayout_ZeroGChamber/JetPackBody", out jetpackMeshAndMaterial);
            materialAndMesh = jetpackMeshAndMaterial;
            return result;
        }

        private static MeshMaterialCombo anglerfishMeshAndMaterial;
        public static bool GetAnglerfishMeshAndMaterial(out MeshMaterialCombo materialAndMesh)
        {
            //Anglerfish_Base/anglerfish_rig:AnglerFish

            if (anglerfishMeshAndMaterial.mesh != null)
            {
                materialAndMesh = anglerfishMeshAndMaterial;
                return true;
            }
            bool result = GetGameObjectMeshAndMaterial("Anglerfish_Base/anglerfish_rig:AnglerFish", out anglerfishMeshAndMaterial, true);
            materialAndMesh = anglerfishMeshAndMaterial;
            return result;
        }

        private static MeshMaterialCombo probehMeshAndMaterial;
        public static bool GetProbeMeshAndMaterial(out MeshMaterialCombo materialAndMesh)
        {
            //Probe

            if (probehMeshAndMaterial.mesh != null)
            {
                materialAndMesh = probehMeshAndMaterial;
                return true;
            }
            bool result = GetGameObjectMeshAndMaterial("Probe", out probehMeshAndMaterial);
            materialAndMesh = probehMeshAndMaterial;
            return result;
        }

        private static MeshMaterialCombo telescopehMeshAndMaterial;
        public static bool GetTelescopeMeshAndMaterial(out MeshMaterialCombo materialAndMesh)
        {
            //Kidtelescope
            if (telescopehMeshAndMaterial.mesh != null)
            {
                materialAndMesh = telescopehMeshAndMaterial;
                return true;
            }
            bool result = GetGameObjectMeshAndMaterial("Kidtelescope", out telescopehMeshAndMaterial);
            materialAndMesh = telescopehMeshAndMaterial;
            return result;
        }
    }

    public struct MeshMaterialCombo
    {
        public Mesh mesh;
        public Material material;
    }
}
