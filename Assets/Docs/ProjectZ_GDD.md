# Project Z — Game Design Document

**Genre:** Co-op action-puzzle survival  
**Players:** 4 (Photon Fusion 2, Shared Mode)  
**Theme:** Computer science and cybersecurity. The squad is an incident-response team. The enemies are virus zombies: malware that has forced corrupted processes into bodies.  
**Status:** Vision locked. Architecture setup is in progress.

Match score is a post-match record. It does not buy weapons, ammo, or doors.

---

## 1. Fantasy and match loop

A quarantined building is executing hostile code. Doors, terminals, and servers are still online. The squad shoots its way to a station, one player operates it, and the others keep viruses off that operator. Solving the station seals part of the outbreak and opens the next wing. The match ends when the final station is sealed, or when every player is down at the same time.

```
Lobby → pick level and class → deploy
  → fight to a station → one player solves it, the others defend
  → repeat for that level's stations
  → seal the incident → MVP board → lobby
```

Each level uses **2 or 3 stations** drawn from the five-minigame catalog. A level never asks the squad to solve all five. The catalog is what makes levels feel different.

**Session rules**

- One operator per station. Anyone can step up. The operator cannot shoot until they close the panel or the station resolves.
- Everyone else can shoot, revive, and use skills during the minigame.
- Virus **Worms** prefer the active station. Basic viruses chase the nearest player. This is what makes defending a job.
- The squad is four different roles. A class cannot be selected twice.
- Anyone of any class can operate any station.
- At 0 HP a player is downed and bleeding out. A teammate revives them in place. If every player is down at the same time, the match is over.
- Friendly fire is off.
- A wipe still opens the MVP board, stamped as a failed containment.

**Starting combat, shared by every class**

- Sidearm, hitscan, short magazine, manual reload.
- Downed at 0 HP. A teammate revives over a short channel. Bleed-out is about 20 seconds.
- No wall-buys and no point doors. Weapons stay with the class. Space opens because a station was solved.

---

## 2. The four classes

The four roles cover the four jobs in a live incident: break in, keep people up, shape the room, and read the attack. Puzzle help is real and small. A missing class never makes a station impossible.

| Class | Job in the fight | Job at the station |
|---|---|---|
| Hacker | Short crash on a packed group | Forgives the first mistake |
| SysAdmin | Healing and faster revives | Can patch the operator without pulling them off the keyboard |
| Network Engineer | Place a wall the viruses cannot cross | Ghosts the next correct step for a nearby operator |
| Security Analyst | Show the flank and slow it | Names the exact check that failed |

### 2.1 Hacker

**Lore.** A red-team operator who stayed after the contract ended. Treats every locked host as a door someone already left open. Talks less than the SysAdmin and moves first.

**Passive — Exploit Familiarity.** When the Hacker is the operator, the first wrong submission on that station does not raise the alarm and does not wipe partial progress. Later mistakes do. If someone else is operating, this passive stays unused.

**Active — Kernel Panic.** Cooldown 18 seconds. A short cone about 6 meters long. Viruses in the cone freeze for 2.5 seconds and take increased damage for that same window. Players are unaffected. This is the "crash the process" button: it buys a reload, it does not clear a wing.

### 2.2 SysAdmin

**Lore.** Kept the campus servers alive through every bad deploy. Treats teammates as machines that are not allowed to go down. Carries tape, a headset, and strong opinions about shutdown order.

**Passive — Failover.** Revives performed by the SysAdmin take about half as long, and the revived player stands up at half health. Anyone can still revive. The SysAdmin is simply the reliable one.

**Active — Hotfix.** Cooldown 14 seconds. Channel about 1.5 seconds on an ally within 8 meters. Restores a large slice of health and clears a slow. Casting it on the operator does not cancel the minigame. Casting it on a downed ally begins the revive. This is debugging a person.

### 2.3 Network Engineer

**Lore.** Builds the paths the rest of the squad actually uses. If a route is not drawn, it does not exist. Thinks in racks, ports, and which hallway is about to become a bottleneck.

**Passive — Priority Queue.** While the Engineer stands near an operator (about 6 meters), the station ghosts the next correct step for roughly a second at a time: the next gate, the next service, the next allow/deny, the next command token. Allies near the Engineer also reload slightly faster. The ghost is a hint, not an auto-solve.

**Active — Drop Firewall.** Two charges. Each charge returns after 20 seconds. Places a wall about 4 meters wide that lasts 8 seconds. Viruses cannot walk through it. Players and shots can. Only one wall exists at a time; placing another replaces the first. This is how the Engineer holds a doorway while the Hacker types.

### 2.4 Security Analyst

**Lore.** Watches logs until the attack has a shape. Speaks in signatures and timestamps. Often the last person to shoot and the first person to call which hallway is about to break.

