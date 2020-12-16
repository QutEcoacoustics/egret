# test suite 

A test suite is a named group of test files.
It will automatically run each test file against each tool.

# Structure


```yaml
label_aliases: []
tests: []
include_cases: []
tool_configs: {}
```

## Label aliases (`label_aliases`)

A list of strings which we'll consider equivalent when comparing results.

For example:

```yaml
label_aliases: ["GHFF", "Ghff", "Pteropus sp.", "Flying fox"]
```

### Example 
If a result has a `Name` field with the value `Pteropus sp.` and we have set up the following expectation:

```yaml
expect:
    - label: "Flying fox"
```

Then the expectation would normally fail. If we have `label_aliases` defined we know that 
`Pteropus sp.` is an acceptable substitute for `Flying fox` and we'll allow the expectation to pass.