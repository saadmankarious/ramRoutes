using Platformer.Core;
using Platformer.Mechanics;
using Platformer.Model;
using UnityEngine;

namespace Platformer.Gameplay
{
    /// <summary>
    /// Fired when a player collides with a token.
    /// </summary>
    /// <typeparam name="PlayerCollision"></typeparam>
    public class PlayerTokenCollision : Simulation.Event<PlayerTokenCollision>
    {
        public PlayerController player;
        public TokenInstance token;

        // Custom event to notify listeners when a token is collected
        public static event System.Action<PlayerTokenCollision> OnEvent; // Custom event

        PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        public override void Execute()
        {
            Debug.Log("Coin collected");

            // Call the event and pass the instance of the current PlayerTokenCollision
            OnEvent?.Invoke(this);
            
            // Play the sound effect for coin collection
            AudioSource.PlayClipAtPoint(token.tokenCollectAudio, token.transform.position);
        }
    }
}
