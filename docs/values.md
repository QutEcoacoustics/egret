# Values in expectations

## Numerics:
 In general any numeric field on a test, as well as all coordinates for events
 and segments
 will accept a variety of syntaxes, including:
 - literal values: `1.0`, `1`, `-1.23`, `0.0023`
 - exponential notation: `4.56e3`, `-1E-6`
 - constants: `ε` , `∞` , `-∞`
 - inequality relations: `>`, `<`, `≥`, `≤`
 - tolerances: `±`
 - approximations: `~`, `≈`
 - intervals: `[x1, x2]`, `(x1, x2]`, `[x1, x2)`, `(x1, x2)`

 This syntax allows for a wide range of concepts to be encoded in a dense format.
 We think this density makes large test-sets easy to work with.

 A formal grammar for this syntax id defined interval.ebnf and can be visualized [here]().

 However, the easiest method of working with this notation is to just look through
 the examples.


## Strings:
 Strings (text, names, labels) are much simpler. They can either be
 - a normal string: `hello`, or `"hello"` (quoted) if special characters are used
 - or a regular expression: `/bo+/`

 Regular expressions are 