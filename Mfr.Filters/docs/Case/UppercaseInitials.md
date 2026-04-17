# UppercaseInitials

Detects **initials-style** patterns: one or more single letters separated by dots (for example `u.s.a`, `d.j`, `e.x.a.m.p.l.e`). Only the letters inside those patterns are uppercased; the rest of the segment is left as-is.

No `options` object. Examples follow [`UppercaseInitialsFilterTests`](../../../Mfr.Tests/Models/Filters/Case/UppercaseInitialsFilterTests.cs).

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| (none) | `bruce springsteen - born in the u.s.a` | `bruce springsteen - born in the U.S.A` | |
| (none) | `live in the u.k and the u.s.a and e.u` | `live in the U.K and the U.S.A and E.U` | |
| (none) | `the u.s.a. is great` | `the U.S.A. is great` | |
| (none) | `alpha beta c and ab.cd stay as-is` | `alpha beta c and ab.cd stay as-is` | `ab.cd` is not letter-dot-letter initials. |
| (none) | `initials a.b and c.d.e` | `initials A.B and C.D.E` | |
