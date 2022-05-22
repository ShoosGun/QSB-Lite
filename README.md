# SNet
This mod allows for multiple players to play together in dedicated server the alpha 1.2 from Outer Wilds.
This is a continuation of the mod [Quantum-Space-Amigos(or QSA for short)](https://github.com/ShoosGun/Quantum-Space-Amigos) which was inspired by [QSB](https://github.com/misternebula/quantum-space-buddies). This continuation rewrote all the networking code from TCP to UDP, made the server code a separated executable and went from a server-client approach to a more peer-to-peer solution. These reasons are why this new repository was made.

### What is already done
* A framework that allows gameobjects (or entities) to have multiple scripts attached to it, which automates syncing of different types of data like:
    * Position, rotation and scale of objects;
    * State data like if the player is using the suit or if the flashlight is turned on.

### What is left to be done
* A lot.

### What is currently being worked on
* Animation for player states like walking, jumping, etc;
* Sync of particle animation;
* General bug fixes to the networking code.

Untill the animation is synced this project will probabilly not exit pre-release state.