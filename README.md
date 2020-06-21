# unami_csv
Translates a text file with a list of Unami routes to a CSV file.

This is intended to be used internally only by the developers of the Unami Airlines Discord bot. However, if you are interested, you can go check out main.cs for more detailed info.

Compile:
```
csc main.cs out:unamiCSV.exe
```
Run (square brackets = required, curly brackets = optional):
```
./unamiCSV.exe [sortedRoutesFilename] [CSVOutputFileName] {-noHeader} {-noComma}
```
