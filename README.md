# IncursionHelper

A clean, customizable, all-in-one helper for Alva and the Temple of Atzoatl.

---

> **Important:** This plugin is an updated replacement for the old IncursionHelper from 3.26 and earlier.
>
> Updated for 3.28+ — `Splinter Research Lab` is now `Anomaly Research Lab`, tiers refreshed, and the overlay now reads the temple directly from the game so indices no longer break. If you had the old version, just replace it and enable this one.

---

## Features

* **Temple Overview at a Glance:** Every room in Alva's window gets a thin tier frame so you can see what matters without reading every tooltip.
    * S-tier — chase rooms (Corruption Locus, Doryani, Temple Nexus, Wealth) in bright magenta
    * A-tier — high-value farms (Apex, Glittering, Legion, Anomaly) in green
    * B/C-tier — solid and niche rooms in yellow/red
    * Untiered — filler corridors (Antechamber etc.) in grey

* **Architect Choice Helper:** When Alva offers two architects, the better pick is highlighted so you do not have to do math mid-map.

  <p align="center"><img width="483" height="377" alt="mini temple map with doorways and UP NEW badge" src="https://github.com/user-attachments/assets/51721d4e-fc70-440f-8bba-c1c1962c073f" /></p>

    * **Clear Badge:** Tiny `UP` / `NEW` label under the recommendation with the target name and tier — no wall of text.
    * **One-Line Reason:** Hover the recommended architect for a short explanation like `Upgrade to Sanctum of Vitality (S) - Alva tier 3 life` or `Change to Shrine (S)` — ASCII only so you never see `?` boxes.
    * **Scarcity + Timing Aware:** Favors a T3 that is not already on your map, leans to seeding new lines early and finishing upgrades late (uses `12 Incursions Remaining` from the window).

* **Door Connections Overlay:** Select any room and see its six walls drawn directly on the temple.

  <p align="center"><img width="574" height="394" alt="grid overlay with connection lines" src="https://github.com/user-attachments/assets/1fa87f04-cb6e-46bb-887c-d1dd83b7e1a4" /></p>

    * **Red = Locked:** That wall still needs a Stone of Passage.
    * **Green = Suggested:** The single best locked wall to open next, also framed on the little diamond preview and on the room itself with an `Open This Door` label.
    * **Clean by Design:** Already-open walls are not drawn, no screenshot parsing, no pixel checks — just the diamond tooltip data (`c3[13][0][3..8]`) the game already exposes.

* **Incursions Remaining:** Reads `12 Incursions Remaining` from the window and adjusts strategy — early incursions bias `Change`, late incursions bias `Upgrade`.

---

* **Fully Customizable Strategy:** Tailor the helper to your league goals. Everything lives in settings and updates live.

* **Strategy Presets:** One-click `Meta Profit`, `Seed Diversity`, and `Rush to T3` — each sets tier weights and per-room tiers just like MercScanner profiles.

* **Tweakable Scoring:** Seven weight sliders (`Tier multiplier`, `Upgrade bonus`, `Scarcity`, `Early Change`, `Late Upgrade`, `S-tier bonus`, `Untiered penalty`) so you can make S-tier dominate or play a more balanced seed game.

* **Per-Room Tiers:** A SimpleInformation-style searchable list — filter by tier (All/S/A/B/C/Untiered), search by name or T3 name, pick any tier from a color-coded combo. The list stays alphabetical so changing a tier does not shuffle it. Stats line at the bottom shows counts per tier.

---

* **Fully Customizable Display:** Keep it subtle or make it pop — your call.
    * Toggle tier frames, recommendation highlight, tiny badge, and door lines individually
    * Frame thickness sliders (1-5 for rooms, 1-6 for recommendations)
    * Pick any colors for S/A/B/C/Untiered, locked, and suggested
