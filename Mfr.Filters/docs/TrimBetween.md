# TrimBetween

Deletes the inclusive range from **start position** through **end position** (each with left or right anchor). Positions are **1-based** and **inclusive**; each endpoint can be anchored from the **left** or **right** end of the string. If the start lies after the end, the range is **swapped** so the removal still makes sense.

**Example**

- Remove characters 3–5 from the left: `ABCDEF` → `ABF` (exact result depends on anchors and length).
