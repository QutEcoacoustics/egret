# Expectations

Expectations are a statement about what results we expect a tool to produce.

Expectations allow you to:

- check coordinates based on bounding box, centroid, or temporal span
- check event duration, bandwidth, or label
- check meta features like event count, check for no events, or check for no extra events


## Expectation types

There are two expectation types, `event` tests and `segment` test. 

All expectations can be given a `name` and can be negated via the match property:

```yaml
expect:
    - name: "a name for a test"
      match:  false  
```

Both `name` and `match` can be omitted (and usually are).

```yaml
expect:
    - 
```

`match`'s default value is `true`. Use `match: false` to negate whatever test 
you're doing.

This will ensure we find a _a noisy airplane_:

```yaml
expect:
    - label: a noisy airplane
```

And now our test will fail if we find a _a noisy airplane_:

```yaml
expect:
    - match:  false  
      label: a noisy airplane
```

 ## Segment tests

Segment tests look for patterns in the results.
If you want to find a particular event, use an [event test](#event-tests) instead.

Segment tests that are available:

### No Events: `no_events`

No Events simply checks that a given test produced no events.

It only makes sense to use this expectation by itself.

```yaml
expect:
    - segment_with: no_events
```

### Events count: `event_count`

Event Count checks that a given test produced at least `count` events.

You can combine this expectation with other expectations.

```yaml
expect:
    - segment_with: event_count
      count: 3
```

### No Other Events: `no_other_events`

No other events ensures that after any _event expectations_ have been checked,
that there are no extra events remaining.

This expectation **should** be combined with other expectations



```yaml
expect:
    - segment_with: no_other_events
    - segment_with: no_other_events
```    

Using `no_other_events` by itself would have the same affect as the
`no_events` expectation.

```yaml
expect:
    - segment_with: no_other_events
```    

## Event tests

Event tests look for distinct results. We call them events because that's usually
what an acoustic recognizer produces. They don't _have_ to be acoustic events though.

Rather, an event should be:
  - an individual result
  - withing the analyzed segment
  - that can be located by coordinates
  - an ideally has some kind of _label_ or classification

There are three way to match an event: `bounds`, `centroid`, and by `time`.
At least one set of coordinates must be used.
Only one set of coordinates can be used at a time.

### Common properties

Each of the `bounds`, `centroid`, and by `time` expectations can include any of 
the following properties:

- `label` - a textual label/tag/classification of the result
- `duration` - check the duration of an event
- `bandwidth` - check the bandwidth of an event
- `index` - the expected index in the results file
- 🚧`condition` - check an arbitrary condition🚧

All of these properties may be omitted. If they are then Egret does not check them.
Omit them unless you need to check for them specifically.

Example:

```yaml
expect:
  - label: boobook
    duration: ">15.0"
    bandwidth: "<1000"
    time: [ ">0", "<30" ]
```

This above example means: 

> Find a result named _boobook_ with a duration of at least 15 seconds,
> and a bandwidth less than 1000 hertz, somewhere in the first 30 seconds
> of the test file.
 
###  Bounding box: `bounds`

A bounding box  is a rectangular region on a spectrogram (temporal and spectral bounds).
 
An event must use the `bounds` property in the _expectation_. A bounds
object must specify four coordinates (start, low, end, high). Each
can be any valid [number](./values.md#Numerics) type.


```yaml
expect:
  - bounds:
      start_seconds: 1.23
      low_hertz: 300
      end_seconds: 4.56
      high_hertz: 1600
```

There is a shortcut form that packs the four values into an array like `[start, low, end, high]`:

```yaml
expect:
  - bounds: [ 1.23, 300, 4.56, 1600 ]
```

The shortcut form is used the most frequently.


### Centroid: `centroid`

A centroid is a point on a spectrogram (temporal and spectral).
You can use it to match either:

- the geometric center of an expectation to a result event
- or, to expect a match to a point-like event result

An event must use the `centroid` property in the _expectation_. A centroid
object must specify two coordinates (start, low). Each
can be any valid [number](./values.md#Numerics) type.


```yaml
expect:
  - centroid:
      start_seconds: 1.23
      low_hertz: 300
```

There is a shortcut form that packs the two values into an array like `[start, low]`:

```yaml
expect:
  - centroid: [ 1.23, 300 ]
```

The shortcut form is used the most frequently.



- temporal range only: `[start, ~, end]`, `[start, ~, end, ~]`

### Time: `time`

A time is a temporal band of time.
You can use it to:

- match events that were detected from a waveform (and thus have no frequency information)
- match events from a spectrogram for which the frequency information is irrelevant
- match machine learning events (most results only output temporal coordinates for matches)


An event must use the `time` property in the _expectation_. A time
object must specify two coordinates (start, end). Each
can be any valid [number](./values.md#Numerics) type.


```yaml
expect:
  - time:
      start_seconds: 1.23
      end_seconds: 300
```

There is a shortcut form that packs the two values into an array like `[start, end]`:

```yaml
expect:
  - time: [ 1.23, 4.56 ]
```

The shortcut form is used the most frequently.

