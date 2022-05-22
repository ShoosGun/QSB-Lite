using System;
using System.Threading;

using UnityEngine;
using SNet_Client.PacketCouriers.Entities;

namespace SNet_Client.Utils
{
    public static class Util
    {
        // Código de Cpp em C# 0_0
        public static int GerarHashInt(string s)
        {
            const int p = 53;
            const int m = 1000000000 + 9; //10e9 + 9
            int hash_value = 0;
            int p_pow = 1;
            foreach (char c in s)
            {
                hash_value = (hash_value + (c - 'a' + 1) * p_pow) % m;
                p_pow = p_pow * p % m;
            }
            return hash_value;
        }

        public static NetworkedEntity GetAttachedNetworkedEntity(this GameObject gameObject)
        {
            return gameObject.GetComponent<NetworkedEntity>();
        }
        public static NetworkedEntity GetAttachedNetworkedEntity(this Component component)
        {
            return component.GetComponent<NetworkedEntity>();
        }

        public static void SetLinearRotationCurve(this AnimationClip clip, string transformPath ,float initialTime, Quaternion initialRotation ,float finalTime, Quaternion finalRotation) 
        {
            clip.SetCurve(transformPath, typeof(Transform), "localRotation.w", AnimationCurve.Linear(initialTime, initialRotation.w,finalTime,finalRotation.w));
            clip.SetCurve(transformPath, typeof(Transform), "localRotation.x", AnimationCurve.Linear(initialTime, initialRotation.x, finalTime, finalRotation.x));
            clip.SetCurve(transformPath, typeof(Transform), "localRotation.y", AnimationCurve.Linear(initialTime, initialRotation.y, finalTime, finalRotation.y));
            clip.SetCurve(transformPath, typeof(Transform), "localRotation.z", AnimationCurve.Linear(initialTime, initialRotation.z, finalTime, finalRotation.z));
        }

        //Advindo de https://stackoverflow.com/questions/391621/compare-using-thread-sleep-and-timer-for-delayed-execution
        class TimerState
        {
            public Timer Timer;
        }

        public static void DelayedAction(int millisecond, Action action)
        {
            TimerState state = new TimerState();

            lock (state)
            {
                state.Timer = new Timer((callbackState) => {
                    action();
                    lock (callbackState) { ((TimerState)callbackState).Timer.Dispose(); }
                }, state, millisecond, -1);
            }
        }

        public static void RepeatDelayedAction(int initialDelay, int delay, Func<bool> func)
        {
            TimerState state = new TimerState();

            lock (state)
            {
                state.Timer = new Timer((callbackState) => {
                    if (!func())
                    {
                        lock (callbackState) { ((TimerState)callbackState).Timer.Dispose(); }
                    }
                }, state, initialDelay, delay);
            }
        }
    }
}
