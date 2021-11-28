using System.Collections.Generic;
using System.Collections;
using UnityEngine;

using SNet_Client.Utils;

namespace SNet_Client.EntityScripts.TransfromSync
{
    public class ClosestReferenceFrameLocator : MonoBehaviour
    {
        private DynamicReferenceTransformEntitySync dynamicReferenceTransformEntitySync;
        private DynamicReferenceRigidbodyEntitySync dynamicReferenceRigidbodyEntitySync;

        public float DeltaTimeForEachCheck = 0.5f;

        public void Start()
        {
            dynamicReferenceTransformEntitySync = gameObject.GetComponent<DynamicReferenceTransformEntitySync>();
            dynamicReferenceRigidbodyEntitySync = gameObject.GetComponent<DynamicReferenceRigidbodyEntitySync>();

            StartCoroutine("CheckForBestReferenceFrameLoop");
        }

        private ReferenceFrames GetClosestReferenceFrame(out Transform referenceFrameTransform)
        {
            Vector3 pos = transform.position;
            ReferenceFrames referenceFrame = ReferenceFrames.Sun;

            float smallestDistanceSqr = 0f;

            ReferenceFrameLocator.ForEach(CalculateClosestDistance);
            void CalculateClosestDistance(KeyValuePair<ReferenceFrames, ReferenceFrameData> pair)
            {
                float distanceSqr = (pos - pair.Value.ReferenceFrameTransform.position).sqrMagnitude;

                if (pair.Value.RadiusOfInfluence * pair.Value.RadiusOfInfluence >= distanceSqr && smallestDistanceSqr > distanceSqr)
                {
                    smallestDistanceSqr = distanceSqr;
                    referenceFrame = pair.Key;
                }
            }

            referenceFrameTransform = ReferenceFrameLocator.GetReferenceFrame(referenceFrame);
            return referenceFrame;
        }
        private IEnumerator CheckForBestReferenceFrameLoop()
        {
            while (true)
            {
                ReferenceFrames referenceFrame = GetClosestReferenceFrame(out Transform referenceFrameTransform);

                if (dynamicReferenceTransformEntitySync != null)
                    dynamicReferenceTransformEntitySync.ChangeReferenceFrame(referenceFrame, referenceFrameTransform);

                if (dynamicReferenceTransformEntitySync != null)
                    dynamicReferenceTransformEntitySync.ChangeReferenceFrame(referenceFrame, referenceFrameTransform);

                Debug.Log(string.Format("Best reference frame is {0}", referenceFrame));

                yield return new WaitForSeconds(DeltaTimeForEachCheck);
            }
        }
    }
}
