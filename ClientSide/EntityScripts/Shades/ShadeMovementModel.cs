//using System;
//using System.Collections.Generic;
//using System.Text;
//using UnityEngine;
//using DIMOWAModLoader;

//namespace ClientSide.EntityScripts.Shades
//{
//    public class ShadeMovementModel : CharacterMovementModel
//    {
//        private ClientDebuggerSide debugger;

//        public MovementPacket CurrentMovementPacket;

//        private void Start()
//        {
//            debugger = GameObject.Find("DIMOWALevelLoaderHandler").GetComponent<ClientDebuggerSide>();

//            _jumpSpeed = 6f;
//            _turnRate = 160f;

//        }
//        protected override Vector3 GetMoveInput()
//        {
//            return CurrentMovementPacket.MoveInput; // x = andar de lado z = andar para frente e para tras
//        }

//        protected override float GetTurnInput()
//        {
//            return CurrentMovementPacket.TurnInput;
//        }

//        protected override bool GetJumpInput()
//        {
//           return CurrentMovementPacket.JumpInput;
//        }

//        public void SetNewPacket(MovementPacket packet)
//        {
//            CurrentMovementPacket = packet;
//        }
//    }
//    public struct MovementPacket
//    {
//        public Vector3 MoveInput;
//        public float TurnInput;
//        public bool JumpInput;
//        public DateTime SendTime;

//        public MovementPacket(Vector3 moveInput, float turnInput, bool jumpInput, DateTime sendTime)
//        {
//            MoveInput = moveInput;
//            TurnInput = turnInput;
//            JumpInput = jumpInput;
//            SendTime = sendTime;
//        }
//    }
//}
