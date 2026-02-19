# LO3: Testing, Debugging, and Results (10 marks)

This folder contains the assessment answer for:

**Properly test the developed application and demonstrate the debugging process and testing results.**

Criteria addressed:

- Comprehensive testing strategies: **unit**, **integration**, and **functional** testing
- Testing results **analyzed and documented**
- **Effective debugging** demonstrated with real issues and resolutions

## Files

| File | Description |
|------|--------------|
| `LO3_Testing_Debugging_and_Results.md` | Full written answer: strategy, test types, results, debugging process |

## Quick links in the answer

1. **Testing strategy** – pyramid, tools, project layout  
2. **Unit testing** – Manufacturer, Distributor, Seller (services + repository examples)  
3. **Integration testing** – order flow and availability tests  
4. **Functional / manual testing** – scenarios and tools  
5. **Testing results** – tables, run commands, coverage notes  
6. **Debugging** – port conflict, SQLite table, namespace, UI tab; tools used  
7. **Conclusion** – summary against the LO3 criteria  

## Converting to PDF

From this folder:

```bash
pandoc LO3_Testing_Debugging_and_Results.md -o LO3_Testing_Debugging_and_Results.pdf
```

Or use VS Code “Markdown PDF” extension, or any markdown-to-PDF tool, as for the main assessment document.

## Source code references

- Unit tests: `ManufacturerService.Tests/`, `DistributorService.Tests/`, `SellerService.Tests/`
- Integration tests: `CozyComfort.IntegrationTests/`
- Broader testing notes: repo root `docs/TESTING_DOCUMENTATION.md`
