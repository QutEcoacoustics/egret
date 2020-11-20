 # Naming and munging:

 Every result property/column is munged using a list of name aliases to try and find columns/properties that can be used for testing.

> mung  
> /mʌn(d)ʒ/  
> verb  (INFORMAL•COMPUTING)  
>     manipulate (data).  
>     "you could do what anti-spammers have done for years and mung the URLs"

This is done so that software does not have to be rewritten or data 
be transformed so that either can be used with Egret.

Currently, Egret applies the following operations:
- it strips whitespace (spaces, tabs)
- it does a case-insensitive string comparison
- tries a few different naming conventions
- ties a few different variants of the expected name

 To see a list of required fields, their definitions, and various alternate acceptable
 names, run the `munging` command.


 ```powershell
 > egret munging
 ```

 Currently Egret will look for these fields in a result set:

 - `label`
 - `start`
 - `end`
 - `low`
 - `high`