# Counter

Inserts a numeric counter based on each file’s index (global or per-folder). You can **prepend**, **append**, or **replace** the whole segment with the formatted number.

**Typical options**

- Start value, step, width, pad character  
- Whether the counter resets per folder  
- Separator when prepending or appending  

**Example**

- Replace mode, width 3, pad `0`, start 1: first file → `001`, second → `002`, …

Use **Counter** when you mainly need **sequential numbers** with padding and placement control. For templates mixing fixed text and metadata tokens, see [Formatter](Formatter.md).
