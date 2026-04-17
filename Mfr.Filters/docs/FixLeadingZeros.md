# FixLeadingZeros

Finds digit sequences in the text and normalizes **leading zeros** to a target width (for example track numbers). Options can limit how many numbers are changed, require **whole-word** numbers only, and optionally strip extra zeros before padding.

**Example**

- Width 2, many files: `09` stays two digits; `9` may become `09` depending on settings.

Use **FixLeadingZeros** so `1`, `01`, and `001` line up for sorting.
