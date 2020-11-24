# Values in expectations

The real world is messy. Egret supports various ways for an expectation to
match a range of values.

For the related topic on matching property names / column names please see
the [munging](./munging.md) article.


## Strings:

Strings (text, names, labels) are have two options. They can either be:


 - a normal string: `hello`, or `"hello"` (quoted) if special characters are used
    - must match exactly, other than by case (they are case-insensitive)
 - or a regular expression: `/bo+/`
    - denoted by a forward slash at the start, and one at the end

 Regular expressions allow us to match wildcards (and many other complex patterns).
 An example:

 `/^boo.*/` means _match any text that starts with boo_ and will match
- `boobook`
- `boobird`
- `booted eagle`
- `booby`
 
 A discussion on regular expressions are outside the scope of this article. 
 See this guide for more help: <https://regexone.com/>.

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

 A formal grammar for this syntax is defined [interval.ebnf](./interval.ebnf) and is shown below.

 However, the easiest method of working with this notation is to just look through
 the examples.

 ### expression

![expression](./media/interval/expression.svg)

References: [number](#number), [tolerance](#tolerance), [relation](#relation), [interval](#interval), [approximation](#approximation)

### tolerance

![tolerance](./media/interval/tolerance.svg)

Used by: [expression](#expression)
References: [number](#number)

Examples: 
- `1.23±0.2`  within 0.2 of 1.23
- `1.23±ε`  ➡ within epsilon (the smallest relevant value) of 1.23

### relation

![relation](./media/interval/relation.svg)

Used by: [expression](#expression)
References: [number](#number)

Examples: 
- `>1.23` ➡ greater than 1.23
- `<1.23` ➡ less than 1.23
- `≥1.23` ➡ greater or equal to 1.23
- `≤1.23` ➡ less or equal to 1.23


### approximation

![approximation](./media/interval/approximation.svg)

Used by: [expression](#expression)
References: [number](#number)

Examples: 
- `~1.23` ➡ loosely approximate (within an order of magnitude) to 1.23  ➡ `(0.123, 12.3)`
    - useful for logarithmic values
- `≈1.23` ➡ approximately equal to 1.23. Defined as within `5%` of target value
    - `≈1.23` ➡ `(1.16, 1.29)`
    - `≈11025` ➡ `(10473, 11576)`


### interval

![interval](./media/interval/interval.svg)

Used by: [expression](#expression)
References: [number](#number)

Examples: 
- `[1.2, 1.3]` ➡ a value that lies between `1.2` and `1.3`, **including** endpoints ➡  `1.2 ≤ x ≤ 1.3`
- `[1.2, 1.3)` ➡ a value that lies between `1.2` and `1.3`, **including** min, **excluding** max ➡  `1.2 ≤ x < 1.3`
- `(1.2, 1.3]` ➡ a value that lies between `1.2` and `1.3`, **excluding** min, **including** max ➡  `1.2 < x ≤ 1.3`
- `(1.2, 1.3)` ➡ a value that lies between `1.2` and `1.3`, **excluding** endpoints ➡  `1.2 < x < 1.3`


### number

![number](./media/interval/number.svg)

Used by: [expression](#expression), [tolerance](#tolerance), [relation](#relation), [approximation](#approximation), [interval](#interval)
References: [constant](#constant), [real](#real)

Any number, including our constants:

Examples: 
- `-1.23` 
- `1.23`
- `0.0`
- `∞`


### constant

![constant](./media/interval/constant.svg)

Used by: [number](#number)

Examples: 
- `ε` ➡ the smallest representable value. This value is
    - `0.001` currently
    - variable in the app configuration
    - used to ignore floating point or rounding errors
    - can be used as a value itself, or more commonly as a threshold
    - think of it as _computers are weird, it is close enough_
- `∞` ➡ Positive infinity
- `-∞` ➡ Negative infinity


### real

![real](./media/interval/real.svg)

Used by: [number](#number)
References: [pos](#pos), [neg](#neg), [zero](#zero)

### pos

![pos](./media/interval/pos.svg)

Used by: [real](#real), [neg](#neg)
References: [digit](#digit)

### neg

![neg](./media/interval/neg.svg)

Used by: [real](#real)
References: [pos](#pos)

### digit

![digit](./media/interval/digit.svg)

Used by: [pos](#pos)



