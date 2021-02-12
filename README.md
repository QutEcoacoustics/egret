# Egret
<img align="right" width="100" height="100" alt="Egret logo. Credit David Clode." src="docs/media/david-clode-u0A7OMSRddM-unsplash-small.png"/>
 
> An **E**coacoustics **G**eneralized **R**ecognition and **E**vent **T**ester.

![egret demo image](docs/media/egret_output.png)

Egret is a general purpose audio recognition benchmarking tool. It's main job
is to compare the performance of acoustic event recognizers that detect faunal
vocalizations in environmental audio files. It can:

- can test hundreds of test audio files in parallel
- source test files from your computer, 🚧the internet🚧, 🚧an acoustic workbench🚧, or 🚧other sources🚧
- test each file in an array of analysis tools
- process acoustic event output from tools
- 🚧produce reports and graphs on recognition performance🚧
- compare performance between tools
- 🚧show recognizer performance over time🚧
- 🚧import existing test and training sets from your own CSV files so you don't have to
  rewrite your datasets!🚧
    - 🚧also supports the AviaNZ result format🚧
    - more formats coming...

Egret doesn't analyze audio itself - that is a job of a _tool_. 

Egret thus is made to be used with _different_ tools. 
It comes with out of the box support for some tool like _AnalysisPrograms.exe_
but it can be easily adapted to run _your_ own tool, or recognizer.

## Status

Egret is a very early prototype - an alpha. Expect many incomplete features, bugs,
and frequent 
breaking changes in features and scope. No warranty, express or implied, is given.

Where possible features that are not yet implemented are marked with a
_Construction_ sign emoji (🚧).

## Install

🚧TODO🚧

## Usage

Egret is a command line tool. You interact with it through a terminal like:

