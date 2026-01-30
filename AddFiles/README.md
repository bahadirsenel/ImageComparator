# AddFiles.exe - Inter-Process File Scanner

## Purpose

AddFiles.exe is a helper executable that performs file system scanning for ImageComparator.exe. It runs as a separate process to scan directories and filter files by type.

## Usage

**⚠️ This executable is designed to be called BY ImageComparator.exe, not run directly.**

When ImageComparator needs to scan folders for images, it:
1. Creates input files: `Directories.json` and `Filters.json`
2. Launches `AddFiles.exe` as a child process
3. Waits for completion
4. Reads the output file: `Results.json`
5. Cleans up temporary files

## Input Files

### Directories.json
```json
{
  "Directories": [
    "C:\\Users\\Username\\Pictures",
    "D:\\Photos"
  ]
}
```

### Filters.json
```json
{
  "IncludeSubfolders": true,
  "JpegFiles": true,
  "GifFiles": false,
  "PngFiles": true,
  "BmpFiles": false,
  "TiffFiles": false,
  "IcoFiles": false
}
```

## Output File

### Results.json
```json
{
  "GotException": false,
  "Files": [
    "C:\\Users\\Username\\Pictures\\photo1.jpg",
    "C:\\Users\\Username\\Pictures\\photo2.png"
  ]
}
```

## Manual Testing

If you need to test AddFiles.exe manually:

1. **Create Directories.json** in the same folder as AddFiles.exe:
   ```json
   {
     "Directories": ["C:\\Test\\Folder"]
   }
   ```

2. **Create Filters.json** in the same folder:
   ```json
   {
     "IncludeSubfolders": false,
     "JpegFiles": true,
     "GifFiles": true,
     "PngFiles": true,
     "BmpFiles": true,
     "TiffFiles": true,
     "IcoFiles": true
   }
   ```

3. **Run AddFiles.exe** from command line:
   ```cmd
   cd path\to\Bin
   AddFiles.exe
   ```

4. **Check Results.json** for output

5. **Check AddFiles_Error.log** if an error occurred

## Diagnostic Files

- **AddFiles_Error.log** - Created if an exception occurs (contains full error details)
- **Results.json** - Always created with results or error flag

## Common Issues

### "Could not find file Directories.json"
**Cause:** Input files missing (you're running it manually without creating them)  
**Solution:** Create the input JSON files as shown above, or run via ImageComparator.exe

### "GotException: true" with empty Files array
**Cause:** An error occurred during scanning  
**Solution:** Check AddFiles_Error.log for details

### Missing DLLs error
**Cause:** System.Text.Json dependencies not deployed  
**Solution:** Rebuild the solution to trigger post-build events

## Dependencies

AddFiles.exe requires these DLLs to be in the same folder:
- System.Text.Json.dll
- Microsoft.Bcl.AsyncInterfaces.dll
- System.Buffers.dll
- System.Memory.dll
- System.Runtime.CompilerServices.Unsafe.dll
- System.Text.Encodings.Web.dll
- System.Threading.Tasks.Extensions.dll
- System.ValueTuple.dll
- System.Numerics.Vectors.dll

These are automatically copied by the post-build event when you build the solution.
