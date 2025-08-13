<h1 align="center">GorillaHands</h1>
<p align="center">
  <strong>GorillaHands</strong> is a plugin/mod for <em>Gorilla Tag</em>.  
  It adds two large, human-like hands that the player can use to grab surfaces and move around the map.
</p>

<p align="center">
  <img src="https://raw.githubusercontent.com/CrafterBotOfficial/GorillaHands/refs/heads/main/Marketing/Thumbnail.png" alt="GorillaHands thumbnail" img width="auto" height="auto">
</p>

---

## Configuration

### Multipliers
| Setting              | Description |
|----------------------|-------------|
| `Arm Offset`         | Offset of the arm. |
| `Booster`            | The velocity multiplier for when you stop climbing. |
| `Follow Force`       | The force the hand uses to get to the target position. |
| `Damping Multiplier` | The multiplier controlling how strongly the follower slows down. |

### Collisions
| Setting                       | Description |
|--------------------------------|-------------|
| `Hand Collisions`              | Whether the hands can interact with surfaces. |
| `Spherecast Radius`            | Snap radius of the hand. |
| `Hand Stuck Distance Threshold`| How fare can the hand get before it will go through walls to return. (Avoids getting stuck in trees) |

### Controls
| Setting       | Description |
|---------------|-------------|
| `Toggle Hand` | Button used to toggle the hands on/off. |

### Misc
| Setting                | Description |
|------------------------|-------------|
| `Rotation Lerp Amount` | The speed that the hands will rotate to match the real player hands. |
| `Transition Speed`     | The speed the hands will appear/disappear when you toggle them |

---

## Development

### Building
1. Set an environment variable named `GORILLATAG_PATH` to match your Gorilla Tag install location.  
2. Build the project:

```bash
dotnet build -c Debug
# or
dotnet build -c Release
```

## Credits

- **[Crafterbot](https://github.com/CrafterBotOfficial)** - Creator & programmer.  
- **[cHin](https://github.com/Chin0303)** - Programmer & assets.  
- **[Lyneca](https://github.com/lyneca)** - Original concept inspiration from *[Mystic Hands](https://mod.io/g/blade-and-sorcery/m/mystic-hands-2#)*.    


## Legal
GorillaHands complies with the modding ruleset, but usage of GorillaHands is at own risk.

This product is not affiliated with Another Axiom Inc. or its videogames Gorilla Tag and Orion Drift and is not endorsed or otherwise sponsored by Another Axiom. Portions of the materials contained herein are property of Another Axiom. Â©2021 Another Axiom Inc. - https://www.anotheraxiom.com/fan-content-and-mod-policy
