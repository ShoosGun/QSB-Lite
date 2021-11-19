using System;
using System.Threading;

namespace SNet_Server.Utils
{
    public static class Util
    {
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
    }
}