- [_Windows Terminal_](https://www.microsoft.com/en-au/p/windows-terminal/9n0dx20hk701) (required) on Windows
- the _Terminal_ app on MacOSX
- the _Terminal_ app on Linux

Once you've opened your terminal you can use one of the following Egret commands.

## Commands

Egret has two main commands:

1. `test` which runs all of the tests once and reports its findings
2. 🚧`watch` which watches all your test files, configs, and tools and will
    continually reports which tests pass or fail🚧

The watch command is useful for interactive training or testing of a new recognizer.

You can see detailed information by using the `--help` option. For example:

- Use `egret --help` to see all the commands available
- Use `egret test --help` to the different options you can use to run the test command

## Configuration

Although Egret is a command line tool, most of it's configuration is done in it's 
config files.

90% of the commands you will run will look like this:

```powershell
> egret test my-configuration-file.yml
```

There are several example config files in this repository and we'll explain the
various options in the rest of this document. The configuration files are written in a configuration language called YAML.

If you need an introduction to YAML please see this article: <https://sweetohm.net/article/introduction-yaml.en.html>.

We highly recommend using [_Visual Studio Code_](https://code.visualstudio.com/)
to edit your YAML config files. It is free, and comes with built in syntax highlighting
for YAML files.


### Configuration: Test Suites (`test_suites`)

The basic purpose of egret is to run tests. A group of tests is called a _suite_
(meaning _a set of programs with a uniform design and the ability to share data_ 
or in our case a set of related tests).

An empty test suite named `boobook` woud look like this in a config file:

```yaml
test_suite:
  boobook:
    # TODO: add tests
```

That configuration file isn't very useful - it just defines a name without any tests!
We'll add some tests in the next section.

You can have multiple suites in a config file:

```yaml
test_suite:
  boobook:
    # TODO: add tests
  koala:
    # TODO: add tests
```

For information on other test suite options, see [Test Suites](docs/test_suites.md)

### Configuration: Tests (`test_suites` ➡ `tests`)

A test suite contains a set of _tests_. Let's look at an example:

```yaml
test_suite:
  boobook:
    tests:
      # TODO: finish tests
      - file: windy.wav
      - file: boobook.wav
      - file: boobook1.wav
      - file: i_do_not_exist_because_someone_gave_me_this_silly_filename.wav
  koala:
      # TODO: finish tests
      - file: helicopter.wav
      - file: motorboat.wav
      - file: koala.wav
```

The above configuration can be read as: 

> There are two test suites, _boobook_ and _koala_. For the _boobook_ suite, there
are 4 tests, each using a different file. In the _koala_ suite there are three tests,
on three files.

It still isn't complete---we'll finish it in the next section. 
None of the _tests_ actually test anything yet but it does run a _tool_ on each _test_ and 
ensure it does not crash.

For example, running Egret with this configuration will produce the following output:

```powershell
C:\Temp > egret test boobook-koala-config.yml
Starting test command
Using configuration: C:\Temp\boobook-koala-config.yml.yml
Found 7 cases, running tests:
Results
✅boobook.0: {4.43 s} for ap with windy.wav 
✅boobook.1: {3.56 s} for ap with boobook.wav 
✅boobook.2: {3.70 s} for ap with boobook1.wav 
❌boobook.3: {0.00 s} for ap with i_do_not_exist_because_someone_gave_me_this_silly_filename.wav
   - Count not find source file: C:\Temp\i_do_not_exist_because_someone_gave_me_this_silly_filename.wav
✅koala.0: {6.12 s} for ap with helicopter.wav 
✅koala.1: {5.33 s} for ap with motorboat.wav 
✅koala.2: {9.28 s} for ap with koala.wav

Finished. Final results:
        Successes: 6
        Failures:1
        Result: 85.71%
```

This works as expected. The file named `i_do_not_exist_because_someone_gave_me_this_silly_filename.wav`
does not actually exist and Egret reported this as an error.

### Configuration: Expectations (`test_suites` ➡ `tests` ➡ `expect`)

Each test can have many _expectations_. That is, we expect that using a _tool_ 
will produce some results and we _expect_ those results
to have certain properties.

This is the really important bit.

We would _expect_ a Koala recognizer to produce (true positive) Koala recognition events.

With _expectations_ we tell Egret what to expect for each test. Let's finally finish our config:

```yaml
test_suite:
  boobook:
    tests:
      - file: windy.wav
        expect:
          - segment_with: no_events
      - file: boobook.wav
        expect:
          - label: boobook
            bounds: [ 1, 500, 2, 600 ]
          - label: boobook
            bounds: [ 33, 500, 37, 600 ]
      - file: boobook1.wav
        expect:
            - segment_with: event_count
              count: 3
              label: boobook
      # NOTE: removed test for the i_do_not_exist_because_someone_gave_me_this_silly_filename.wav file
      # because it clearly doesn't exist... it was right there in the name!
  koala:
      - file: helicopter.wav
        expect:
          - segment_with: no_events
      - file: motorboat.wav
        expect:
          - segment_with: no_events
      - file: koala.wav
        expect:
          - label: koala
            bounds: [ 4.75, 300, 19.5, 1200 ]
          - segment_with: no_extra_events

```

The above config can be read as:

> - for the _boobook_ suite, run three tests
>     - in the file `windy.wav` expect no acoustic events to be detected
>     - in the file `boobook.wav` expect two acoustics events
>         1. The first should have the label `boobook`, start at 1 second, be 1 second long, and have a bandwidth of 100 hertz
>         1. The second should have the label `boobook`, start at 33 seconds, be 5 seconds long, and have a bandwidth of 100 hertz
>     - in the file `boobook1.wav` we expect three events, all labelled with `boobook`, but we don't care where they are
> - for the _koala_ suite, run three tests
>     - in the file `helicopter.wav` expect no acoustic events to be detected
>     - in the file `motorboat.wav` expect no acoustic events to be detected
>     - in the file `koala.wav` we expect 1 event
>         1. The first should have the label `koala`, start at 4.75 seconds, be 14.75 seconds long, and have a bandwidth of 900 hertz
>         2. and: we check there are no other events found

There are several interesting features demonstrated in this example, like:

- checking for true positives
- ensuring exhaustive checks are done to help assess false positive and false negatives
- checking the duration and bandwidth of the detected events to ensure a correct match is identified

If we run this tool we get output that looks like:

```powershell
Starting test command
Using configuration: C:\Work\GitHub\egret\src\Egret.Cli\config.yml
Found 2 cases, running tests:
Results
✅ boobook.0:  {5.32 s} for ap with windy.wav
  Segment tests:
    - ✅ 0: no events {match: true}
✅ boobook.1:  {4.98 s} for ap with boobook.wav
  Events:
    - ✅ 0: {label: boobook, bounds: [ 1, 500, 2, 600 ], match: true}
    - ✅ 1: {label: boobook, bounds: [ 33, 500, 37, 600 ], match: true}
✅ boobook.1:  {6.34 s} for ap with boobook1.wav
  Segment tests:
    - ✅ 0: Segment has 3 events {count: 3, match: true, label: boobook}
❌koala.1:  {4.27 s} for ap helicopter.wav
  Segment tests:
    - ❌ 0: no events {match: true}
      - ❌ Event count: Expected 0 results but 3 were found
❌koala.2:  {6.67 s} for ap motorboat.wav
  Segment tests:
    - ❌ 0: no events {match: true}
      - ❌ Event count: Expected 0 results but 1 were found
❌koala.3:  {5.03 s} for ap koala.wav
  Events:
    - ✅ 1: {label: KOALA, bounds: [ 4.75, 300, 19.5, 1200 ], match: true}
  Segment tests:
    - ❌ 0: no extra events {match: true}
      - ❌ Event count: Expected 0 extra results but 1 other were found

Finished. Final results:
        Successes: 3
        Failures:3
        Result: 50.00%
```

Clearly our Koala recognizer needs some work!

Crucially though, after we (attempt to) improve our Koala recognizer we can 
check how well it performs by simply running the `egret test` command again.

🚧We can even compare a new result to old results!🚧

## FAQ

### I have questions

Please see our [docs](./docs/)

### I want to see more examples

Please see our [samples](./samples/)

### I have more questions / found a bug / want a feature / need some help

Please see:

- [discussions](https://github.com/QutEcoacoustics/egret/issues)
- [bugs](https://github.com/QutEcoacoustics/egret/issues)
- [features](https://github.com/QutEcoacoustics/egret/issues)
- [help](https://github.com/QutEcoacoustics/egret/issues)

### What units does Egret expect and use?

Unprefixed SI base units.

This means: 

- durations will always be expressed/stored/read in seconds
    - never milli, micro, mega, decimal hours, or decimal days (looking at you Excel!)
- frequency will always be expressed in hertz
    - never kilohertz, or octaves
- dates and times will always be expressed as [ISO8601 encoded strings](https://en.wikipedia.org/wiki/ISO_8601)
    - examples: 
        - `2020-11-12T15:43:38+00:00`
        - `2020-11-12`
        - `15:43:38`
    - never: 12-hour time, never any other date format (looking at you Americans)

In the rare case a unit or value must be reported or used that does not adhere to
the above rules, the column/property name using that value will have the full unit
encoded in the name.

Example:

```yaml
# good
lowKilohertz: 1.0

# bad
low: 1.0
```

### What are the different expectations I can check?

See [expectations](./docs/expectations.md) in our docs. Currently you can:

- check coordinates based on bounding box, centroid, or temporal span
- check event duration, bandwidth, or label
- check meta features like event count, check for no events, or check for no extra events

### What kind of values can I use in my expectations?

<small>(yeah I realize I am asking this _for_ you)</small>

Great question! The real world is messy. Generally everywhere you see a number
in an Egret config file you can replace it with an Egret _expression_.
Expressions allow results values to roughly match each other within some given
range or tolerance.

Expressions can represent strict equality, inequality, relations, tolerances,
thresholds, approximations, and intervals.

See [values](./docs/values.md) for a lot more detail.

### How does egret know how to read results?

Please see [results](./docs/results.md).

### Can Egret handle columns/properties that have different names?

Hopefully! We go to great lengths to try and read your data.

Please see [munging](./docs/munging.md) for more detail.
 
### Can egret read my existing test datasets made with {{software package xyz}}?

It depends. Please see [imports](./docs/imports.md).

🚧
We want to add support for importing (reusing) labelled datasets from:

- other Egret config  files (useful for reusing noise/anti-matching datasets)
- CSV files
- AvianNZ label files
- Audacity annotations
- Raven labels

But all these features take work. Which would you like?
🚧

### Does Egret work with  {{software package xyz}}?

If your analysis tool of choice can:

- analyze a single segment of audio
- do so via a shell command
- return results in a file 

then probably yes!

If it doesn't work then we're keen to address that.

Please see [tools](./docs/tools.md) for guidance in setting up a new tool.

### Does Egret work with my fancy-schmancy python/R script/program?

It can. There are two steps:

1. ensure you can run your analysis via the command line (see [tools](./docs/tools.md) for guidance)
2. ensure you return results that approximately adhere to our standards (see [results](./docs/results.md) for guidance)
3. (this is now an _index off by one joke_)

### Does Egret work with machine learning / deep learning tools?

Yes? No? Egret doesn't care.

Egret works with _tools_ that produce _results_. Egret does not care how you
produce your result, only that it was produced and passes the tests defined
in the configuration file.

You're free to use whatever method of analysis that you want.

### Why C#?

Because I (@atruskie) am fast and productive with it, especially when I have to
produce long-lived, real-world products.

## LICENSE

Apache v2.

---

Egret logo image credit: [David Clode](https://unsplash.com/photos/u0A7OMSRddM)