**Passive — Signature Match.** Virus cores are visible to the whole squad while the Analyst is alive and reasonably close. When a station check fails and the Analyst is near that station, the panel names the failed piece ("bit 4", "gate B", "service AUTH", "packet 3") instead of a generic reject.

**Active — Quarantine.** Cooldown 16 seconds. A pulse about 8 meters around the Analyst. Viruses inside are outlined through walls for 5 seconds and move slower for 4 seconds. This does not freeze them. Freezing belongs to Kernel Panic. Quarantine is how the squad sees the Worms coming around the operator.

---

## 3. Virus pressure

Three enemy behaviors are enough for the first levels.

| Virus | Behavior | Why it exists |
|---|---|---|
| Corrupt | Walks at the nearest player and melee | The crowd |
| Worm | Runs at the active station, ignoring closer players | Forces someone to watch the operator |
| Daemon | Stays at range and spits a slow projectile | Punishes a squad that stacks on one doorway |

Daemons are the Analyst's problem to mark and the Hacker's problem to crash. Worms are what the Firewall is for. Corrupts fill the space between.

A failed station check spawns a short burst, usually two Corrupts and one Worm, at that station. The station stays unsolved. Progress inside the minigame follows the rules in section 4.

---

## 4. Five minigames

Shared rules:

- The station shows a one-line goal before the panel opens.
- Target clear time is 20 to 40 seconds once the player understands the puzzle. The first time will be slower.
- Input is the mouse and a few keys. Nobody has to be a programmer.
- Closing the panel early keeps whatever progress the specific puzzle says it keeps, and the operator can shoot again.
- Class help never fills in the whole answer.

### 4.1 Logic Gate Patch

**Fantasy.** A door controller is stuck on the wrong output. The operator rebuilds a three-gate circuit so the lamp matches the spec.

**Setup.** Two inputs, A and B, are shown as 0 or 1. A truth table of four rows is the spec (the desired output for every pair). Three sockets sit between the inputs and the lamp. The operator assigns each socket AND, OR, or NOT.

**Solve.** All four rows match the spec, then the operator submits.

**Fail.** The lamp flashes the first wrong row. Partial gate placement is kept. The alarm spawns a burst. Signature Match names the socket that broke the row. Priority Queue briefly ghosts the next socket.

**Level use.** Opens a physically shut door. Best early station, because the rule is visible on the board.

### 4.2 Binary Beacon

**Fantasy.** A distress beacon is transmitting a password in binary. The operator decodes it before the next wing will accept a login.

**Setup.** Two or three groups of 8 bits, plus a one-line key: "read as letters" or "flip every bit, then read as letters." A keypad enters a short word, two to four letters. The bits stay on screen next to the keypad.

**Solve.** The word matches. Each beacon is its own submit. Solved beacons stay solved if a later one is wrong.

**Fail.** That beacon reshuffles. Solved beacons remain. The alarm spawns a burst only on every second failed beacon, so a single typo is not a wipe. Signature Match highlights the first wrong chunk of four bits.

**Level use.** A login on a sealed server room. Quiet, readable, and easy to defend because the operator is staring at one panel.

### 4.3 Dependency Reboot

**Fantasy.** The host will not seal until its services stop in a safe order. Stopping a service while something still needs it crashes the shutdown.

**Setup.** Five named services: APP, AUTH, DB, DNS, BACKUP. Arrows on the board show who depends on whom. The safe rule is on the panel in plain words: stop a service only when nothing still running depends on it. A typical board is "APP needs AUTH and DB. AUTH needs DNS. BACKUP is watching the others, so it is last."

**Solve.** The operator clicks the five services in a valid leaf-to-root order. Example for that typical board: APP, then AUTH and DB in either order, then DNS, then BACKUP.

**Fail.** The illegal click is rejected and that click does not consume the service. The first illegal click is free if the Hacker is operating. Any later illegal click spawns a burst. The correct clicks already made stay locked in.

**Level use.** The mid-level "we have to take the server down cleanly" beat. It is the SysAdmin's fantasy even when the SysAdmin is not the one clicking.

### 4.4 ACL Builder

**Fantasy.** Packets are queued at a dead firewall. The operator applies the posted policy before the queue overflows into the room.

**Setup.** Four packets, each with a source address, a port, and a protocol. A policy of three lines is pinned above them, written in normal sentences. Example policy: "Deny port 23. Allow addresses that start with 10 to port 443. Deny everything else." Each packet has an ALLOW and a DENY toggle. The operator sets all four, then submits once.

**Solve.** Every toggle matches the policy.

**Fail.** Wrong packets are marked and stay wrong until changed. Correct packets lock. Each wrong packet in the submission spawns one Corrupt. Priority Queue pre-fills one packet. Signature Match names the packet number, not the right answer.

