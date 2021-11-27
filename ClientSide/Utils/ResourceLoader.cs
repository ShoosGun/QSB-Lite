using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

//TODO adicionar chaching desses dados
//TODO descobrir como fazer isso funcionar
namespace SNet_Client.Utils
{
    public class ResourceLoader
    {
		public static bool GetGameObjectMeshAndMaterial(string path, out MeshMaterialCombo materialAndMesh, bool isSkinnedMesh = false)
		{
			materialAndMesh = new MeshMaterialCombo();
			var go = GameObject.Find(path);
			if(go == null)
				return false;

            if(isSkinnedMesh)
                materialAndMesh.mesh = go.GetComponent<SkinnedMeshRenderer>().sharedMesh;
            else
                materialAndMesh.mesh = go.GetComponent<MeshFilter>().mesh;

            materialAndMesh.material = go.GetComponent<Renderer>().material;
			
			return true;
		}
        public static bool GetVillagerMeshAndMaterial(out MeshMaterialCombo materialAndMesh)
        {
			//Villagers/Craftsman/Villager_Base/villager_rig:Villager_Dude
			return GetGameObjectMeshAndMaterial("Villagers/Craftsman/Villager_Base/villager_rig:Villager_Dude", out materialAndMesh, true);
        }
        public static bool GetJetpackMeshAndMaterial(out MeshMaterialCombo materialAndMesh)
        {
			//CaveEntrance/SpaceSuit/SuitLayout_ZeroGChamber/JetPackBody
			return GetGameObjectMeshAndMaterial("CaveEntrance/SpaceSuit/SuitLayout_ZeroGChamber/JetPackBody", out materialAndMesh);
        }
        public static bool GetAnglerfishMeshAndMaterial(out MeshMaterialCombo materialAndMesh)
        {
            //TODO reencontrar o nome desse
			//AnglerFishContainer/AnglerFish/Anglerfish_Base/anglerfish_rig:AnglerFish
			return GetGameObjectMeshAndMaterial("AnglerFishContainer/AnglerFish/Anglerfish_Base/anglerfish_rig:AnglerFish", out materialAndMesh, true);
        }
        public static bool GetProbeMeshAndMaterial(out MeshMaterialCombo materialAndMesh)
        {
            //TODO reencontrar o nome desse
            //Satellite_Body/ProbeMesh
            return GetGameObjectMeshAndMaterial("Satellite_Body/ProbeMesh", out materialAndMesh);
        }
    }

    public struct MeshMaterialCombo
    {
        public Mesh mesh;
        public Material material;
    }
}
