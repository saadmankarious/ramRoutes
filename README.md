# Trials of Venus!

I aim to develop a 2D video game called *Trials Of Venus*, based on an adaptation of Greek mythology set at Cornell in the 21st century. The game aims to raise awareness about pressing environmental issues in a fun, mythical way.  

## Alpha Release
### Teleportation (Week 1)
This feature enables the player to travel instantly from one point in the level to another. We introduced this feature for two reasons: by the fourth level, the platform gets very long and it gets boring if the player walks from one end to the other and because it looks fun! 
There are three pieces into this feature:
1. The player walks into one of the four designated teleport station (positioned around Jupyter) and a message appears to prompt them to teleport
2. An animation that plays at the spawn and destination slot 
3. Shaking of the camera to create dynamic effect
   
### Level Transition (Week 2)
The whole game is on one unity Scene, one level essentially. However, this one level starts small but expands as the player advances through the four trials. To implement this transition and platform expansion, I extend the camera frame through swithcing the Cinemachineconfiner component while syncing this with the teleportation effect.

### Spaceship (Week 2)
We are currently working on implementing another feature that helps players navigate the game level easily. This is a spaceship concept that the player sits on thinly able to move from one end of the game to another. This feature is crucial because it expands the game from one dimensional platform to two dimensional. We finalized the design for the spaceship I still need to work on implementing the logic for it.

## Challenges
### Making player visible at end of level
One big challenge I faced this week was to stop Player movement at the end of the CineMachineConfiner (camera) to limit them from advancing to next level until they finish the task specified. I was able to make the player stop moving by detecting the interaction between the player sprite and the frame of the camera. However, the player will be invisible by the time they stop. I tried including a padding between the camera and the border of detection but it did not work. I also tried  
### Player can teleport from anywhere
I had the teleport feature configured so that when the player hits the T key, teleportation begins. However, this will happen even if the player was not at one of the designated teleport stations. To fix this, I created an enum and instantiated from it two objects to track where the player location and movement then overrided the methods that detect colision with the teleport stations:
```csharp
void OnTriggerEnter2D(Collider2D other)
{
    if(other.CompareTag("Player")){
        currentCollidedStation = teleportStation;
        ShowDialog("You are now on " + teleportStation + ". Hit T to time travel!", other);
    }
}

void OnTriggerExit2D(Collider2D other)
{
    if(other.CompareTag("Player")){
        currentCollidedStation = null;
        HideDialog();
    }
}
```


## Interesting Code
### Teleportation
```csharp
private IEnumerator TeleportWithDelay()
{
    int switchIndex = GetNextConfinerIndex();
    switchConfiner.SwitchToConfiner(switchIndex);

    // Play teleport effect (particles) at the current location
    if (teleportEffect != null)
    {
        Instantiate(teleportEffect, player.transform.position, Quaternion.identity);
    }

    // Play teleport sound
    if (teleportSound != null)
    {
        AudioSource.PlayClipAtPoint(teleportSound, player.transform.position);
    }

    // Shake the camera using Cinemachine Impulse
    if (impulseSource != null)
    {
        impulseSource.GenerateImpulse();
        Debug.Log("generated impulse");
    }

    // Wait for the specified delay
    yield return new WaitForSeconds(teleportDelay);

    // Teleport the player to the target location
    player.transform.position = targetLocation.position;

    // Play teleport effect at the new location
    if (teleportEffect != null)
    {
        Instantiate(teleportEffect, targetLocation.position, Quaternion.identity);
    }
}

```
### Confiner Switching
```csharp
if (index >= 0 && index < confiners.Length && confiners[index] != null)
{
   Debug.Log("Switching confiner to index: " + index + " (" + confiners[index].name + ")");

   // Assign the new Collider2D to the CinemachineConfiner's Bounding Shape 2D
   confiner.m_BoundingShape2D = confiners[index];

   // Refresh the Cinemachine Confiner
   confiner.InvalidatePathCache();

   // Update the current confiner index
   currentConfinerIndex = index;
}
else
{
   Debug.LogWarning("Invalid confiner index or confiner is null!");
}
```
### Stop player at edge of level
```csharp
if (boundary.OverlapPoint(newPosition))
{
    move.x = inputX; // Allow movement
    Debug.Log("overlaping now");
}
else
{
    // Optional: Try sliding along walls
    Vector2 edgePosition = boundary.ClosestPoint(newPosition);
    if (Mathf.Abs(edgePosition.x - transform.position.x) > 0.05f)
    {
        move.x = inputX * 0.3f; // Reduced speed when sliding
    }
    else
    {
        move.x = 0; // Stop completely when directly against wall
    }
}
```
