using System.Collections.Generic;

using UnityEngine;

namespace SNet_Client.Utils
{
    public static class ReferenceFrameLocator
    {
        private static Dictionary<ReferenceFrames, Transform> referenceFrames = new Dictionary<ReferenceFrames, Transform>();

        private static bool CacheReferenceFrame(ReferenceFrames referenceFrame, out Transform cachedTransform)
        {            
            bool foundTheObject = false;
            cachedTransform = null;

            if (referenceFrame == ReferenceFrames.GlobalRoot)
                return false;

            for (int i =0; i< referenceFrameObjectNames[referenceFrame].Length && !foundTheObject; i++)
            {
                GameObject gameObject = GameObject.Find(referenceFrameObjectNames[referenceFrame][i]);
                if (gameObject != null)
                {
                    referenceFrames[referenceFrame] = gameObject.transform;
                    cachedTransform = gameObject.transform;
                    foundTheObject = true;
                }
            }
            return foundTheObject;
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

            if (!referenceFrames.ContainsKey(referenceFrame))
                referenceFrames.Add(referenceFrame, null);

            Transform reference = referenceFrames[referenceFrame];

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

        private static readonly Dictionary<ReferenceFrames, string[]> referenceFrameObjectNames = new Dictionary<ReferenceFrames, string[]>()
        {
            {ReferenceFrames.Player, new string[]{"Player_Body"} },
            {ReferenceFrames.Sun, new string[]{ "Sun_Body" } },

            {ReferenceFrames.Hourglass_Twins, new string[]{ "FocalBody" } },

            {ReferenceFrames.Timber_Hearth, new string[]{ "TimberHearth_Body" } },
            {ReferenceFrames.Attlerock, new string[]{ "Moon_Body" } },

            {ReferenceFrames.Brittle_Hollow, new string[]{ "BrittleHollow_Body" } },
            {ReferenceFrames.Hollows_Lantern, new string[]{ "VolcanicMoon_Body" } },
            {ReferenceFrames.White_Hole, new string[]{ "WhiteHole_Body" } },

            {ReferenceFrames.Giants_Deep, new string[]{ "GiantsDeep_Body" } },

            {ReferenceFrames.Quantum_Moon, new string[]{ "QuantumMoon_Body" } },

            {ReferenceFrames.Interloper, new string[]{ "Comet_Body" } },

            {ReferenceFrames.Dark_Bramble, new string[]{ "DarkBramble_Body" } },
            {ReferenceFrames.Dark_Bramble_Nodes, new string[]{ "DarkBramble_Body" } },
            {ReferenceFrames.Derelict_Node, new string[]{ "DerelictDimension_Body" } },

            {ReferenceFrames.Stranger, new string[]{ } },
            {ReferenceFrames.Dream_World, new string[]{ } },
        };
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
