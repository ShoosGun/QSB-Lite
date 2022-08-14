using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace SNet_Client.Utils
{
    public static class ReferenceFrameLocator
    {
        private static Dictionary<ReferenceFrames, ReferenceFrameData> referenceFrames = new Dictionary<ReferenceFrames, ReferenceFrameData>();

        private static bool CacheReferenceFrame(ReferenceFrames referenceFrame, out Transform cachedTransform)
        {
            bool foundTheObject = false;
            cachedTransform = null;

            if (referenceFrame == ReferenceFrames.GlobalRoot)
                return false;
            
            for (int i = 0; i < referenceFrameObjectData[referenceFrame].Length && !foundTheObject; i++)
            {
                ReferenceFrameFindingData findingData = referenceFrameObjectData[referenceFrame][i];

                GameObject gameObject = GameObject.Find(findingData.ObjectSceneName);
                if (gameObject != null)
                {
                    referenceFrames[referenceFrame].ReferenceFrameTransform = gameObject.transform;
                    referenceFrames[referenceFrame].RadiusOfInfluence = referenceFrameObjectData[referenceFrame][i].RadiusOfInfluence;
                    cachedTransform = gameObject.transform;
                    foundTheObject = true;
                }
            }
            return foundTheObject;
        }
        public static void CacheAllReferenceFrames()
        {
            foreach (var pair in referenceFrameObjectData)
            {
                if (pair.Key != ReferenceFrames.GlobalRoot && pair.Key != ReferenceFrames.AnyOWRigidbody)
                {
                    bool foundTheObject = false;
                    for (int i = 0; i < pair.Value.Length && !foundTheObject; i++)
                    {
                        GameObject gameObject = GameObject.Find(pair.Value[i].ObjectSceneName);
                        if (gameObject != null)
                        {
                            ReferenceFrameData data = new ReferenceFrameData
                            {
                                ReferenceFrameTransform = gameObject.transform,
                                RadiusOfInfluence = pair.Value[i].RadiusOfInfluence,
                            };

                            referenceFrames[pair.Key] = data;

                            foundTheObject = true;
                        }
                    }
                }
            }
        }
        public static void ForEach(Action<KeyValuePair<ReferenceFrames, ReferenceFrameData>> action)
        {
            foreach (var pair in referenceFrames)
                action(pair);
        }

        private static bool GetAnyOWRigidbody(out Transform reference)
        {
            reference = null;
            OWRigidbody rigidbody = (OWRigidbody)GameObject.FindObjectOfType(typeof(OWRigidbody));

            if (rigidbody == null)
                return false;

            reference = rigidbody.transform;
            return true;
        }
        public static Transform GetReferenceFrame(ReferenceFrames referenceFrame)
        {
            if (referenceFrame == ReferenceFrames.GlobalRoot)
                return null;

            Transform reference;

            if (referenceFrame == ReferenceFrames.AnyOWRigidbody)
            {
                if(!GetAnyOWRigidbody(out reference))
                    Debug.Log("Couldn't find any OWRigidbody, returning null.");

                return reference;
            }
            if (!referenceFrames.ContainsKey(referenceFrame))
                referenceFrames.Add(referenceFrame, new ReferenceFrameData { ReferenceFrameTransform = null});
        
            reference = referenceFrames[referenceFrame].ReferenceFrameTransform;

            if (reference == null)
            {
                Debug.Log("OWRigidbody for this reference frame wasn't cached, doing so now.");
                if (!CacheReferenceFrame(referenceFrame, out reference))
                {
                    Debug.Log("Couldn't find specific OWRigidbody, searching for any OWRigidbody.");
                    if (!GetAnyOWRigidbody(out reference))
                    {
                        Debug.Log("Couldn't find any OWRigidbody, returning null.");
                    }
                }
            }
            return reference;
        }
        //TODO Adicionar aqui as ilhas em GD (por elas não serem previsiveis)
        private static readonly Dictionary<ReferenceFrames, ReferenceFrameFindingData[]> referenceFrameObjectData = new Dictionary<ReferenceFrames, ReferenceFrameFindingData[]>()
        {
            //{ReferenceFrames.Player, new ReferenceFrameFindingData[]{
            //new ReferenceFrameFindingData(){ ObjectSceneName= "Player_Body", RadiusOfInfluence = 75f} } },

            {ReferenceFrames.Sun, new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "Sun_Body", RadiusOfInfluence = 4000f} } },


	        {ReferenceFrames.Ash_Twin,  new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName = "Twin02_Body", RadiusOfInfluence = 350f} } },

	        {ReferenceFrames.Ember_Twin,  new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName = "Twin01_Body", RadiusOfInfluence = 350f} } },



            {ReferenceFrames.Timber_Hearth, new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "TimberHearth_Body", RadiusOfInfluence = 400f} } },

            {ReferenceFrames.Attlerock, new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "Moon_Body", RadiusOfInfluence = 200f} } },


            {ReferenceFrames.Brittle_Hollow, new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "BrittleHollow_Body", RadiusOfInfluence = 750f} } },

            {ReferenceFrames.Hollows_Lantern, new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "VolcanicMoon_Body", RadiusOfInfluence = 300f} } },

            {ReferenceFrames.White_Hole, new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "WhiteHole_Body", RadiusOfInfluence = 750f} } },


            {ReferenceFrames.Giants_Deep,new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "GiantsDeep_Body", RadiusOfInfluence = 2250f} } },

            {ReferenceFrames.Giants_Deep_Island_Gabbro,new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "ZenIsland_Body", RadiusOfInfluence = 150f} } },
            {ReferenceFrames.Giants_Deep_Island_Antenna,new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "TimeLoopIsland_Body", RadiusOfInfluence = 150f} } },
            {ReferenceFrames.Giants_Deep_Island_Teleporter,new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "TeleportIsland", RadiusOfInfluence = 150f} } },
            {ReferenceFrames.Giants_Deep_Island_Empty,new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "QuantumIsland_Body", RadiusOfInfluence = 150f} } },

            {ReferenceFrames.Quantum_Moon, new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "QuantumMoon_Body", RadiusOfInfluence = 350f} } },

            {ReferenceFrames.Interloper, new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "Comet_Body", RadiusOfInfluence = 300f} } },

            {ReferenceFrames.Dark_Bramble, new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "DarkBramble_Body", RadiusOfInfluence = 3000f} } },

            {ReferenceFrames.Dark_Bramble_Nodes, new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "DarkBramble_Body", RadiusOfInfluence = 3000f} } },

            {ReferenceFrames.Dark_Bramble_Feldspar_Node, new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "DonutAsteroid_Body", RadiusOfInfluence = 150f} } },

            {ReferenceFrames.Derelict_Node, new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "DerelictDimension_Body", RadiusOfInfluence = 600f} } },

            {ReferenceFrames.Stranger, new ReferenceFrameFindingData[]{ } },
            {ReferenceFrames.Dream_World, new ReferenceFrameFindingData[]{ } },
        };
    }

    public struct ReferenceFrameFindingData
    {
        public string ObjectSceneName;
        public float RadiusOfInfluence;
    }

    public class ReferenceFrameData
    {
        public Transform ReferenceFrameTransform;
        public float RadiusOfInfluence;
    }

    public enum ReferenceFrames : byte
    {
        Player,
        Sun,
	    Ash_Twin,
	    Ember_Twin,
        Timber_Hearth,
        Attlerock,
        Brittle_Hollow,
        Hollows_Lantern,
        White_Hole,

        Giants_Deep,
        Giants_Deep_Island_Gabbro,
        Giants_Deep_Island_Antenna,
        Giants_Deep_Island_Teleporter,
        Giants_Deep_Island_Empty,

        Quantum_Moon,
        Interloper,

        Dark_Bramble,
        Dark_Bramble_Nodes,
        Dark_Bramble_Feldspar_Node,
        Derelict_Node, // The vessel node in the alpha

        Stranger,
        Dream_World,

        AnyOWRigidbody, //For when the reference frame changes BUT there is no special object to reference on (like on eye scene and such)
        GlobalRoot //Uses unity's reference frame (for when the reference frame doesn't change, like in the main menu)
    }
}
