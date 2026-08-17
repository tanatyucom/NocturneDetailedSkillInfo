$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$validationRoot = Join-Path $repositoryRoot 'docs\validation'

$cases = @(
    @{
        Name = 'Japanese'
        Path = Join-Path $validationRoot 'skill_validation_ja.csv'
        Sha256 = '5CF701D41A20A92497A6F8B8A03265D99B214EF22228DE329891032ED355C6AE'
    },
    @{
        Name = 'English'
        Path = Join-Path $validationRoot 'skill_validation_en.csv'
        Sha256 = 'DBDFAD68A6DB0E0550F7B1480B162CBCCC82B98866ADC5D7A19FA5C253771091'
    }
)

foreach ($case in $cases) {
    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $case.Path).Hash
    if ($hash -ne $case.Sha256) {
        throw "$($case.Name) audit SHA-256 mismatch: $hash"
    }

    $rows = @(Import-Csv -LiteralPath $case.Path)
    $changed = @($rows | Where-Object Changed -eq 'YES')

    if ($rows.Count -ne 512) {
        throw "$($case.Name) audit row count was $($rows.Count), expected 512."
    }

    if ($changed.Count -ne 150) {
        throw "$($case.Name) changed count was $($changed.Count), expected 150."
    }

    Write-Host "PASS $($case.Name): SHA-256, 512 rows, 150 changed"
}

Write-Host 'All validation artifacts passed integrity checks.'
