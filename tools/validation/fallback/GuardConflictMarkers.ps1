param(
    [string]$Root = "../../../Assets/Scripts"
)

# Audit (ninth pass G-P1) — fail-fast scan for unresolved git merge markers.
$open  = ([string][char]60) * 7
$close = ([string][char]62) * 7

$resolved = Resolve-Path -Path $Root -ErrorAction SilentlyContinue
if (-not $resolved) {
    Write-Host "GuardConflictMarkers: skip - path does not exist."
    exit 0
}

$hits = Get-ChildItem -Path $resolved.Path -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue |
    Select-String -Pattern @($open, $close) -SimpleMatch -List -ErrorAction SilentlyContinue

if ($hits -and $hits.Count -gt 0) {
    Write-Host "CONFLICT MARKER FOUND in Assets/Scripts"
    foreach ($hit in $hits) {
        $msg = "  " + [string]$hit.Path + " line " + [string]$hit.LineNumber
        Write-Host $msg
    }
    exit 1
}

exit 0
