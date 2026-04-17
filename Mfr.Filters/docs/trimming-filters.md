# Trimming filters

Cut or extract parts of the target segment by position, or collapse repeated characters.

Positions in **TrimBetween** are **1-based** and **inclusive**; each endpoint can be anchored from the **left** or **right** end of the string.

---

## TrimLeft

Removes a fixed **count** of characters from the **start** of the segment.

**Example**

- Count `3`: `ABCDEF` → `DEF`

---

## TrimRight

Removes a fixed **count** of characters from the **end** of the segment.

**Example**

- Count `2`: `ABCDEF` → `ABCD`

---

## ExtractLeft

Keeps only the **first** N characters (drops the rest).

**Example**

- Count `4`: `ABCDEF` → `ABCD`

---

## ExtractRight

Keeps only the **last** N characters.

**Example**

- Count `3`: `ABCDEF` → `DEF`

---

## TrimBetween

Deletes the inclusive range from **start position** through **end position** (each with left or right anchor). If the start lies after the end, the range is **swapped** so the removal still makes sense.

**Example**

- Remove characters 3–5 from the left: `ABCDEF` → `ABF` (exact result depends on anchors and length).

---

## ShrinkDuplicateCharacters

Collapses **adjacent** repeats of one chosen character to a **single** copy (regex `+` style).

**Example**

- Character `-`: `a---b` → `a-b`

---

### Tips

- Use **Extract*** when you want a **short prefix or suffix**; use **Trim*** when you want to **drop** a known number of characters from an edge.
- **ShrinkDuplicateCharacters** is useful for repeated dashes or dots after other cleanup.
