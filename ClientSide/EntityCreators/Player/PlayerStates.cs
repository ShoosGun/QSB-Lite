namespace SNet_Client.EntityCreators.Player
{
    public enum PlayerStates : byte
    {
        SUIT_EQUIP,
        FLASHLIGHT,
        USING_TELESCOPE,
        ROASTING_MELLOWS,

        JUMPING,
        BOOST_THRUSTER,
        MOVING_OR_FLYING
    }
    public enum PlayerMovementStates : byte
    {
        MOVE_FOWARD = 1,
        MOVE_BACKWARDS = 2,
        MOVE_LEFT = 4,
        MOVE_RIGHT = 8,

        FLY_FOWARD = 16,
        FLY_BACKWARDS = 32,
        FLY_LEFT = 64,
        FLY_RIGHT = 128,
    }
}
