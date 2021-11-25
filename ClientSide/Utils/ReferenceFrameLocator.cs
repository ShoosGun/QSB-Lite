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
                GameObject gameObject = GameObject.Find(referenceFrameObjectData[referenceFrame][i].ObjectSceneName);
                if (gameObject != null)
                {
                    referenceFrames[referenceFrame].ReferenceFrameTransform = gameObject.transform;
                    referenceFrames[referenceFrame].MaxDistanceOfInfluence = referenceFrameObjectData[referenceFrame][i].MaxDistanceOfInfluence;
                    referenceFrames[referenceFrame].MinDistanceOfInfluence = referenceFrameObjectData[referenceFrame][i].MinDistanceOfInfluence;
                    cachedTransform = gameObject.transform;
                    foundTheObject = true;
                }
            }
            return foundTheObject;
        }
        public static void CacheAllReferenceFrame()
        {
            foreach(var pair in referenceFrameObjectData)
            {
                if (pair.Key != ReferenceFrames.GlobalRoot && pair.Key != ReferenceFrames.AnyOWRigidbody)
                {
                    bool foundTheObject = false;
                    for (int i = 0; i < pair.Value.Length && !foundTheObject; i++)
                    {
                        GameObject gameObject = GameObject.Find(pair.Value[i].ObjectSceneName);
                        if (gameObject != null)
                        {
                            referenceFrames[pair.Key].ReferenceFrameTransform = gameObject.transform;
                            referenceFrames[pair.Key].MaxDistanceOfInfluence = pair.Value[i].MaxDistanceOfInfluence;
                            referenceFrames[pair.Key].MinDistanceOfInfluence = pair.Value[i].MinDistanceOfInfluence;

                            foundTheObject = true;
                        }
                    }
                }
            }
        }
        public static IEnumerator GetEnumerator()
        {
            return referenceFrames.GetEnumerator();
        }
        private static bool GetAnyOWRigidbody(out Transform reference)
        {
            reference = null;
            OWRigidbody rigidbody = (OWRigidbody)Object.FindObjectOfType(typeof(OWRigidbody));

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
                referenceFrames.Add(referenceFrame, null);

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

        //TODO colocar os dados aki
        private static readonly Dictionary<ReferenceFrames, ReferenceFrameFindingData[]> referenceFrameObjectData = new Dictionary<ReferenceFrames, ReferenceFrameFindingData[]>()
        {
            {ReferenceFrames.Player, new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "Player_Body", MaxDistanceOfInfluence = 10f, MinDistanceOfInfluence = 1f, } } },

            {ReferenceFrames.Sun, new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "Sun_Body", MaxDistanceOfInfluence = 10f, MinDistanceOfInfluence = 1f, } } },


            {ReferenceFrames.Hourglass_Twins,  new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName = "FocalBody" } } },


            {ReferenceFrames.Timber_Hearth, new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "TimberHearth_Body" } } },

            {ReferenceFrames.Attlerock, new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "Moon_Body" } } },


            {ReferenceFrames.Brittle_Hollow, new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "BrittleHollow_Body" } } },

            {ReferenceFrames.Hollows_Lantern, new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "VolcanicMoon_Body" } } },

            {ReferenceFrames.White_Hole, new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "WhiteHole_Body" } } },


            {ReferenceFrames.Giants_Deep,new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "GiantsDeep_Body" } } },

            {ReferenceFrames.Quantum_Moon, new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "QuantumMoon_Body" } } },

            {ReferenceFrames.Interloper, new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "Comet_Body" } } },

            {ReferenceFrames.Dark_Bramble, new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "DarkBramble_Body" } } },

            {ReferenceFrames.Dark_Bramble_Nodes, new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "DarkBramble_Body" } } },

            {ReferenceFrames.Derelict_Node, new ReferenceFrameFindingData[]{
            new ReferenceFrameFindingData(){ ObjectSceneName= "DerelictDimension_Body" } } },

            {ReferenceFrames.Stranger, new ReferenceFrameFindingData[]{ } },
            {ReferenceFrames.Dream_World, new ReferenceFrameFindingData[]{ } },
        };
    }

    public struct ReferenceFrameFindingData
    {
        public string ObjectSceneName;
        public float MinDistanceOfInfluence;
        public float MaxDistanceOfInfluence;
    }

    public class ReferenceFrameData
    {
        public Transform ReferenceFrameTransform;
        public float MinDistanceOfInfluence;
        public float MaxDistanceOfInfluence;
    }

    public enum ReferenceFrames : byte
    {
        Player,
        Sun,
        Hourglass_Twins,
        Timber_Hearth,
        Attlerock,
        Brittle_Hollow,
        Hollows_Lantern,
        White_Hole,
        Giants_Deep,
        Quantum_Moon,
        Interloper,

        Dark_Bramble,
        Dark_Bramble_Nodes,
        Derelict_Node, // The vessel node in the alpha

        Stranger,
        Dream_World,

        AnyOWRigidbody, //For when the reference frame changes BUT there is no special object to reference on (like on eye scene and such)
        GlobalRoot //Uses unity's reference frame (for when the reference frame doesn't change, like in the main menu)
    }
}
