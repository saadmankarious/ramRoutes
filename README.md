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



## Beta Release
### Improve Play UI
Added stats that track progress towards finishing the game including indicator of current trial and knowldge points. I also adde directions to remind player of mission objective.

### Spaceship
I finalized implementation of spaceship feature including animation and navigation. Upon hitting the E key, the spaceship finds the player location and moves towards it. The player gets picked up autmatically by the space ship before ascending to space. The spaceship then waits for player's inputs to move along the X axis.

## Project Organization
All assets for creating the game are included under the Assets folder. Scripts are organized under the Assets/Scripts subfolder. The game has three Scenes, LevelFall (containing all 4 trials), Landing, and Onboarding.

## Challenges
After pitching the game to a couple people, I realzied the objective of the game is not clear to the player at the beginning of the game. I consulted with my parter to see ways to fix this. We decided to enhance the UI and add extra elements to resolve this issue. 

We also worked on setting fixed objectives for each trial. For example to pass trial one, 20 pieces of trash need to be collected.

## Interesting Code
```csharp
private IEnumerator TypeText(string message, float activeFor)
 {
     dialogText.text = ""; // Clear previous text
     
     foreach (char letter in message.ToCharArray())
     {
         dialogText.text += letter; // Add one character at a time
         yield return new WaitForSeconds(typingSpeed); // Pause between letters
     }
     
     // Auto-hide after delay (if activeFor > 0)
     if (activeFor > 0)
     {
         yield return new WaitForSeconds(activeFor);
         HideDialog();
     }
 }
```
