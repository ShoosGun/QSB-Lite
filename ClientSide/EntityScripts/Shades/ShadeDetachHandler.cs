using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ClientSide.EntityScripts.Shades
{
    public class ShadeDetachHandler : MonoBehaviour
    {
        private void Start()
        {
            GlobalMessenger.AddListener("DetachPlayerFromPoint", new Callback(OnDetachPlayerFromPoint));
        }
        private void OnDestroy()
        {
            GlobalMessenger.RemoveListener("DetachPlayerFromPoint", new Callback(OnDetachPlayerFromPoint));
        }
        private void OnDetachPlayerFromPoint()
        {
            Transform playerT = OWUtilities.GetTaggedComponent<Transform>(gameObject, "Player");
            gameObject.transform.rotation = playerT.rotation;

            Collider taggedComponent = playerT.gameObject.collider;
            bool enabled = taggedComponent.enabled;

            if (enabled && collider.enabled)
            {
                Physics.IgnoreCollision(collider, taggedComponent);
            }
        }
    }
}
