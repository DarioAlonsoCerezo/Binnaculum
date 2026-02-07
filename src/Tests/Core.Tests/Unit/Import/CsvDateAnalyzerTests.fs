namespace Tests

open System
open System.IO
open Microsoft.VisualStudio.TestTools.UnitTesting
open Binnaculum.Core.Import

/// <summary>
/// Unit tests for CsvDateAnalyzer multi-file import functionality
/// Tests date extraction, file sorting, gap detection, and overlap detection
/// </summary>
[<TestClass>]
type CsvDateAnalyzerTests() =

    /// <summary>
    /// Test extracting dates from IBKR filename patterns
    /// </summary>
    [<TestMethod>]
    member this.``extractDateFromFilename should parse IBKR format correctly``() =
        let result = CsvDateAnalyzer.extractDateFromFilename "Daily_statement.1332220.20230228.csv"
        Assert.AreEqual(Some(DateTime(2023, 2, 28)), result)
    
    [<TestMethod>]
    member this.``extractDateFromFilename should parse IBKR format with different account number``() =
        let result = CsvDateAnalyzer.extractDateFromFilename "Daily_statement.9876543.20240101.csv"
        Assert.AreEqual(Some(DateTime(2024, 1, 1)), result)
    
    /// <summary>
    /// Test extracting dates from Tastytrade filename patterns
    /// </summary>
    [<TestMethod>]
    member this.``extractDateFromFilename should parse Tastytrade format correctly``() =
        let result = CsvDateAnalyzer.extractDateFromFilename "tastytrade_transactions_history_x5WY40536_240401_to_240430.csv"
        Assert.AreEqual(Some(DateTime(2024, 4, 1)), result)
    
    [<TestMethod>]
    member this.``extractDateFromFilename should parse Tastytrade format with different dates``() =
        let result = CsvDateAnalyzer.extractDateFromFilename "tastytrade_transactions_history_ABC12345_230215_to_230228.csv"
        Assert.AreEqual(Some(DateTime(2023, 2, 15)), result)
    
    /// <summary>
    /// Test handling of unrecognized filename formats
    /// </summary>
    [<TestMethod>]
    member this.``extractDateFromFilename should return None for unrecognized format``() =
        let result = CsvDateAnalyzer.extractDateFromFilename "random_file.csv"
        Assert.AreEqual(None, result)
    
    [<TestMethod>]
    member this.``extractDateFromFilename should return None for empty filename``() =
        let result = CsvDateAnalyzer.extractDateFromFilename ""
        Assert.AreEqual(None, result)
    
    /// <summary>
    /// Test parsing dates from CSV rows with various formats
    /// </summary>
    [<TestMethod>]
    member this.``tryParseDateFromCsvRow should parse ISO format date``() =
        let result = CsvDateAnalyzer.tryParseDateFromCsvRow "2023-02-28,100.50,AAPL,Buy"
        Assert.AreEqual(Some(DateTime(2023, 2, 28)), result)
    
    [<TestMethod>]
    member this.``tryParseDateFromCsvRow should parse US format date``() =
        let result = CsvDateAnalyzer.tryParseDateFromCsvRow "02/28/2023,100.50,AAPL,Buy"
        Assert.AreEqual(Some(DateTime(2023, 2, 28)), result)
    
    [<TestMethod>]
    member this.``tryParseDateFromCsvRow should parse date with timestamp``() =
        let result = CsvDateAnalyzer.tryParseDateFromCsvRow "2023-02-28 14:30:00,100.50,AAPL,Buy"
        Assert.AreEqual(Some(DateTime(2023, 2, 28, 14, 30, 0)), result)
    
    [<TestMethod>]
    member this.``tryParseDateFromCsvRow should parse compact date format``() =
        let result = CsvDateAnalyzer.tryParseDateFromCsvRow "AAPL,20230228,100.50,Buy"
        Assert.AreEqual(Some(DateTime(2023, 2, 28)), result)
    
    [<TestMethod>]
    member this.``tryParseDateFromCsvRow should return None for row without date``() =
        let result = CsvDateAnalyzer.tryParseDateFromCsvRow "AAPL,100.50,Buy,Sell"
        Assert.AreEqual(None, result)
    
    [<TestMethod>]
    member this.``tryParseDateFromCsvRow should return None for empty row``() =
        let result = CsvDateAnalyzer.tryParseDateFromCsvRow ""
        Assert.AreEqual(None, result)
    
    [<TestMethod>]
    member this.``tryParseDateFromCsvRow should ignore dates before year 2000``() =
        let result = CsvDateAnalyzer.tryParseDateFromCsvRow "1999-12-31,100.50,AAPL,Buy"
        Assert.AreEqual(None, result)
    
    /// <summary>
    /// Test extractAllDatesFromCsv with in-memory test data
    /// </summary>
    [<TestMethod>]
    member this.``extractAllDatesFromCsv should return empty list for non-existent file``() =
        let result = CsvDateAnalyzer.extractAllDatesFromCsv "/tmp/nonexistent_file.csv"
        Assert.AreEqual(0, result.Count)
    
    /// <summary>
    /// Test analyzeFile with temporary test files
    /// </summary>
    [<TestMethod>]
    member this.``analyzeFile should extract metadata from CSV with dates``() =
        // Create a temporary CSV file
        let tempFile = Path.GetTempFileName()
        let tempCsvNullable = Path.ChangeExtension(tempFile, ".csv")
        
        match tempCsvNullable with
        | null -> Assert.Fail("Failed to create temp file path")
        | tempCsv ->
            File.Move(tempFile, tempCsv)
            
            try
                File.WriteAllLines(tempCsv, [|
                    "Date,Symbol,Quantity,Price"
                    "2023-02-01,AAPL,10,150.00"
                    "2023-02-15,MSFT,5,300.00"
                    "2023-02-28,GOOGL,3,100.00"
                |])
                
                let result = CsvDateAnalyzer.analyzeFile tempCsv
                
                let fileNameNullable = Path.GetFileName(tempCsv)
                match fileNameNullable with
                | null -> Assert.Fail("Failed to get file name")
                | fileName -> Assert.AreEqual(fileName, result.FileName)
                
                Assert.AreEqual(3, result.ExactRecordCount)
                Assert.AreEqual(Some(DateTime(2023, 2, 1, result.EarliestDate)))
                Assert.AreEqual(Some(DateTime(2023, 2, 28, result.LatestDate)))
                Assert.AreEqual(3, result.AllDates.Length)
            finally
                if File.Exists(tempCsv) then File.Delete(tempCsv)
    
    /// <summary>
    /// Test detectDateGaps with sample data
    /// </summary>
    [<TestMethod>]
    member this.``detectDateGaps should detect gap between files``() =
        let metadata = [
            { FilePath = "/tmp/file1.csv"
              FileName = "file1.csv"
              EarliestDate = Some(DateTime(2023, 2, 1))
              LatestDate = Some(DateTime(2023, 2, 10))
              ExactRecordCount = 10
              AllDates = [DateTime(2023, 2, 1); DateTime(2023, 2, 10)] }
            { FilePath = "/tmp/file2.csv"
              FileName = "file2.csv"
              EarliestDate = Some(DateTime(2023, 2, 20))
              LatestDate = Some(DateTime(2023, 2, 28))
              ExactRecordCount = 5
              AllDates = [DateTime(2023, 2, 20); DateTime(2023, 2, 28)] }
        ]
        
        let gaps = CsvDateAnalyzer.detectDateGaps metadata
        
        Assert.AreEqual(1, gaps.Length)
        let (startDate, endDate, daysMissing) = gaps.[0]
        Assert.AreEqual(DateTime(2023, 2, 10, startDate))
        Assert.AreEqual(DateTime(2023, 2, 20, endDate))
        Assert.AreEqual(10, daysMissing)
    
    [<TestMethod>]
    member this.``detectDateGaps should return empty for consecutive files``() =
        let metadata = [
            { FilePath = "/tmp/file1.csv"
              FileName = "file1.csv"
              EarliestDate = Some(DateTime(2023, 2, 1))
              LatestDate = Some(DateTime(2023, 2, 10))
              ExactRecordCount = 10
              AllDates = [DateTime(2023, 2, 1); DateTime(2023, 2, 10)] }
            { FilePath = "/tmp/file2.csv"
              FileName = "file2.csv"
              EarliestDate = Some(DateTime(2023, 2, 11))
              LatestDate = Some(DateTime(2023, 2, 20))
              ExactRecordCount = 5
              AllDates = [DateTime(2023, 2, 11); DateTime(2023, 2, 20)] }
        ]
        
        let gaps = CsvDateAnalyzer.detectDateGaps metadata
        
        Assert.AreEqual(0, gaps.Count)
    
    /// <summary>
    /// Test detectDateOverlaps with sample data
    /// </summary>
    [<TestMethod>]
    member this.``detectDateOverlaps should detect overlapping dates``() =
        let metadata = [
            { FilePath = "/tmp/file1.csv"
              FileName = "file1.csv"
              EarliestDate = Some(DateTime(2023, 2, 1))
              LatestDate = Some(DateTime(2023, 2, 15))
              ExactRecordCount = 10
              AllDates = [DateTime(2023, 2, 1); DateTime(2023, 2, 10); DateTime(2023, 2, 15)] }
            { FilePath = "/tmp/file2.csv"
              FileName = "file2.csv"
              EarliestDate = Some(DateTime(2023, 2, 10))
              LatestDate = Some(DateTime(2023, 2, 28))
              ExactRecordCount = 5
              AllDates = [DateTime(2023, 2, 10); DateTime(2023, 2, 20); DateTime(2023, 2, 28)] }
        ]
        
        let overlaps = CsvDateAnalyzer.detectDateOverlaps metadata
        
        Assert.AreEqual(1, overlaps.Length)
        let (file1, file2, overlapDate) = overlaps.[0]
        Assert.AreEqual("file1.csv", file1)
        Assert.AreEqual("file2.csv", file2)
        Assert.AreEqual(DateTime(2023, 2, 10, overlapDate))
    
    [<TestMethod>]
    member this.``detectDateOverlaps should return empty for non-overlapping files``() =
        let metadata = [
            { FilePath = "/tmp/file1.csv"
              FileName = "file1.csv"
              EarliestDate = Some(DateTime(2023, 2, 1))
              LatestDate = Some(DateTime(2023, 2, 10))
              ExactRecordCount = 10
              AllDates = [DateTime(2023, 2, 1); DateTime(2023, 2, 10)] }
            { FilePath = "/tmp/file2.csv"
              FileName = "file2.csv"
              EarliestDate = Some(DateTime(2023, 2, 15))
              LatestDate = Some(DateTime(2023, 2, 28))
              ExactRecordCount = 5
              AllDates = [DateTime(2023, 2, 15); DateTime(2023, 2, 28)] }
        ]
        
        let overlaps = CsvDateAnalyzer.detectDateOverlaps metadata
        
        Assert.AreEqual(0, overlaps.Count)
    
    /// <summary>
    /// Test analyzeAndSort for chronological ordering
    /// </summary>
    [<TestMethod>]
    member this.``analyzeAndSort should order files chronologically``() =
        // Create temporary CSV files with known dates
        let tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
        Directory.CreateDirectory(tempDir) |> ignore
        
        try
            let file1 = Path.Combine(tempDir, "file_20230301.csv")
            let file2 = Path.Combine(tempDir, "file_20230215.csv")
            let file3 = Path.Combine(tempDir, "file_20230228.csv")
            
            File.WriteAllLines(file1, [| "Date,Symbol"; "2023-03-01,AAPL" |])
            File.WriteAllLines(file2, [| "Date,Symbol"; "2023-02-15,MSFT" |])
            File.WriteAllLines(file3, [| "Date,Symbol"; "2023-02-28,GOOGL" |])
            
            let analysis = CsvDateAnalyzer.analyzeAndSort [file1; file2; file3]
            
            Assert.AreEqual(3, analysis.TotalFiles)
            Assert.AreEqual(3, analysis.TotalRecords)
            
            // Verify chronological ordering
            let orderedDates = 
                analysis.FilesOrderedByDate 
                |> List.choose (fun m -> m.EarliestDate)
            
            Assert.AreEqual(DateTime(2023, 2, 15, orderedDates.[0]))
            Assert.AreEqual(DateTime(2023, 2, 28, orderedDates.[1]))
            Assert.AreEqual(DateTime(2023, 3, 1, orderedDates.[2]))
            
            // Verify overall date range
            match analysis.OverallDateRange with
            | Some (earliest, latest) ->
                Assert.AreEqual(DateTime(2023, 2, 15, earliest))
                Assert.AreEqual(DateTime(2023, 3, 1, latest))
            | None ->
                Assert.Fail("Expected overall date range to be calculated")
        finally
            if Directory.Exists(tempDir) then 
                Directory.Delete(tempDir, true)
    
    [<TestMethod>]
    member this.``analyzeAndSort should detect gaps and generate warnings``() =
        // Create temporary CSV files with a date gap
        let tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
        Directory.CreateDirectory(tempDir) |> ignore
        
        try
            let file1 = Path.Combine(tempDir, "file_20230201.csv")
            let file2 = Path.Combine(tempDir, "file_20230220.csv")
            
            File.WriteAllLines(file1, [| "Date,Symbol"; "2023-02-01,AAPL"; "2023-02-05,MSFT" |])
            File.WriteAllLines(file2, [| "Date,Symbol"; "2023-02-20,GOOGL"; "2023-02-25,TSLA" |])
            
            let analysis = CsvDateAnalyzer.analyzeAndSort [file1; file2]
            
            Assert.IsTrue(analysis.DateGaps.Length > 0)
            Assert.IsTrue(analysis.Warnings.Length > 0)
            Assert.IsTrue(analysis.Warnings |> List.exists (fun w -> w.Contains("gap")))
        finally
            if Directory.Exists(tempDir) then 
                Directory.Delete(tempDir, true)