**Level use.** A hallway checkpoint. Slightly meaner than the logic gate because the whole queue submits together. The policy text is always on screen so the puzzle is attention, not trivia.

### 4.5 Shell Containment

**Fantasy.** The last host still accepts a command line. A paper runbook lists the containment commands. The operator types them.

**Setup.** Three commands, shown in order on the runbook beside the prompt. They are short and fixed for that station, for example `isolate eth0`, `kill 4412`, `seal vault`. The operator types the next command and submits. The terminal shows the usual reject line on a mismatch. Case does not matter. Extra spaces do not matter.

**Solve.** All three commands accepted in order.

**Fail.** A mismatch does not advance the list. Every third mismatch on the same command spawns a burst. Earlier commands stay accepted. Exploit Familiarity covers the first mismatch on the station. Priority Queue ghosts the first word of the current command for a moment. The Hacker does not get the whole line filled in.

**Level use.** The final seal of a level. It is the loud one: Worms arrive on a timer while the operator types, even before any mistake. Defending here is the match.

---

## 5. Scoring and the MVP board

Points exist so the board can name what happened. They are not spent inside the level.

### 5.1 Point values

Starting values, meant to be tuned after a few playtests:

| Event | Points | Notes |
|---|---|---|
| Elimination | 10 | Any virus killed by that player |
| Defense kill | +5 on top of the 10 | The kill happened while a station panel was open |
| Station solved | 150 | Awarded to the operator who submitted the solving input |
| Flawless station | +50 | Awarded to that same operator if the station never raised an alarm |
| Revive | 75 | The player who finished the revive |

Skills do not award points. A firewall block, a quarantine slow, a kernel panic, and a hotfix are visible in the fight, and they stay off the board.

Defense kills, puzzle solves, and revives are intentionally the heavy numbers. A player who only shoots in the hallway cannot outscore a player who sealed two stations unless the defense kills were constant and the puzzles were few.

### 5.2 What the board shows

One row per player, in squad order:

- Name and class
- Total eliminations
- Defense kills
- Stations solved
- Flawless stations
- Revives
- Total score

The header shows the level name, the elapsed time, and either **CONTAINED** or **LOST**.

### 5.3 Badges

Badges sit on the row. More than one player can share a badge on a tie. A player can wear more than one.

| Badge | Who gets it |
|---|---|
| MVP | Highest total score. Tie breaks by stations solved, then revives, then eliminations |
| Root | Most stations solved |
| Silent Alarm | Most flawless stations. Hidden if nobody was flawless |
| Containment | Most defense kills |
| Uptime | Most revives. Hidden if nobody revived anyone |

MVP is the only badge called out in the title of the screen. The others are labels on the rows, so a Hacker who typed the seal and a SysAdmin who picked up two revives are both visible.

### 5.4 Worked example

A contained level with three stations. Scores use only the five tracked stats.

- Hacker operates all three stations, one of them flawless, and kills 8 viruses of which 4 were defense kills. Score: (3 × 150) + 50 + (8 × 10) + (4 × 5) = **600**.
- SysAdmin revives twice and kills 6 viruses. Score: (2 × 75) + (6 × 10) = **210**.
- Engineer kills 10 viruses of which 7 were defense kills. Score: (10 × 10) + (7 × 5) = **135**.
- Analyst kills 9 viruses of which 6 were defense kills. Score: (9 × 10) + (6 × 5) = **120**.

The Hacker is MVP and Root. If the SysAdmin is the only one who revived, they take Uptime. Containment goes to whoever led defense kills. Walls, scans, and heals stay in the match and off this table.

---

## 6. How a level is supposed to feel

1. The squad picks a class in the lobby. The level card names its stations in theme language ("beacon", "reboot", "seal"), not in puzzle-mechanic language.
2. The first wing is a short fight into Logic Gate Patch or Binary Beacon. The teacher station. Alarm on failure is gentle.
3. The middle wing is Dependency Reboot or ACL Builder. Worms start preferring the station.
4. The last wing is Shell Containment, or ACL Builder if the shell was used earlier in the night. Worms spawn on a timer while the panel is open.
5. The seal plays, the board comes up, and the squad returns to the lobby with classes still selected so a rematch is one click.

Five levels can reuse this shape and swap which station sits in which wing. Identity comes from the room and from which two puzzles are paired, not from a new ruleset every time.

---

## 7. Locked decisions

1. **One role each.** Four players, four different classes. The lobby refuses a second pick of the same role.
2. **Open stations.** Any class can operate any station. Class passives still apply only when that role is the operator or is standing in range, as written in section 2.
3. **Downed, then game over.** A downed player bleeds out and can be revived by a teammate. Game over happens only when every player is down at the same time.
4. **Clean board.** Score is eliminations, defense kills, stations solved, flawless stations, and revives. Ability micro-points are not tracked.
