# Disclaimer
Please keep in mind that the older versions are less supported and potentially even broken. Also, this is a work in progress mod (And also very new,) so it's going to have a lot of flaws. Hopefully, the code is functional enough to work, but I see no errors so far with the current system.
New updates often.

# License
https://choosealicense.com/licenses/gpl-3.0/

Since they decompile my code, I might as well make it public so that you can't misunderstand how it works, so for now here's the license I've chosen. Please respect it, and enjoy the rest of the mod!

# Premise:
In vanilla, you start with nothing. You have base stats, a singular goal, and to get to that goal, the challenge becomes more and more difficult until, eventually, either you stop playing or you lose the run.

I've found that the issue lies in loot being too easy to get early game and too hard to get late game, so I made a mod to affect player movement. I was intending, and still hope to achieve, the type being less limited in the future to just the amount of time you can sprint for, but...

To put it simply, there are two systems at play here- A simpler one that buffs (or nerfs) all players, and a vastly more complicated one that buffs (or nerfs) individual players.

You start off with normal stats, apart from the amount of time you can sprint, but the more you profit, the stronger of a buff you get.

These buffs affect the amount of time which you can sprint for, and they stack with the other buffs.
But you're probably wondering how to actually GET them.

There's two simple mechanics here that are used to determine the easier component.

If the ship recieved loot last round, it checks to see if it met a hidden quota. If it did, then all players gets buffed, and if it didn't, all players gets nerfed... If they didn't just sell, of course.
This buff requirement gets increased with each quota, going in increments of 4 with a minimum of 4, incrementing each quota. I'm considering increasing or decreasing it, something to do with dividing the current quota to get an actual number.
The second simple mechanic is using a similar mechanic, but with a higher number, being a minimum of 13 scrap daily. You don't get nerfed if you miss this one, though, you just don't get buffed. 

Both are applied at the end of the day, when you're given your performance report, though the code might not be the most optimal. I'm not too good at C# quite yet as my practice lies in modding Minecraft.

# Group Modifiers are...
* Applied to all players
* Less effective than Personal Modifiers

# Personal Modifiers are...
* Only applied in Multiplayer (Since you'd not have anyone to compete with otherwise)
* Applied on an individual scale
* More effective than Group Modifiers
* Giving a large nerf to the player who winds up being the laziest BUT
* Giving a large buff to the player who winds up being the most profitable AND
* Giving a decent buff to any other players without either of those two accolades, and I actually wrote my own code for that since the accolades don't actually check against all the players to see if they're the most profitable or the laziest.
